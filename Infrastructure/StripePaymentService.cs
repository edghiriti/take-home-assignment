using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace StripeOnboardingSlice.Infrastructure;

public class StripePaymentService : IPaymentService
{
    private readonly SessionService _sessionService;
    private readonly StripeOptions _options;

    public StripePaymentService(SessionService sessionService, IOptions<StripeOptions> options)
    {
        _sessionService = sessionService;
        _options = options.Value;
    }

    public async Task<string> CreateCheckoutSessionAsync(string clientReferenceId, string customerEmail, CancellationToken cancellationToken)
    {
        var options = new SessionCreateOptions
        {
            CustomerEmail = customerEmail,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = _options.DefaultPriceAmount,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Subscription",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            ClientReferenceId = clientReferenceId
        };

        var session = await _sessionService.CreateAsync(options, null, cancellationToken);

        return session.Url;
    }
}