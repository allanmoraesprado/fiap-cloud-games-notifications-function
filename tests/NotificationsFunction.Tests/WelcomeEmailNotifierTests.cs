using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Messaging;
using Xunit;

namespace NotificationsFunction.Tests;

public class WelcomeEmailNotifierTests
{
    [Fact]
    public void Format_includes_recipient_name_and_subject()
    {
        var evt = new UserCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "John", "john@fcg.com", DateTime.UtcNow);

        var message = WelcomeEmailNotifier.Format(evt);

        message.Should().Contain("john@fcg.com");
        message.Should().Contain("John");
        message.Should().Contain("WELCOME EMAIL");
    }

    [Fact]
    public void Send_logs_the_formatted_email_as_information()
    {
        var logger = new CapturingLogger<WelcomeEmailNotifier>();
        var evt = new UserCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "John", "john@fcg.com", DateTime.UtcNow);

        new WelcomeEmailNotifier(logger).Send(evt);

        logger.Messages(LogLevel.Information).Should().ContainSingle(m => m == WelcomeEmailNotifier.Format(evt));
    }
}
