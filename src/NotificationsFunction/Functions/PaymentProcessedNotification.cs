using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Messaging;

namespace NotificationsFunction.Functions;

// Kafka-triggered function: consumes fcg.payments.processed (group notifications-function) and logs a
// purchase-confirmation e-mail only when the payment was Approved. Rejected payments are logged and
// intentionally produce no e-mail. Replaces the Phase 2 PaymentProcessedConsumer (NotificationsAPI).
// Distinct consumer group from CatalogAPI (catalog-service) => both receive every payment event (fan-out).
public class PaymentProcessedNotification
{
    private const string Approved = "Approved";

    private readonly PurchaseConfirmationNotifier _notifier;
    private readonly ILogger<PaymentProcessedNotification> _logger;

    public PaymentProcessedNotification(PurchaseConfirmationNotifier notifier, ILogger<PaymentProcessedNotification> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    [Function(nameof(PaymentProcessedNotification))]
    public void Run(
        [KafkaTrigger("%Kafka:BootstrapServers%", "%Kafka:PaymentProcessedTopic%",
            ConsumerGroup = "%Kafka:ConsumerGroup%",
            Protocol = BrokerProtocol.Plaintext)] string kafkaEvent)
    {
        var evt = KafkaEventParser.Parse<PaymentProcessedEvent>(kafkaEvent, _logger);
        if (evt is null) return;

        if (!string.Equals(evt.Status, Approved, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Payment {Status} for order {OrderId}; no confirmation e-mail sent.", evt.Status, evt.OrderId);
            return;
        }

        _notifier.Send(evt);
    }
}
