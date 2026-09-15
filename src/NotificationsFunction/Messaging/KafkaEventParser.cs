using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotificationsFunction.Messaging;

// Two-step deserialization: the Kafka envelope first, then the event carried in envelope.Value.
// Malformed input is logged as a warning and yields null so the caller skips the message.
// The Kafka extension commits the offset either way (CommitOnFailure) and there is no retry
// policy by design: same at-least-once / skip-poison semantics as the Phase 2 consumers.
public static class KafkaEventParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static TEvent? Parse<TEvent>(string kafkaEvent, ILogger logger) where TEvent : class
    {
        KafkaEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<KafkaEnvelope>(kafkaEvent, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed Kafka envelope; skipping message.");
            return null;
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.Value))
        {
            logger.LogWarning("Kafka envelope without Value; skipping message (offset {Offset}).", envelope?.Offset);
            return null;
        }

        try
        {
            var evt = JsonSerializer.Deserialize<TEvent>(envelope.Value, JsonOptions);
            if (evt is null)
                logger.LogWarning("Malformed {EventType}; skipping message (offset {Offset}).", typeof(TEvent).Name, envelope.Offset);
            return evt;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed {EventType}; skipping message (offset {Offset}).", typeof(TEvent).Name, envelope.Offset);
            return null;
        }
    }
}
