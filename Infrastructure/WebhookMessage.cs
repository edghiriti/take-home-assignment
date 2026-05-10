using Stripe;

public record WebhookMessage(string RawJson, string EventId);