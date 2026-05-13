using StripeOnboardingSlice.Data;
using StripeOnboardingSlice.Enums;
using StripeOnboardingSlice.Infrastructure;

namespace StripeOnboardingSlice.Features.StartOnboarding;

public class StartOnboardingHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IPaymentService _paymentService;

    public StartOnboardingHandler(AppDbContext dbContext, IPaymentService paymentService)
    {
        _dbContext = dbContext;
        _paymentService = paymentService;
    }

    public async Task<string> HandleAsync(StartOnboardingRequest request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            AdminEmail = request.AdminEmail,
            Status = CompanyStatus.PendingPayment
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
            clientReferenceId: company.Id.ToString(),
            customerEmail: company.AdminEmail,
            cancellationToken: cancellationToken
        );

        return checkoutUrl;
    }
}