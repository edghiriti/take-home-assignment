namespace StripeOnboardingSlice.Data;

public class ProcessedWebhook
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}