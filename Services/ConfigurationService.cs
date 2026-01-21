using Microsoft.Data.SqlClient;

namespace AISPubSub.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string Host { get; set; } = "https://eu.ais.connect.aveva.com/data";
        public string? AccessToken { get; set; } = Environment.GetEnvironmentVariable("AIS_ACCESS_TOKEN");
        public string SelectedDataSource { get; set; } = string.Empty;
        public string SelectedAcknowledgementId { get; set; } = string.Empty;
        public string SelectedTableName { get; set; } = string.Empty;

        public string GetConnectionString(string serverInstance, string database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverInstance,
                InitialCatalog = database,
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }
    }
}
