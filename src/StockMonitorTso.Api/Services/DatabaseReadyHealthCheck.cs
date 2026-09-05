using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Api.Services;

/// <summary>
/// Readiness check: verifikasi database dapat dihubungi (SELECT 1).
/// /health = liveness (tanpa dependensi); /ready = readiness (dengan cek database).
/// </summary>
public sealed class DatabaseReadyHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("Database siap.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database tidak dapat dihubungi.", ex);
        }
    }
}
