using Microsoft.AspNetCore.Mvc;
using Stripe;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StripeWebhooks;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IWebhookQueue _queue;
    private readonly string _webhookSecret;

    public StripeWebhookController(IWebhookQueue queue, IConfiguration config)
    {
        _queue = queue;
        _webhookSecret = config["Stripe:WebhookSecret"];
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _webhookSecret
            );

            var message = new WebhookMessage(stripeEvent, stripeEvent.Id);
            await _queue.QueueWebhookAsync(message, CancellationToken.None);

            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }
}