using System.Data;

namespace AISPubSub.Services
{
    public interface IDataProcessingService
    {
        Task ProcessAndSaveDataAsync(DataTable? dataTable, string tableName, string databaseType, string? serverInstance = null, string? database = null);
        Task<DataTable?> ProcessEngineeringTablesAsync(string dataSource, string? accessToken);
    }
}
