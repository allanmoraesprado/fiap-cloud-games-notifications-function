namespace NotificationsFunction.Contracts;

// Mirrors the canonical contract in fiap-cloud-games-orchestration/contracts/README.md.
public record UserCreatedEvent(
    Guid EventId,
    Guid UserId,
    string Name,
    string Email,
    DateTime OccurredAt);
