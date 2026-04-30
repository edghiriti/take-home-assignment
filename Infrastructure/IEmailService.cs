namespace StripeOnboardingSlice.Infrastructure;

public interface IEmailService
{
    Task SendPaymentSuccessEmailAsync(string toEmail, string companyName);
    Task SendCheckoutExpiredEmailAsync(string toEmail, string companyName);
}