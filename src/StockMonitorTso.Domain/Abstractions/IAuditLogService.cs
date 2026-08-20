using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Domain.Abstractions;

public interface IAuditLogService
{
    Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default);
}
