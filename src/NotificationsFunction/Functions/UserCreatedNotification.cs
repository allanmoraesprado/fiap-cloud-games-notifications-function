using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Messaging;

namespace NotificationsFunction.Functions;

// Kafka-triggered function: consumes fcg.users.created (group notifications-function) and logs a
// simulated welcome e-mail. Replaces the Phase 2 UserCreatedConsumer (NotificationsAPI).
public class UserCreatedNotification
{
    private readonly WelcomeEmailNotifier _notifier;
    private readonly ILogger<UserCreatedNotification> _logger;

    public UserCreatedNotification(WelcomeEmailNotifier notifier, ILogger<UserCreatedNotification> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    // Broker, topic and consumer group come from the Kafka__* app settings (environment variables),
    // referenced with ':' inside the attribute ('%Kafka__X%' does not resolve; validated in P3-M0).
    // Protocol Plaintext is local-only; a cloud broker would use SaslSsl + credentials (documented, not implemented).
    [Function(nameof(UserCreatedNotification))]
    public void Run(
        [KafkaTrigger("%Kafka:BootstrapServers%", "%Kafka:UserCreatedTopic%",
            ConsumerGroup = "%Kafka:ConsumerGroup%",
            Protocol = BrokerProtocol.Plaintext)] string kafkaEvent)
    {
        var evt = KafkaEventParser.Parse<UserCreatedEvent>(kafkaEvent, _logger);
        if (evt is null) return;

        _notifier.Send(evt);
    }
}
