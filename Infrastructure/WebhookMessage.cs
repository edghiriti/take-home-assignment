using Stripe;

public record WebhookMessage(Event StripeEvent, string EventId);