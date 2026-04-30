using StripeOnboardingSlice.Infrastructure;
using System.Threading.Channels;

public class WebhookQueue : IWebhookQueue
{
    private readonly Channel<WebhookMessage> _queue;

    public WebhookQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<WebhookMessage>(options);
    }

    public async ValueTask QueueWebhookAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(message, cancellationToken);
    }

    public async ValueTask<WebhookMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}