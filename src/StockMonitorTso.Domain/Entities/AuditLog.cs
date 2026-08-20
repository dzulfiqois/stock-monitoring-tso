namespace StockMonitorTso.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string? ActorUserId { get; set; }

    public string? ActorEmail { get; set; }

    public string? ActorRole { get; set; }

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public string? EntityId { get; set; }

    public string? Before { get; set; }

    public string? After { get; set; }

    public string? Detail { get; set; }
}
