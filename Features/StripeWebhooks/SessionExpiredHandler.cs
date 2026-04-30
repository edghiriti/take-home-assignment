using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using StripeOnboardingSlice.Data;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StripeWebhooks;

public class SessionExpiredHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<SessionExpiredHandler> _logger;

    public SessionExpiredHandler(
        AppDbContext dbContext,
        IEmailService emailService,
        ILogger<SessionExpiredHandler> logger)
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
                _logger.LogWarning("Expired Webhook received without a valid ClientReferenceId. Ignoring.");
                return;
            }

            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

            if (company != null && company.Status == "PendingPayment")
            {
                company.Status = "Abandoned";

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

            _logger.LogInformation("Marked Company {CompanyId} as Abandoned.", companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Expired Webhook {EventId}. Rolling back.", eventId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (!string.IsNullOrEmpty(companyEmail))
        {
            try
            {
                await _emailService.SendCheckoutExpiredEmailAsync(companyEmail, companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send abandoned checkout email to {Email}", companyEmail);
            }
        }
    }
}