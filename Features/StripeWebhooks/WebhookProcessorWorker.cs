using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StripeWebhooks;

public class WebhookProcessorWorker : BackgroundService
{
    private readonly IWebhookQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookProcessorWorker> _logger;

    public WebhookProcessorWorker(
        IWebhookQueue queue, 
        IServiceScopeFactory scopeFactory, 
        ILogger<WebhookProcessorWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Background Processor is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queue.DequeueAsync(stoppingToken);

                if (message == null || string.IsNullOrWhiteSpace(message.RawJson))
                {
                    _logger.LogWarning("Received a Poison Pill message from SQS (missing RawJson). Skipping.");
                    continue;
                }

                var stripeEvent = Stripe.EventUtility.ParseEvent(message.RawJson);

                using var scope = _scopeFactory.CreateScope();
                
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var successHandler = scope.ServiceProvider.GetRequiredService<PaymentSucceededHandler>();
                    var session = stripeEvent.Data.Object as Session;
                    await successHandler.HandleAsync(session, message.EventId, stoppingToken);
                }
                else if (stripeEvent.Type == EventTypes.CheckoutSessionExpired)
                {
                    var expiredHandler = scope.ServiceProvider.GetRequiredService<SessionExpiredHandler>();
                    var session = stripeEvent.Data.Object as Session;
                    await expiredHandler.HandleAsync(session, message.EventId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A critical error occurred while processing a queued webhook.");
            }
        }
    }
}
