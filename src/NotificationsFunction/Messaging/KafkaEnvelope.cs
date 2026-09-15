namespace NotificationsFunction.Messaging;

// Payload shape the Kafka extension hands to the isolated worker.
// The event JSON published by the services travels as a string in Value; Headers are ignored.
public sealed record KafkaEnvelope(
    long Offset,
    int Partition,
    string? Topic,
    DateTime? Timestamp,
    string? Value,
    string? Key);
