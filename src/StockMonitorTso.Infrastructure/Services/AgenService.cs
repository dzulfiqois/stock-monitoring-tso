using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

/// <summary>
/// Manajemen identitas Agen (keputusan user 2026-08, amend STOCK §3):
/// Create/Update = Superadmin + Supervisi; Delete = Superadmin only (konsisten aturan Delete global).
/// Membuat agen otomatis membuat baris stok per produk (stok 0, DOT 0) agar halaman inventaris
/// selalu punya baris; angka stok tetap diisi lewat transaksi stok (Invarian Konservasi, STOCK §2.c).
/// </summary>
public sealed class AgenService(
    ApplicationDbContext db,
    IAuditLogService auditLog,
    IStockWriteService stockWrite) : IAgenService
{
    public async Task<Agen> CreateAsync(ClaimsPrincipal actor, CreateAgenRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var nama = NormalizeNama(request.Nama);
        await EnsureNamaUniqueAsync(request.Wilayah, nama, null, ct);

        var agen = new Agen
        {
            Nama = nama,
            Wilayah = request.Wilayah,
            TanggalDaftar = DateTime.Today,
            Keterangan = request.Keterangan,
        };

        foreach (var produk in ProdukInfo.All)
        {
            agen.StokEntitas.Add(new StokEntitas
            {
                Wilayah = request.Wilayah,
                Produk = produk,
                Tier = Tier.Agen,
                TanggalStokAwal = DateTime.Today,
                Stok = 0m,
                DOT = 0m,
            });
        }

        db.Agen.Add(agen);
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "CreateAgen",
            EntityType = "Agen",
            EntityId = agen.Id.ToString(),
            After = $"{request.Wilayah}|{nama}",
            Detail = request.Keterangan,
        }, ct);

        return agen;
    }

    public async Task<Agen> UpdateAsync(ClaimsPrincipal actor, int agenId, UpdateAgenRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var agen = await GetActiveAgenAsync(agenId, ct);
        var nama = NormalizeNama(request.Nama);
        await EnsureNamaUniqueAsync(agen.Wilayah, nama, agenId, ct);

        var before = agen.Nama;
        agen.Nama = nama;
        agen.Keterangan = request.Keterangan;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateAgen",
            EntityType = "Agen",
            EntityId = agen.Id.ToString(),
            Before = before,
            After = nama,
        }, ct);

        return agen;
    }

    public async Task DeleteAsync(ClaimsPrincipal actor, int agenId, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin");

        var agen = await GetActiveAgenAsync(agenId, ct);
        var stokRows = await db.StokEntitas.Where(e => e.AgenId == agenId).ToListAsync(ct);
        foreach (var row in stokRows)
        {
            row.IsDeleted = true;
        }

        agen.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "DeleteAgen",
            EntityType = "Agen",
            EntityId = agen.Id.ToString(),
            After = $"{agen.Wilayah}|{agen.Nama}",
        }, ct);
    }

    private async Task<Agen> GetActiveAgenAsync(int agenId, CancellationToken ct)
        => await db.Agen.FirstOrDefaultAsync(a => a.Id == agenId && !a.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Agen tidak ditemukan.");

    public async Task TransferFromWarehouseAsync(
        ClaimsPrincipal actor,
        Wilayah wilayah,
        int agenId,
        IReadOnlyDictionary<Produk, decimal> qtyPerProduk,
        string? catatan = null,
        CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var agen = await GetActiveAgenAsync(agenId, ct);
        if (agen.Wilayah != wilayah)
        {
            throw new InvalidOperationException("Agen tidak berada di Gudang Wilayah tujuan transfer.");
        }

        var transfers = qtyPerProduk
            .Where(kv => kv.Value > 0)
            .ToList();
        if (transfers.Count == 0)
        {
            throw new ArgumentException("Tidak ada kuantitas transfer yang valid (> 0).");
        }

        foreach (var (produk, qty) in transfers)
        {
            var sumberId = await ResolveWarehouseEntityIdAsync(wilayah, produk, ct);
            var tujuanId = await ResolveAgenEntityIdAsync(agenId, produk, ct);
            await stockWrite.TransactAsync(actor, sumberId, StockTransactionType.Transfer, qty, tujuanId, catatan, ct);
        }
    }

    private async Task<int> ResolveWarehouseEntityIdAsync(Wilayah wilayah, Produk produk, CancellationToken ct)
        => await db.StokEntitas
            .Where(e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.GudangWilayah && !e.IsDeleted)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Gudang Wilayah {wilayah}/{produk} tidak ditemukan.");

    private async Task<int> ResolveAgenEntityIdAsync(int agenId, Produk produk, CancellationToken ct)
        => await db.StokEntitas
            .Where(e => e.AgenId == agenId && e.Produk == produk && e.Tier == Tier.Agen && !e.IsDeleted)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Baris stok agen (Agen {agenId}) untuk {produk} tidak ditemukan.");

    private async Task EnsureNamaUniqueAsync(Wilayah wilayah, string nama, int? excludeId, CancellationToken ct)
    {
        var duplicated = await db.Agen.AnyAsync(
            a => a.Wilayah == wilayah
                && a.Nama.ToLower() == nama.ToLower()
                && !a.IsDeleted
                && (excludeId == null || a.Id != excludeId.Value),
            ct);
        if (duplicated)
        {
            throw new InvalidOperationException($"Agen bernama '{nama}' sudah terdaftar di wilayah {wilayah.DisplayName()}.");
        }
    }

    private static string NormalizeNama(string nama)
    {
        var trimmed = nama.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Nama agen wajib diisi.");
        }

        return trimmed;
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
