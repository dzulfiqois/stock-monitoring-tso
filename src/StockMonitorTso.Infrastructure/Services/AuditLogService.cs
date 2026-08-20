using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class AuditLogService(ApplicationDbContext db) : IAuditLogService
{
    public async Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        entry.Timestamp = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
