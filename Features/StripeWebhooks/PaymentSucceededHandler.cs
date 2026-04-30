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

    public PaymentSucceededHandler(
        AppDbContext dbContext,
        IEmailService emailService,
        ILogger<PaymentSucceededHandler> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(Session session, string eventId, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await _dbContext.ProcessedWebhooks
            .AnyAsync(w => w.EventId == eventId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Webhook {EventId} already processed. Skipping.", eventId);
            return;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        string companyEmail = string.Empty;
        string companyName = string.Empty;

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

            _dbContext.ProcessedWebhooks.Add(new ProcessedWebhook
            {
                EventId = eventId,
                ProcessedAt = DateTime.UtcNow
            });

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