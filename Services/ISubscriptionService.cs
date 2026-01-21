using AVEVA.IntegrationService.DataAPI.SDK.Event;

namespace AISPubSub.Services
{
    public interface ISubscriptionService : IDisposable
    {
        event EventHandler<PubSubMessageEventArgs>? MessagePublished;
        Task<bool> SubscribeAsync(string dataSource, string host, string? accessToken);
        Task<bool> UnsubscribeAsync(string dataSource);
        bool IsSubscribed { get; }
    }
}
