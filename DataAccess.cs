using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Serilog;

namespace AISPubSub
{
    public class DataAccess
    {
        //Database related
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DbPath = Path.Combine(BaseDir, "identifier.sqlite");
        private static readonly string DefaultConnectionString = $"Data Source={DbPath}";
        public string Connectionstring { get; }

        
        public DataAccess(string? connectionstring = null)
        {
            Connectionstring = connectionstring ?? DefaultConnectionString;
        }

        public Task InitializeDatabase()
        {
            try
            {
                // Ensure the directory exists (SQLite creates the file, but not the path)
                var directory = Path.GetDirectoryName(DbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.Information("Created directory: {Directory}", directory);
                }

                // Microsoft.Data.Sqlite automatically creates the file on Open()
                using var connection = new SqliteConnection(Connectionstring);
                connection.OpenAsync();

                Log.Information("SQLite database initialized at: {DbPath}", DbPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize SQLite database.");
                throw;
            }
            return Task.CompletedTask;
        }
        private static readonly Dictionary<Type, string> SqliteTypeMap = new()
        {
            { typeof(int), "INTEGER" },
            { typeof(long), "INTEGER" },
            { typeof(short), "INTEGER" },
            { typeof(byte), "INTEGER" },
            { typeof(bool), "INTEGER" }, // SQLite stores booleans as 0 or 1
            { typeof(double), "REAL" },
            { typeof(float), "REAL" },
            { typeof(decimal), "REAL" },
            { typeof(DateTime), "TEXT" }, // Best practice for SQLite date storage
            { typeof(Guid), "TEXT" },
            { typeof(byte[]), "BLOB" }
        };
        
        private async Task CreateSqliteDynamicTableIfNotExists(SqliteConnection connection, string tableName, DataTable dataTable)
        {
            var columnDefinitions = new List<string>();

            foreach (DataColumn column in dataTable.Columns)
            {
                // Use the type map, defaulting to TEXT for unknown types
                if (!SqliteTypeMap.TryGetValue(column.DataType, out string? sqliteType))
                {
                    sqliteType = "TEXT";
                }

                // Sanitize column names: Remove non-alphanumeric characters
                string safeColumnName = Regex.Replace(column.ColumnName, @"[^a-zA-Z0-9_]", "");
                columnDefinitions.Add($"[{safeColumnName}] {sqliteType}");
            }

            var columnQuery      = string.Join(", ", columnDefinitions);
            var createTableQuery = $"CREATE TABLE IF NOT EXISTS [{tableName}] ({columnQuery});";

            await using var command = new SqliteCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
            Log.Debug("Ensured table {TableName} exists.", tableName);
        }


        public async Task InsertDataIntoSqlServerAsync(DataTable dataTable, string tableName, SqlConnectionStringBuilder connectionString)
        {
            // tableName = tableName.Replace("-", "_");
            // tableName = tableName.Replace(" ", "_");
            // tableName = tableName.Replace(".csv", "");
            // tableName = tableName.Replace(".", "_");
            // Clean Column Names: %20 -> " ", %26 -> "&", then replace with "_"
            foreach (DataColumn col in dataTable.Columns)
            {
                string decodedName = WebUtility.UrlDecode(col.ColumnName);
                // Replace spaces, ampersands, and other special characters with underscores
                col.ColumnName = Regex.Replace(decodedName, @"[^a-zA-Z0-9_]", "_");
            }

            // Clean Table Name
            tableName = Regex.Replace(tableName, @"[^a-zA-Z0-9_]", "_").Replace("_csv", "");
            
            if (!connectionString.TrustServerCertificate)
            {
                connectionString.TrustServerCertificate = true;
            }
            await using var connection = new SqlConnection(connectionString.ConnectionString);
            await connection.OpenAsync();
            
            // Dynamically create a table based on DataTable's columns if it doesn't exist
            await CreateDynamicTableIfNotExists(connection, tableName, dataTable);

            // Bulk insert the data from DataTable into the SQL table
            using (SqlBulkCopy bulkCopy = new(connection))
            {
                bulkCopy.DestinationTableName = $"[{tableName}]";
                bulkCopy.BulkCopyTimeout = 60;

                // Map each column from the DataTable to the SQL Server table
                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable); // Efficiently write data
                Log.Information("Inserted {Count} rows into SQLite table {Table}", dataTable.Rows.Count, tableName);
  
            }
        }

        private async Task CreateDynamicTableIfNotExists(SqlConnection connection, string tableName, DataTable dataTable)
        {
            // 1. Check if the table already exists
            string checkTableQuery = $@"(SELECT COUNT(*) FROM sys.tables WHERE name = N'{tableName}')";
            
            await using (SqlCommand checkCmd = new SqlCommand(checkTableQuery, connection))
            {
                var result = await checkCmd.ExecuteScalarAsync();
                bool tableExists = Convert.ToInt32(result) > 0;

                if (tableExists)
                {
                    try
                    {
                        // Option A: Truncate (Fastest, keeps schema/indexes)
                        string truncateQuery = $"TRUNCATE TABLE [{tableName}]";
                        await using SqlCommand truncateCmd = new SqlCommand(truncateQuery, connection);
                        await truncateCmd.ExecuteNonQueryAsync();
                        Log.Information("Table {TableName} truncated successfully.", tableName);
                        
                        // Inside the 'if (tableExists)' block:
                        foreach (DataColumn col in dataTable.Columns)
                        {
                            string addColQuery = $@"
                                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[{tableName}]') AND name = '{col.ColumnName}')
                                    ALTER TABLE [{tableName}] ADD [{col.ColumnName}] {GetSqlType(col.DataType)} NULL";
    
                            await using SqlCommand addColCmd = new SqlCommand(addColQuery, connection);
                            await addColCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // If Truncate fails (e.g., due to Foreign Keys), fallback to DELETE
                        Log.Warning("Truncate failed for {TableName}, attempting DELETE. Error: {Msg}", tableName, ex.Message);
                        string deleteQuery = $"DELETE FROM [{tableName}]";
                        await using SqlCommand deleteCmd = new SqlCommand(deleteQuery, connection);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    // 2. Table doesn't exist - Create it from scratch
                    var columnDefinitions = new List<string>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        string sqlType = GetSqlType(column.DataType);
                        columnDefinitions.Add($"[{column.ColumnName}] {sqlType} NULL");
                    }

                    string createTableQuery = $"CREATE TABLE [{tableName}] ({string.Join(", ", columnDefinitions)});";

                    try
                    {
                        await using SqlCommand createCmd = new SqlCommand(createTableQuery, connection);
                        await createCmd.ExecuteNonQueryAsync();
                        Log.Information("Table {TableName} created successfully.", tableName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to create table {TableName}", tableName);
                    }
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

        // private async Task InsertDataIntoSqliteServerAsync(DataTable? dataTable, string tableName, string connectionString)
        // {
        //     if (dataTable == null || dataTable.Rows.Count == 0) return;
        //
        //     foreach (DataColumn col in dataTable.Columns)
        //     {
        //         // Decode %20 and replace other problematic characters with underscores
        //         string cleanName = WebUtility.UrlDecode(col.ColumnName);
        //         cleanName = Regex.Replace(cleanName, @"[.\-\s%]", "_");
        //         col.ColumnName = cleanName;
        //     }
        //
        //     tableName = Regex.Replace(tableName, @"[.\-\s]", "_").Replace(".csv", "");
        //
        //     await using var connection = new SqliteConnection(connectionString);
        //     await connection.OpenAsync();
        //
        //     // Ensure this method exists and handles SQLite types correctly
        //     await CreateSqliteDynamicTableIfNotExists(connection, tableName, dataTable);
        //
        //     await using var transaction = await connection.BeginTransactionAsync();
        //     try
        //     {
        //         // We use a dictionary or parallel arrays to ensure the @ParamName in SQL matches the C# Add() call exactly.
        //         var columns = dataTable.Columns.Cast<DataColumn>().ToList();
        //
        //         var colNamesSql = string.Join(", ", columns.Select(c => $"[{c.ColumnName.Replace("_", "")}]"));
        //
        //         // Sanitize parameter names to remove spaces/symbols, ensuring they are valid SQL variables
        //         var paramNamesList = columns.Select(c => $"@param_{Regex.Replace(c.ColumnName, @"\W", "")}").ToList();
        //         var paramNamesSql = string.Join(", ", paramNamesList);
        //
        //         var insertSql = $"INSERT INTO [{tableName}] ({colNamesSql}) VALUES ({paramNamesSql})";
        //         Console.WriteLine(insertSql);
        //
        //         await using var command = new SqliteCommand(insertSql, connection, (SqliteTransaction)transaction);
        //
        //         var sqliteParameters = new List<SqliteParameter>();
        //         for (int i = 0; i < columns.Count; i++)
        //         {
        //             // Create the parameter with the specific name used in the SQL string
        //             var param = command.Parameters.AddWithValue(paramNamesList[i], DBNull.Value);
        //             sqliteParameters.Add(param);
        //         }
        //
        //         foreach (DataRow row in dataTable.Rows)
        //         {
        //             for (int i = 0; i < columns.Count; i++)
        //             {
        //                 // Handle nulls: if the logic is null, send DBNull.Value
        //                 var value = row[columns[i]];
        //                 sqliteParameters[i].Value = value ?? DBNull.Value;
        //             }
        //
        //             await command.ExecuteNonQueryAsync();
        //         }
        //
        //         await transaction.CommitAsync();
        //         //Log($"Successfully inserted {dataTable.Rows.Count} rows into SQLite.");
        //         
        //     }
        //     catch (Exception )
        //     {
        //         await transaction.RollbackAsync();
        //         //Log($"SQLite Insert Error: {ex.Message}");
        //     }
        // }
         public async Task InsertDataIntoSqliteServerAsync(DataTable? dataTable, string tableName)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return;
            
            // Clean Column Names
            foreach (DataColumn col in dataTable.Columns)
            {
                string decodedName = WebUtility.UrlDecode(col.ColumnName);
                col.ColumnName = Regex.Replace(decodedName, @"[^a-zA-Z0-9_]", "_");
            }
            
            // Sanitize Table Name
            tableName = Regex.Replace(tableName, @"[^a-zA-Z0-9_]", "_");

            await using var connection = new SqliteConnection(Connectionstring);
            await connection.OpenAsync();

            await CreateSqliteDynamicTableIfNotExists(connection, tableName, dataTable);

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var columns = dataTable.Columns.Cast<DataColumn>().ToList();
                var safeColNames = columns.Select(c => $"[{Regex.Replace(c.ColumnName, @"[^a-zA-Z0-9_]", "")}]");
                var paramNames = columns.Select((c, i) => $"@p{i}");

                var insertSql = $"INSERT INTO [{tableName}] ({string.Join(",", safeColNames)}) VALUES ({string.Join(",", paramNames)})";

                await using var command = new SqliteCommand(insertSql, connection, (SqliteTransaction)transaction);

                // Pre-create parameters for performance
                var sqliteParams = new List<SqliteParameter>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var param = new SqliteParameter($"@p{i}", DBNull.Value);
                    command.Parameters.Add(param);
                    sqliteParams.Add(param);
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        sqliteParams[i].Value = row[i] ?? DBNull.Value;
                    }
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                Log.Information("Inserted {Count} rows into SQLite table {Table}", dataTable.Rows.Count, tableName);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Log.Error(ex, "Error during SQLite bulk insert.");
            }
        }
    }
}
