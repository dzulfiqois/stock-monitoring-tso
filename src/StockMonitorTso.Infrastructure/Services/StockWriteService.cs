using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

/// <summary>
/// Write service stok. Menegakkan Invarian Konservasi (STOCK §2.c): stok hanya berubah
/// lewat transaksi atomic debit-kredit; overdraft (stok &lt; 0) ditolak (G3/F4);
/// tiap mutasi tercatat di audit log.
/// RBAC: Create = Superadmin+Operator; Update = Superadmin+Supervisi; Delete = Superadmin only.
/// </summary>
public sealed class StockWriteService(
    ApplicationDbContext db,
    IAuditLogService auditLog) : IStockWriteService
{
    public async Task<StokEntitas> RegisterAsync(ClaimsPrincipal actor, RegisterStokRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Operator");

        var exists = await db.StokEntitas.AnyAsync(
            e => e.Wilayah == request.Wilayah && e.Produk == request.Produk && e.Tier == request.Tier && !e.IsDeleted,
            ct);
        if (exists)
        {
            throw new InvalidOperationException(
                $"Entitas {request.Wilayah} / {request.Produk} / {request.Tier} sudah terdaftar.");
        }

        var entity = new StokEntitas
        {
            Wilayah = request.Wilayah,
            Produk = request.Produk,
            Tier = request.Tier,
            TanggalStokAwal = request.TanggalStokAwal,
            Stok = request.Stok,
            DOT = request.DOT,
            StokHabisTerjual = request.StokHabisTerjual,
            StokIntransit = request.StokIntransit,
            Keterangan = request.Keterangan,
        };
        db.StokEntitas.Add(entity);
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "Register",
            EntityType = "StokEntitas",
            EntityId = entity.Id.ToString(),
            After = $"{request.Wilayah}|{request.Produk}|{request.Tier}|{request.Stok}",
        }, ct);

        return entity;
    }

    public async Task<StokEntitas> UpdateDetailAsync(ClaimsPrincipal actor, int entitasId, UpdateDetailRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var entity = await GetActiveEntityAsync(entitasId, ct);
        entity.DOT = request.DOT;
        entity.TanggalStokAwal = request.TanggalStokAwal;
        entity.StokHabisTerjual = request.StokHabisTerjual;
        entity.StokIntransit = request.StokIntransit;
        entity.Keterangan = request.Keterangan;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateDetail",
            EntityType = "StokEntitas",
            EntityId = entity.Id.ToString(),
        }, ct);

        return entity;
    }

    public async Task<StokEntitas> TransactAsync(
        ClaimsPrincipal actor,
        int entitasId,
        StockTransactionType type,
        decimal kuantitas,
        int? tujuanId = null,
        string? catatan = null,
        CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");
        if (type == StockTransactionType.Adjust)
        {
            if (kuantitas == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(kuantitas), "Kuantitas opname tidak boleh 0.");
            }
        }
        else if (kuantitas <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kuantitas), "Kuantitas transaksi harus > 0.");
        }

        var entity = await GetActiveEntityAsync(entitasId, ct);
        var tujuan = type == StockTransactionType.Transfer
            ? await GetActiveEntityAsync(tujuanId!.Value, ct)
            : null;

        if (type == StockTransactionType.Transfer && tujuan!.Wilayah != entity.Wilayah)
        {
            throw new InvalidOperationException("Transfer hanya diperbolehkan antar-tier dalam wilayah yang sama.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var stokSumberSebelum = entity.Stok;
        switch (type)
        {
            case StockTransactionType.Receive:
                entity.Stok += kuantitas;
                break;

            case StockTransactionType.Issue:
                EnsureSufficientStock(entity, entity.Stok - kuantitas);
                entity.Stok -= kuantitas;
                entity.StokHabisTerjual = (entity.StokHabisTerjual ?? 0m) + kuantitas;
                break;

            case StockTransactionType.Adjust:
                entity.Stok += kuantitas;
                break;

            case StockTransactionType.Transfer:
                EnsureSufficientStock(entity, entity.Stok - kuantitas);
                var stokTujuanSebelum = tujuan!.Stok;
                entity.Stok -= kuantitas;
                tujuan.Stok += kuantitas;
                db.StockTransactions.Add(new StockTransactionRecord
                {
                    StokEntitasId = entity.Id,
                    StokEntitasTujuanId = tujuan.Id,
                    Type = type,
                    Kuantitas = kuantitas,
                    Tanggal = entity.TanggalStokAwal,
                    Catatan = catatan,
                    StokSumberSebelum = stokSumberSebelum,
                    StokSumberSesudah = entity.Stok,
                    StokTujuanSebelum = stokTujuanSebelum,
                    StokTujuanSesudah = tujuan.Stok,
                });
                break;

            default:
                throw new InvalidOperationException($"Tipe transaksi '{type}' tidak didukung.");
        }

        if (type != StockTransactionType.Transfer)
        {
            EnsureSufficientStock(entity, entity.Stok);
            db.StockTransactions.Add(new StockTransactionRecord
            {
                StokEntitasId = entity.Id,
                Type = type,
                Kuantitas = kuantitas,
                Tanggal = entity.TanggalStokAwal,
                Catatan = catatan,
                StokSumberSebelum = stokSumberSebelum,
                StokSumberSesudah = entity.Stok,
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = type.ToString(),
            EntityType = "StokEntitas",
            EntityId = entity.Id.ToString(),
            After = $"{type} {kuantitas}",
            Detail = catatan,
        }, ct);

        return entity;
    }

    public async Task DeleteAsync(ClaimsPrincipal actor, int entitasId, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin");

        var entity = await GetActiveEntityAsync(entitasId, ct);
        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "Delete",
            EntityType = "StokEntitas",
            EntityId = entity.Id.ToString(),
        }, ct);
    }

    private async Task<StokEntitas> GetActiveEntityAsync(int id, CancellationToken ct)
    {
        var entity = await db.StokEntitas.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Entitas stok tidak ditemukan.");
        return entity;
    }

    private static void EnsureSufficientStock(StokEntitas entity, decimal stok)
    {
        if (stok < 0)
        {
            throw new InvalidOperationException(
                $"Stok tidak mencukupi: {entity.Wilayah} / {entity.Produk} / {entity.Tier} hanya tersisa {entity.Stok}.");
        }
    }

    private static void RequireAnyRole(ClaimsPrincipal actor, params string[] roles)
    {
        if (!roles.Any(actor.IsInRole))
        {
            throw new UnauthorizedAccessException("Pihak tidak memiliki otoritas untuk aksi ini.");
        }
    }

    private static string? GetUserId(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.NameIdentifier);

    private static string? GetEmail(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.Email);

    private static string? GetActiveRole(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.Role);
}
