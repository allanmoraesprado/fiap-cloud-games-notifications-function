using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;
using NotificationsFunction.Functions;
using NotificationsFunction.Messaging;
using Xunit;

namespace NotificationsFunction.Tests;

public class PaymentProcessedNotificationTests
{
    private readonly CapturingLogger<PurchaseConfirmationNotifier> _notifierLog = new();
    private readonly CapturingLogger<PaymentProcessedNotification> _functionLog = new();
    private readonly PaymentProcessedNotification _function;

    public PaymentProcessedNotificationTests()
        => _function = new PaymentProcessedNotification(new PurchaseConfirmationNotifier(_notifierLog), _functionLog);

    private static PaymentProcessedEvent Payment(string status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 199.90m, status, DateTime.UtcNow);

    [Fact]
    public void Run_logs_a_purchase_confirmation_when_the_payment_is_approved()
    {
        var evt = Payment("Approved");

        _function.Run(KafkaEnvelopes.WrapEvent(evt, key: evt.OrderId.ToString()));

        _notifierLog.Messages(LogLevel.Information).Should().ContainSingle(m => m.Contains("[PURCHASE CONFIRMATION]") && m.Contains(evt.OrderId.ToString()));
        _functionLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Run_logs_information_and_sends_nothing_when_the_payment_is_rejected()
    {
        var evt = Payment("Rejected");

        _function.Run(KafkaEnvelopes.WrapEvent(evt));

        _notifierLog.Entries.Should().BeEmpty();
        _functionLog.Messages(LogLevel.Information).Should().ContainSingle(m => m.Contains("Rejected") && m.Contains(evt.OrderId.ToString()) && m.Contains("no confirmation"));
    }

    [Fact]
    public void Run_compares_the_status_case_insensitively()
    {
        _function.Run(KafkaEnvelopes.WrapEvent(Payment("approved")));

        _notifierLog.Messages(LogLevel.Information).Should().ContainSingle(m => m.Contains("[PURCHASE CONFIRMATION]"));
    }

    [Fact]
    public void Run_warns_and_skips_a_malformed_message_without_throwing()
    {
        var act = () => _function.Run(KafkaEnvelopes.Wrap("{\"OrderId\":\"nope\",\"Price\":\"free\"", offset: 4));

        act.Should().NotThrow();
        _functionLog.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("Malformed PaymentProcessedEvent"));
        _notifierLog.Entries.Should().BeEmpty();
    }
}
