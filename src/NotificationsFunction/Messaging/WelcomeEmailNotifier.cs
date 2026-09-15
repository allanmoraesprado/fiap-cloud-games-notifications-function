using Microsoft.Extensions.Logging;
using NotificationsFunction.Contracts;

namespace NotificationsFunction.Messaging;

// Simulates sending a welcome e-mail by logging it. No SMTP / real provider (academic MVP).
// Same wording as the Phase 2 NotificationsAPI so demos and docs stay recognizable.
public class WelcomeEmailNotifier
{
    private readonly ILogger<WelcomeEmailNotifier> _logger;
    public WelcomeEmailNotifier(ILogger<WelcomeEmailNotifier> logger) => _logger = logger;

    public static string Format(UserCreatedEvent evt) =>
        $"[WELCOME EMAIL] To: {evt.Email} | Subject: Welcome to FIAP Cloud Games, {evt.Name}! " +
        $"| Body: Your account (id {evt.UserId}) was created successfully.";

    public void Send(UserCreatedEvent evt) => _logger.LogInformation("{EmailMessage}", Format(evt));
}
