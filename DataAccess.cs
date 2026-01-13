using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.File;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;




namespace AISPubSub
{
    public class DataAccess : IDisposable
    {
        //Database related
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DbPath = Path.Combine(BaseDir, "identifier.sqlite");
        private static readonly string ConnectionString = $"Data Source={DbPath}";
        private string _connectionstring;


        public DataAccess(string connectionstring)
        {
            _connectionstring = connectionstring;
        }
        public Task InitializeDatabase()
        {

            //// Ensure the file exists
            //await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteSample.db", CreationCollisionOption.OpenIfExists);

            //await ApplicationData.Current.LocalFolder

            string dbpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sqliteSample.db");

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            Console.WriteLine(dbpath);
            Console.WriteLine(folder);

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(folder, "app.log"), outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day);
                

            return Task.CompletedTask;
            //string dbpath = Path.Combine(folder, "sqliteSample.db");

            //using var db = new SqliteConnection($"Filename={dbpath}");
            //db.Open();

            //string tableCommand = @"CREATE TABLE IF NOT EXISTS MyTable (
            //                    Primary_Key INTEGER PRIMARY KEY, 
            //                    Text_Entry NVARCHAR(2048) NULL)";

            //using var createTable = new SqliteCommand(tableCommand, db);
            //// Use ExecuteNonQuery for commands that don't return data rows
            //createTable.ExecuteNonQuery();

        }

        private async Task CreateSqliteDynamicTableIfNotExists(SqliteConnection connection, string tableName, DataTable dataTable)
        {
            var columnDefinitions = new List<string>();

            foreach (DataColumn column in dataTable.Columns)
            {
                string sqliteType = column.DataType switch
                {
                    var t when t == typeof(int) || t == typeof(long) => "INTEGER",
                    var t when t == typeof(double) || t == typeof(float) || t == typeof(decimal) => "REAL",
                    _ => "TEXT"
                };
                columnDefinitions.Add($"[{column.ColumnName.Replace("_", "")}] {sqliteType}");
            }

            var createTableQuery = $"CREATE TABLE IF NOT EXISTS [{tableName}] ({string.Join(", ", columnDefinitions)});";

            await using var command = new SqliteCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
        }


        public async Task InsertDataIntoSqlServerAsync(DataTable dataTable, string tableName, SqlConnectionStringBuilder connectionString)
        {
            tableName = tableName.Replace("-", "_");
            tableName = tableName.Replace(" ", "_");
            tableName = tableName.Replace(".csv", "");
            tableName = tableName.Replace(".", "_");

            using (var connection = new SqlConnection(connectionString.ConnectionString))
            {
                await connection.OpenAsync();

                // Dynamically create a table based on DataTable's columns if it doesn't exist
                await CreateDynamicTableIfNotExists(connection, tableName, dataTable);

                // Bulk insert the data from DataTable into the SQL table
                using (SqlBulkCopy bulkCopy = new(connection))
                {
                    bulkCopy.DestinationTableName = tableName;

                    // Map each column from the DataTable to the SQL Server table
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable); // Efficiently write data
                    //Log(string.Format("Inserted {0} rows into SQL table '{1}'", dataTable.Rows.Count, tableName));
                }
            }
        }

        private async Task CreateDynamicTableIfNotExists(SqlConnection connection, string tableName, DataTable dataTable)
        {

            string dropTableQuery = $"IF EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U') BEGIN " +
                                      $"DROP TABLE {tableName}; END";

            string createTableQuery = $"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U') BEGIN " +
                                      $"CREATE TABLE {tableName} (";

            // Dynamically generate the column definitions based on DataTable columns
            foreach (DataColumn column in dataTable.Columns)
            {
                string sqlType = GetSqlType(column.DataType); // Map C# types to SQL types
                createTableQuery += $"[{column.ColumnName}] {sqlType},"; //.Replace("%20", "_")
            }

            createTableQuery = createTableQuery.TrimEnd(',') + "); END";

            using (SqlCommand command = new(dropTableQuery, connection))
            {
                try
                {
                    int result = await command.ExecuteNonQueryAsync();
                    if (result == -1)
                    {
                        //Log(string.Format("Drop table {0} in SQL database.", tableName));
                    }
                    else
                    {
                        //Log(string.Format("Error dropping table {0} in SQL database.", tableName));
                    }
                }
                catch (Exception)
                {
                    //Log(string.Format("Error dropping table {0} in SQL database.", tableName));
                }

            }

            using (SqlCommand command = new(createTableQuery, connection))
            {
                try
                {
                    int result = await command.ExecuteNonQueryAsync();
                    if (result == -1)
                    {
                        //Log(string.Format("Created table {0} in SQL database.", tableName));
                    }
                    else
                    {
                        //Log(string.Format("Error Creating table {0} in SQL database.", tableName));
                    }
                }
                catch (Exception)
                {
                    //Log(string.Format("Error Creating table {0} in SQL database.", tableName));
                }

            }
        }

        private static string GetSqlType(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Int32 => "INT",
                TypeCode.String => "NVARCHAR(MAX)",
                TypeCode.DateTime => "DATETIME",
                TypeCode.Boolean => "BIT",
                TypeCode.Decimal => "DECIMAL(18, 2)",
                _ => throw new NotSupportedException($"Type {type.Name} is not supported"),
            };
        }

        private async Task InsertDataIntoSqliteServerAsync(DataTable? dataTable, string tableName, string ConnectionString)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return;

            foreach (DataColumn col in dataTable.Columns)
            {
                // Decode %20 and replace other problematic characters with underscores
                string cleanName = System.Net.WebUtility.UrlDecode(col.ColumnName);
                cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"[.\-\s%]", "_");
                col.ColumnName = cleanName;
            }

            tableName = System.Text.RegularExpressions.Regex.Replace(tableName, @"[.\-\s]", "_").Replace(".csv", "");

            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            // Ensure this method exists and handles SQLite types correctly
            await CreateSqliteDynamicTableIfNotExists(connection, tableName, dataTable);

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // We use a dictionary or parallel arrays to ensure the @ParamName in SQL matches the C# Add() call exactly.
                var columns = dataTable.Columns.Cast<DataColumn>().ToList();

                var colNamesSql = string.Join(", ", columns.Select(c => $"[{c.ColumnName.Replace("_", "")}]"));

                // Sanitize parameter names to remove spaces/symbols, ensuring they are valid SQL variables
                var paramNamesList = columns.Select(c => $"@param_{System.Text.RegularExpressions.Regex.Replace(c.ColumnName, @"\W", "")}").ToList();
                var paramNamesSql = string.Join(", ", paramNamesList);

                var insertSql = $"INSERT INTO [{tableName}] ({colNamesSql}) VALUES ({paramNamesSql})";
                Console.WriteLine(insertSql);

                await using var command = new SqliteCommand(insertSql, connection, (SqliteTransaction)transaction);

                var sqliteParameters = new List<SqliteParameter>();
                for (int i = 0; i < columns.Count; i++)
                {
                    // Create the parameter with the specific name used in the SQL string
                    var param = command.Parameters.AddWithValue(paramNamesList[i], DBNull.Value);
                    sqliteParameters.Add(param);
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        // Handle nulls: if the logic is null, send DBNull.Value
                        var value = row[columns[i]];
                        sqliteParameters[i].Value = value ?? DBNull.Value;
                    }

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                //Log($"Successfully inserted {dataTable.Rows.Count} rows into SQLite.");
                
            }
            catch (Exception )
            {
                await transaction.RollbackAsync();
                //Log($"SQLite Insert Error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
