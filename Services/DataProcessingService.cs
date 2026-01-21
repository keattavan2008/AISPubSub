using System.Data;
using AISPubSub.Infrastructure.Api;
using AISPubSub.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Serilog;

namespace AISPubSub.Services
{
    public class DataProcessingService : IDataProcessingService
    {
        private readonly DataAccess _dataAccess;
        private readonly ApiService _apiService;
        private readonly IAisClientService _aisClientService;

        public DataProcessingService(
            DataAccess dataAccess,
            ApiService apiService,
            IAisClientService aisClientService)
        {
            _dataAccess = dataAccess;
            _apiService = apiService;
            _aisClientService = aisClientService;
        }

        public async Task ProcessAndSaveDataAsync(
            DataTable? dataTable,
            string tableName,
            string databaseType,
            string? serverInstance = null,
            string? database = null)
        {
            try
            {
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    Log.Warning("DataTable is null or empty, skipping save operation");
                    return;
                }

                if (databaseType.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    await _dataAccess.InsertDataIntoSqliteServerAsync(dataTable, tableName);
                }
                else if (databaseType.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(serverInstance) || string.IsNullOrEmpty(database))
                    {
                        Log.Error("SQL Server connection details are missing");
                        return;
                    }

                    var builder = new SqlConnectionStringBuilder
                    {
                        ConnectionString = $"Server={serverInstance};Database={database};Trusted_Connection=True;TrustServerCertificate=True;"
                    };

                    await _dataAccess.InsertDataIntoSqlServerAsync(dataTable, tableName, builder);
                }
                else
                {
                    Log.Warning("Unknown database type: {DatabaseType}", databaseType);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error processing and saving data: {Error}", ex.Message);
                throw;
            }
        }

        public async Task<DataTable?> ProcessEngineeringTablesAsync(string dataSource, string? accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(dataSource))
                {
                    Log.Warning("Data Source is empty");
                    return null;
                }

                // POST to datarequest to get acknowledgement ID
                string ackId = await _apiService.GetAcknowledgementIdAsync(dataSource, accessToken ?? string.Empty);
                
                if (string.IsNullOrEmpty(ackId))
                {
                    Log.Warning("Failed to get Acknowledgement ID");
                    return null;
                }

                // Wait a bit for the data to be ready
                await Task.Delay(3000);

                Log.Debug("Acknowledgement ID: {AckId}", ackId);

                // Get table data using the acknowledgement ID
                var tableList = await _aisClientService.GetTableDataByAcknowledgementIdAsync(
                    dataSource,
                    ackId,
                    accessToken);

                return tableList;
            }
            catch (Exception ex)
            {
                Log.Error("Error processing engineering tables: {Error}", ex.Message);
                return null;
            }
        }
    }
}
