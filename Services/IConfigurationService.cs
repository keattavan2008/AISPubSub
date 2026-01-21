namespace AISPubSub.Services
{
    public interface IConfigurationService
    {
        string Host { get; set; }
        string? AccessToken { get; set; }
        string SelectedDataSource { get; set; }
        string SelectedAcknowledgementId { get; set; }
        string SelectedTableName { get; set; }
        string GetConnectionString(string serverInstance, string database);
    }
}
