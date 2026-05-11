using Amazon.DynamoDBv2.DataModel;

namespace StripeOnboardingSlice.Data;

[DynamoDBTable("ProcessedWebhooks")]
public class DynamoDbProcessedWebhook
{
    [DynamoDBHashKey]
    public string EventId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime ProcessedAt { get; set; }

    [DynamoDBProperty]
    public long ExpiresAtEpoch { get; set; }
}
