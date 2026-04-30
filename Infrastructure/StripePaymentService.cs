using Stripe.Checkout;

namespace StripeOnboardingSlice.Infrastructure;

public class StripePaymentService : IPaymentService
{
    private readonly SessionService _sessionService;

    public StripePaymentService(SessionService sessionService)
    {
        _sessionService = sessionService;
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
                        UnitAmount = 5000,
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
            SuccessUrl = "http://localhost:5001/success",
            CancelUrl = "http://localhost:5001/cancel",
            ClientReferenceId = clientReferenceId
        };

        var session = await _sessionService.CreateAsync(options, null, cancellationToken);

        return session.Url;
    }
}