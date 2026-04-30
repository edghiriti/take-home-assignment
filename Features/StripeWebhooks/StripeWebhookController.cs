using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StripeWebhooks;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeOptions _options;
    private readonly PaymentSucceededHandler _paymentSucceededHandler;
    private readonly SessionExpiredHandler _sessionExpiredHandler;

    public StripeWebhookController(IOptions<StripeOptions> options, PaymentSucceededHandler paymentSucceededHandler, SessionExpiredHandler sessionExpiredHandler)
    {
        _options = options.Value;
        _paymentSucceededHandler = paymentSucceededHandler;
        _sessionExpiredHandler = sessionExpiredHandler;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _options.WebhookSecret
            );

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                await _paymentSucceededHandler.HandleAsync(session, stripeEvent.Id, CancellationToken.None);
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionExpired)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                await _sessionExpiredHandler.HandleAsync(session, stripeEvent.Id, CancellationToken.None);
            }

            return Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine($"[STRIPE WEBHOOK ERROR]: {e.Message}");
            return BadRequest();
        }
    }
}