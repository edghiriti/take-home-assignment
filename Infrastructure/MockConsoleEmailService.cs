namespace StripeOnboardingSlice.Infrastructure;

public class MockConsoleEmailService : IEmailService
{
    private readonly ILogger<MockConsoleEmailService> _logger;

    public MockConsoleEmailService(ILogger<MockConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPaymentSuccessEmailAsync(string toEmail, string companyName)
    {
        _logger.LogWarning($"EMAIL SENT TO: {toEmail} | Subject: Welcome to TEST, {companyName}! | Body: Your payment was successful. Your account is now Active.");

        return Task.CompletedTask;
    }

    public Task SendCheckoutExpiredEmailAsync(string toEmail, string companyName)
    {
        _logger.LogWarning($"EMAIL SENT TO: {toEmail} | Subject: Trouble checking out, {companyName}? | Body: We noticed you didn't finish setting up your TEST account. Reply to this email if you need help!");

        return Task.CompletedTask;
    }
}