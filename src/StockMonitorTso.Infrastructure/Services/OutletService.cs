using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class OutletService(
    ApplicationDbContext db,
    IAuditLogService auditLog,
    IStockWriteService stockWrite) : IOutletService
{
    public async Task<Outlet> CreateAsync(ClaimsPrincipal actor, CreateOutletRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var nama = NormalizeNama(request.Nama);
        var agen = await GetActiveAgenAsync(request.AgenId, ct);
        await EnsureNamaUniqueAsync(request.AgenId, nama, null, ct);

        var outlet = new Outlet
        {
            Nama = nama,
            AgenId = agen.Id,
            Wilayah = agen.Wilayah,
            TanggalDaftar = DateTime.Today,
            Keterangan = request.Keterangan,
        };

        foreach (var produk in ProdukInfo.All)
        {
            outlet.StokEntitas.Add(new StokEntitas
            {
                Wilayah = agen.Wilayah,
                Produk = produk,
                Tier = Tier.Outlet,
                TanggalStokAwal = DateTime.Today,
                Stok = 0m,
                DOT = 0m,
            });
        }

        db.Outlet.Add(outlet);
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "CreateOutlet",
            EntityType = "Outlet",
            EntityId = outlet.Id.ToString(),
            After = $"{agen.Wilayah}|{agen.Nama}|{nama}",
            Detail = request.Keterangan,
        }, ct);

        return outlet;
    }

    public async Task<Outlet> UpdateAsync(ClaimsPrincipal actor, int outletId, UpdateOutletRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var outlet = await GetActiveOutletAsync(outletId, ct);
        var nama = NormalizeNama(request.Nama);
        await EnsureNamaUniqueAsync(outlet.AgenId, nama, outletId, ct);

        var before = outlet.Nama;
        outlet.Nama = nama;
        outlet.Keterangan = request.Keterangan;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateOutlet",
            EntityType = "Outlet",
            EntityId = outlet.Id.ToString(),
            Before = before,
            After = nama,
        }, ct);

        return outlet;
    }

    public async Task DeleteAsync(ClaimsPrincipal actor, int outletId, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin");

        var outlet = await GetActiveOutletAsync(outletId, ct);
        var stokRows = await db.StokEntitas.Where(e => e.OutletId == outletId).ToListAsync(ct);
        foreach (var row in stokRows)
        {
            row.IsDeleted = true;
        }

        outlet.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "DeleteOutlet",
            EntityType = "Outlet",
            EntityId = outlet.Id.ToString(),
            After = $"{outlet.Wilayah}|{outlet.Nama}",
        }, ct);
    }

    public async Task TransferFromAgenAsync(
        ClaimsPrincipal actor,
        int agenId,
        int outletId,
        IReadOnlyDictionary<Produk, decimal> qtyPerProduk,
        string? catatan = null,
        CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var outlet = await GetActiveOutletAsync(outletId, ct);
        if (outlet.AgenId != agenId)
        {
            throw new InvalidOperationException("Outlet tidak berada di Agen tujuan transfer.");
        }

        var transfers = qtyPerProduk.Where(kv => kv.Value > 0).ToList();
        if (transfers.Count == 0)
        {
            throw new ArgumentException("Tidak ada kuantitas transfer yang valid (> 0).");
        }

        foreach (var (produk, qty) in transfers)
        {
            var sumberId = await ResolveAgenEntityIdAsync(agenId, produk, ct);
            var tujuanId = await ResolveOutletEntityIdAsync(outletId, produk, ct);
            await stockWrite.TransactAsync(actor, sumberId, StockTransactionType.Transfer, qty, tujuanId, catatan, ct);
        }
    }

    private async Task<Agen> GetActiveAgenAsync(int agenId, CancellationToken ct)
        => await db.Agen.FirstOrDefaultAsync(a => a.Id == agenId && !a.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Agen tidak ditemukan.");

    private async Task<Outlet> GetActiveOutletAsync(int outletId, CancellationToken ct)
        => await db.Outlet.FirstOrDefaultAsync(o => o.Id == outletId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Outlet tidak ditemukan.");

    private async Task EnsureNamaUniqueAsync(int agenId, string nama, int? excludeId, CancellationToken ct)
    {
        var duplicated = await db.Outlet.AnyAsync(
            o => o.AgenId == agenId
                && o.Nama.ToLower() == nama.ToLower()
                && !o.IsDeleted
                && (excludeId == null || o.Id != excludeId.Value), ct);
        if (duplicated)
        {
            throw new InvalidOperationException($"Outlet bernama '{nama}' sudah terdaftar di agen tersebut.");
        }
    }

    private async Task<int> ResolveAgenEntityIdAsync(int agenId, Produk produk, CancellationToken ct)
        => await db.StokEntitas.Where(e => e.AgenId == agenId && e.Produk == produk && e.Tier == Tier.Agen && !e.IsDeleted)
            .Select(e => (int?)e.Id).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Baris stok agen (Agen {agenId}) untuk {produk} tidak ditemukan.");

    private async Task<int> ResolveOutletEntityIdAsync(int outletId, Produk produk, CancellationToken ct)
        => await db.StokEntitas.Where(e => e.OutletId == outletId && e.Produk == produk && e.Tier == Tier.Outlet && !e.IsDeleted)
            .Select(e => (int?)e.Id).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Baris stok outlet (Outlet {outletId}) untuk {produk} tidak ditemukan.");

    private static string NormalizeNama(string nama)
    {
        var trimmed = nama.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Nama outlet wajib diisi.");
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
