using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using StripeOnboardingSlice.Infrastructure;

public class SqsWebhookQueue : IWebhookQueue
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsWebhookQueue(IAmazonSQS sqsClient, IConfiguration config)
    {
        _sqsClient = sqsClient;
        _queueUrl = config["AWS:SqsQueueUrl"]!;
    }

    public async ValueTask QueueWebhookAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = json
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }

    public async ValueTask<WebhookMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _sqsClient.ReceiveMessageAsync(request, cancellationToken);

            if (response?.Messages != null && response.Messages.Count > 0)
            {
                var msg = response.Messages[0];
                var webhook = JsonSerializer.Deserialize<WebhookMessage>(msg.Body);

                await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);

                return webhook!;
            }
        }

        throw new OperationCanceledException();
    }
}