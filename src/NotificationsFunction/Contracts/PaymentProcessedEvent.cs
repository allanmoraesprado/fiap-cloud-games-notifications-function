namespace NotificationsFunction.Contracts;

// Mirrors the canonical contract in fiap-cloud-games-orchestration/contracts/README.md.
public record PaymentProcessedEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Status,        // "Approved" | "Rejected"
    DateTime OccurredAt);
