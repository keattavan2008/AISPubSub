using AVEVA.IntegrationService.DataAPI.SDK;
using AVEVA.IntegrationService.DataAPI.SDK.ApiClient;
using AVEVA.IntegrationService.DataAPI.SDK.Event;
using Serilog;

namespace AISPubSub.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private SignalRPubSubClient? _signalRPubSubClient;
        private string? _currentDataSource;
        private readonly HubConnectionManager _hubConnectionManager;

        public SubscriptionService()
        {
            _hubConnectionManager = new HubConnectionManager();
        }

        public event EventHandler<PubSubMessageEventArgs>? MessagePublished;

        public bool IsSubscribed => _signalRPubSubClient != null && !string.IsNullOrEmpty(_currentDataSource);

        public async Task<bool> SubscribeAsync(string dataSource, string host, string? accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(dataSource))
                {
                    Log.Warning("Data Source is empty, cannot subscribe");
                    return false;
                }

                host = host.TrimEnd('/') + "/";

                // Unsubscribe from previous subscription if exists
                if (_signalRPubSubClient != null && !string.IsNullOrEmpty(_currentDataSource))
                {
                    await UnsubscribeAsync(_currentDataSource);
                }

                // Create new client if needed
                if (_signalRPubSubClient == null)
                {
                    _signalRPubSubClient = await SignalRHubConnectionFactory.CreatePubSubClient(
                        null,
                        _hubConnectionManager,
                        !string.IsNullOrEmpty(accessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM,
                        host,
                        token: accessToken);

                    Log.Information("Pub sub client created");
                }

                // Unsubscribe from the data source first (in case of re-subscription)
                await _signalRPubSubClient.Unsubscribe(dataSource);
                _signalRPubSubClient.MessagePublished -= OnMessagePublished;

                // Subscribe to the new data source
                await _signalRPubSubClient.Subscribe(dataSource);
                _currentDataSource = dataSource;

                // Attach event handler
                _signalRPubSubClient.MessagePublished += OnMessagePublished;

                Log.Information($"Pub sub client subscribed to {dataSource}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error subscribing to data source {DataSource}: {Error}", dataSource, ex.Message);
                return false;
            }
        }

        public async Task<bool> UnsubscribeAsync(string dataSource)
        {
            try
            {
                if (_signalRPubSubClient == null || string.IsNullOrEmpty(dataSource))
                {
                    return false;
                }

                _signalRPubSubClient.MessagePublished -= OnMessagePublished;
                await _signalRPubSubClient.Unsubscribe(dataSource);

                if (_currentDataSource == dataSource)
                {
                    _currentDataSource = null;
                }

                Log.Information($"Pub sub client unsubscribed from {dataSource}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error unsubscribing from data source {DataSource}: {Error}", dataSource, ex.Message);
                return false;
            }
        }

        private void OnMessagePublished(object? sender, PubSubMessageEventArgs e)
        {
            MessagePublished?.Invoke(sender, e);
        }

        public void Dispose()
        {
            if (_signalRPubSubClient != null && !string.IsNullOrEmpty(_currentDataSource))
            {
                try
                {
                    UnsubscribeAsync(_currentDataSource).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    Log.Warning("Error during unsubscribe on dispose: {Error}", ex.Message);
                }
            }

            // Note: SDK clients may not implement IDisposable
            // They will be garbage collected when no longer referenced
            _signalRPubSubClient = null;
            // _hubConnectionManager is readonly and will be GC'd
        }
    }
}
