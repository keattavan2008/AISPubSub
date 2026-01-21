using System.Data;
using AVEVA.IntegrationService.DataAPI.SDK;
using AVEVA.IntegrationService.DataAPI.SDK.ApiClient;
using AVEVA.IntegrationService.DataAPI.SDK.Models;
using Serilog;

namespace AISPubSub.Services
{
    public class AisClientService : IAisClientService
    {
        private DataApiClient? _dataApiClient;
        private HealthCheckClient? _healthCheckClient;
        private string? _host;
        private string? _accessToken;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public AisClientService()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void InitializeClient(string host, string? accessToken)
        {
            _host = host.TrimEnd('/') + "/";
            _accessToken = accessToken;

            _dataApiClient = new DataApiClient(
                _host,
                !string.IsNullOrEmpty(_accessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM,
                waitingTimeInMinutesForLiveData: 10,
                _accessToken,
                _cancellationTokenSource);

            Log.Information("Data API Client initialized");
        }

        public async Task<bool> HealthCheckAsync(string host, string? accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(host))
                {
                    Log.Warning("Host URL is empty for health check");
                    return false;
                }

                _healthCheckClient = HealthCheckClientFactory.CreateHealthCheckHttpClient(host, accessToken);
                var apiHealthCheck = await _healthCheckClient.HealthCheckAPI(host);

                if (apiHealthCheck == System.Net.HttpStatusCode.OK)
                {
                    Log.Information("AIS Data API is Healthy");
                    return true;
                }

                Log.Warning("AIS Data API health check returned: {StatusCode}", apiHealthCheck);
                return false;
            }
            catch (Exception e)
            {
                Log.Fatal("Error during Health Check: {Error}", e.Message);
                return false;
            }
        }

        public async Task<List<string>> GetDataSourcesAsync(string host, string? accessToken)
        {
            try
            {
                if (_dataApiClient == null)
                {
                    InitializeClient(host, accessToken);
                }

                var res = await _dataApiClient!.GetDataSources(_accessToken);

                var names = res.Tables.Cast<DataTable>()
                    .SelectMany(t => t.AsEnumerable())
                    .Select(r => r.Field<string>("Name"))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .Select(n => n!)
                    .ToList();

                if (!names.Any())
                {
                    Log.Information("No Data Sources found or Access issue");
                    return new List<string>();
                }

                Log.Information($"Loaded {names.Count} Data Sources across {res.Tables.Count} categories.");
                return names;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting data sources: {Error}", ex.Message);
                return new List<string>();
            }
        }

        public async Task<DataTable?> GetTableDataByAcknowledgementIdAsync(string dataSource, string acknowledgementId, string? accessToken)
        {
            try
            {
                if (_dataApiClient == null)
                {
                    Log.Error("Data API Client is not initialized");
                    return null;
                }

                if (string.IsNullOrEmpty(acknowledgementId))
                {
                    Log.Information("Acknowledgement ID is empty");
                    return null;
                }

                Log.Information($"Fetching data for Acknowledgement ID: {acknowledgementId}");

                var tableResult = await _dataApiClient.GetTableDataByAcknowledgementId(
                    dataSource,
                    acknowledgementId,
                    !string.IsNullOrEmpty(accessToken) ? accessToken : null);

                Log.Information("Data fetch completed. Table: {TableName}, Rows: {RowCount}",
                    tableResult.TableName, tableResult.Rows.Count);

                return tableResult;
            }
            catch (Exception ex)
            {
                Log.Error("Error retrieving data by acknowledgement id: {Error}", ex.Message);
                return null;
            }
        }

        public async Task<List<Acknowledgement>> GetAcknowledgementsAsync(string dataSource, string? accessToken)
        {
            try
            {
                if (_dataApiClient == null)
                {
                    Log.Error("Data API Client is not initialized");
                    return new List<Acknowledgement>();
                }

                if (string.IsNullOrEmpty(dataSource))
                {
                    Log.Information("Data Source is empty");
                    return new List<Acknowledgement>();
                }

                var ackList = await _dataApiClient.GetAcknowledgements(
                    dataSource,
                    !string.IsNullOrEmpty(accessToken) ? accessToken : null);

                var acknowledgements = ackList?.ToList() ?? new List<Acknowledgement>();
                Log.Information($"Loaded {acknowledgements.Count} Acknowledgements.");

                return acknowledgements;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting acknowledgements: {Error}", ex.Message);
                return new List<Acknowledgement>();
            }
        }

        public async Task<bool> DeleteAcknowledgementsAsync(string dataSource, List<string> acknowledgementIds, string? accessToken)
        {
            try
            {
                if (_dataApiClient == null)
                {
                    Log.Error("Data API Client is not initialized");
                    return false;
                }

                Log.Information($"Deleting {acknowledgementIds.Count} Acknowledgements");

                var result = await _dataApiClient.DeleteAcknowledgements(
                    dataSource,
                    acknowledgementIds,
                    !string.IsNullOrEmpty(accessToken) ? accessToken : null);

                Log.Information($"Delete Acknowledgements Status: {result.StatusCode}");
                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Log.Error("Error deleting acknowledgements: {Error}", ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            // Note: SDK clients may not implement IDisposable
            // They will be garbage collected when no longer referenced
            _dataApiClient = null;
            _healthCheckClient = null;
        }
    }
}
