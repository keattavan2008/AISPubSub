using System.Net.Http.Headers;
using System.Net.Http.Json;
using Serilog;

namespace AISPubSub.Infrastructure.Api
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://eu.ais.connect.aveva.com/data/api/v1.1/datasources/Engineering/";

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<string> GetAcknowledgementIdAsync(string datasource, string bearerToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                // Body requirement: { "path": "/tables" }
                var body = new { path = "/tables" };

                Log.Information("Requesting Acknowledgement ID for All Tables...");
                
                _httpClient.BaseAddress = new Uri(BaseUrl + datasource + "/");
                
                var response = await _httpClient.PostAsJsonAsync("datarequest", body);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AcknowledgementResponse>();
                    Log.Information("Received AcknowledgementId: {Id}", result?.AcknowledgementId);
                    return result!.AcknowledgementId!;
                }

                string error = await response.Content.ReadAsStringAsync();
                Log.Error("Failed: {Status} - {Error}", response.StatusCode, error);
                return null!;
            }
            catch (Exception ex)
            {
                Log.Error("Exception during (POST):" + ex.Message);
                return null!;
            }
        }
        
    }
}