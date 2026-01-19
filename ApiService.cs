using System.Net.Http.Headers;
using System.Net.Http.Json;
using Serilog;

namespace AISPubSub
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a JSON POST request with a Bearer Token.
        /// </summary>
        /// <typeparam name="T">The type of data being sent</typeparam>
        public async Task<bool> PostAcknowledgementAsync<T>(string endpoint, T payload, string bearerToken)
        {
            try
            {
                // Create the request message
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                
                // Attach the Bearer Token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                
                // Serialize the object to JSON
                request.Content = JsonContent.Create(payload);

                Log.Information("Posting acknowledgement to {Endpoint}...", endpoint);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Successfully posted. Status: {Code}", response.StatusCode);
                    return true;
                }

                // Log detailed error if it fails
                var errorBody = await response.Content.ReadAsStringAsync();
                Log.Warning("API returned error {Code}: {Body}", response.StatusCode, errorBody);
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error("Network error while posting: {Message}", httpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in ApiService");
                return false;
            }
        }
    }
}