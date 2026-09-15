using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Messaging;
using Xunit;

namespace NotificationsFunction.Tests;

public class KafkaEventParserTests
{
    private readonly CapturingLogger<KafkaEventParserTests> _logger = new();

    [Fact]
    public void Parse_returns_the_event_carried_in_the_envelope_value()
    {
        var expected = new UserCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@fcg.com", DateTime.UtcNow);

        var evt = KafkaEventParser.Parse<UserCreatedEvent>(KafkaEnvelopes.WrapEvent(expected, offset: 7), _logger);

        evt.Should().Be(expected);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Parse_returns_null_and_warns_when_the_envelope_is_malformed()
    {
        var evt = KafkaEventParser.Parse<UserCreatedEvent>("{\"Offset\":1,\"Value\":", _logger);

        evt.Should().BeNull();
        _logger.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("Malformed Kafka envelope"));
    }

    [Fact]
    public void Parse_returns_null_and_warns_when_the_event_value_is_malformed()
    {
        var envelope = KafkaEnvelopes.Wrap("{\"EventId\":\"not-a-guid\",\"Name\":123", offset: 3);

        var evt = KafkaEventParser.Parse<UserCreatedEvent>(envelope, _logger);

        evt.Should().BeNull();
        _logger.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("Malformed UserCreatedEvent") && m.Contains("offset 3"));
    }

    [Fact]
    public void Parse_returns_null_and_warns_when_the_envelope_has_no_value()
    {
        var evt = KafkaEventParser.Parse<UserCreatedEvent>("{\"Offset\":5,\"Partition\":0,\"Topic\":\"t\"}", _logger);

        evt.Should().BeNull();
        _logger.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("without Value"));
    }
}
