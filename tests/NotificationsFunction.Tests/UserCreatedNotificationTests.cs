using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Functions;
using NotificationsFunction.Messaging;
using Xunit;

namespace NotificationsFunction.Tests;

public class UserCreatedNotificationTests
{
    private readonly CapturingLogger<WelcomeEmailNotifier> _notifierLog = new();
    private readonly CapturingLogger<UserCreatedNotification> _functionLog = new();
    private readonly UserCreatedNotification _function;

    public UserCreatedNotificationTests()
        => _function = new UserCreatedNotification(new WelcomeEmailNotifier(_notifierLog), _functionLog);

    [Fact]
    public void Run_logs_a_welcome_email_for_a_valid_event()
    {
        var evt = new UserCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@fcg.com", DateTime.UtcNow);

        _function.Run(KafkaEnvelopes.WrapEvent(evt, key: evt.UserId.ToString()));

        _notifierLog.Messages(LogLevel.Information).Should().ContainSingle(m => m.Contains("[WELCOME EMAIL]") && m.Contains("ana@fcg.com"));
        _functionLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Run_warns_and_skips_a_malformed_message_without_throwing()
    {
        var act = () => _function.Run(KafkaEnvelopes.Wrap("{\"EventId\":\"not-a-guid\",\"Name\":123,\"Email\":", offset: 9));

        act.Should().NotThrow();
        _functionLog.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("Malformed UserCreatedEvent"));
        _notifierLog.Entries.Should().BeEmpty();
    }
}
