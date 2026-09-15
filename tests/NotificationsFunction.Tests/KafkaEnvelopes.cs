using System.Text.Json;

namespace NotificationsFunction.Tests;

// Builds the JSON envelope the Kafka extension delivers to the worker (shape validated in P3-M0).
internal static class KafkaEnvelopes
{
    public static string Wrap(string value, long offset = 0, string? key = null, string topic = "test-topic") =>
        JsonSerializer.Serialize(new
        {
            Offset = offset,
            Partition = 0,
            Topic = topic,
            Timestamp = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc),
            Value = value,
            LeaderEpoch = 0,
            IsPartitionEOF = false,
            Key = key,
            Headers = Array.Empty<object>()
        });

    public static string WrapEvent<T>(T evt, long offset = 0, string? key = null) =>
        Wrap(JsonSerializer.Serialize(evt), offset, key);
}
