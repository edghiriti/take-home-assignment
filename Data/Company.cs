using StripeOnboardingSlice.Enums;

namespace StripeOnboardingSlice.Data;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public CompanyStatus Status { get; set; } = CompanyStatus.PendingPayment;
}