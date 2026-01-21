using System.Data;
using AVEVA.IntegrationService.DataAPI.SDK.Models;

namespace AISPubSub.Services
{
    public interface IAisClientService : IDisposable
    {
        Task<bool> HealthCheckAsync(string host, string? accessToken);
        Task<List<string>> GetDataSourcesAsync(string host, string? accessToken);
        Task<DataTable?> GetTableDataByAcknowledgementIdAsync(string dataSource, string acknowledgementId, string? accessToken);
        Task<List<Acknowledgement>> GetAcknowledgementsAsync(string dataSource, string? accessToken);
        Task<bool> DeleteAcknowledgementsAsync(string dataSource, List<string> acknowledgementIds, string? accessToken);
        void InitializeClient(string host, string? accessToken);
    }
}
