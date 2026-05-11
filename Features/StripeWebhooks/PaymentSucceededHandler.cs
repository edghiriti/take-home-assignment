using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using StripeOnboardingSlice.Data;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StripeWebhooks;

public class PaymentSucceededHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentSucceededHandler> _logger;
    private readonly IDynamoDBContext _dynamoDbContext;

    public PaymentSucceededHandler(
        AppDbContext dbContext,
        IEmailService emailService,
        ILogger<PaymentSucceededHandler> logger,
        IDynamoDBContext dynamoDBContext)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
        _dynamoDbContext = dynamoDBContext;
    }

    public async Task HandleAsync(Session session, string eventId, CancellationToken cancellationToken)
    {
        var existingWebhook = await _dynamoDbContext.LoadAsync<DynamoDbProcessedWebhook>(eventId, cancellationToken);

        if (existingWebhook != null)
        {
            _logger.LogInformation("Webhook {EventId} already processed. Skipping.", eventId);
            return;
        }

        string companyEmail = string.Empty;
        string companyName = string.Empty;

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!Guid.TryParse(session.ClientReferenceId, out var companyId))
            {
                _logger.LogError("Critical Error: Webhook received without a valid ClientReferenceId.");
                return;
            }

            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

            if (company != null)
            {
                company.Status = "Active";
                companyEmail = company.AdminEmail;
                companyName = company.Name;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully processed payment for Company: {CompanyId}", companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe Webhook {EventId}. Rolling back.", eventId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        try
        {
            var processedLog = new DynamoDbProcessedWebhook
            {
                EventId = eventId,
                ProcessedAt = DateTime.UtcNow,
                ExpiresAtEpoch = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
            };

            await _dynamoDbContext.SaveAsync(processedLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log webhook {EventId} to DynamoDB.", eventId);
            throw;
        }

        if (!string.IsNullOrEmpty(companyEmail))
        {
            try
            {
                await _emailService.SendPaymentSuccessEmailAsync(companyEmail, companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment succeeded, but failed to send welcome email to {Email}", companyEmail);
            }
        }
    }
}