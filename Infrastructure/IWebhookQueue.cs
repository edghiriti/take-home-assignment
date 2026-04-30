using Stripe;

namespace StripeOnboardingSlice.Infrastructure;

public interface IWebhookQueue
{
    ValueTask QueueWebhookAsync(WebhookMessage message, CancellationToken cancellationToken);
    ValueTask<WebhookMessage> DequeueAsync(CancellationToken cancellationToken);
}