namespace StripeOnboardingSlice.Infrastructure;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(
        string clientReferenceId,
        string customerEmail,
        CancellationToken cancellationToken);
}