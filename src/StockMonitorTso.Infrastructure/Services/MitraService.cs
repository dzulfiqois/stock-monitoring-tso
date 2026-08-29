using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class MitraService(
    ApplicationDbContext db,
    IAuditLogService auditLog) : IMitraService
{
    private static readonly string[] AllowedSatuanMinyak = ["per_kiloliter", "per_kilometer"];
    private static readonly string[] AllowedSatuanTabung = ["per_tabung", "per_kilometer"];

    public async Task<MitraTso> CreateAsync(ClaimsPrincipal actor, CreateMitraRequest request, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);
        ValidateMitraFields(request.Nama, request.JenisKendaraan, request.KapasitasMax, request.SatuanKapasitas, request.Rute, request.AreaCoverage, request.Kontak, request.Pic);
        if (await db.MitraTso.AnyAsync(m => m.Id == request.Id, ct))
        {
            throw new InvalidOperationException($"Mitra dengan Id '{request.Id}' sudah terdaftar.");
        }

        if (request.Tarifs.Count == 0)
        {
            throw new ArgumentException("Tarif per jenis produk wajib diisi minimal 1.");
        }

        var mitra = new MitraTso
        {
            Id = request.Id.Trim(),
            Nama = request.Nama.Trim(),
            JenisKendaraan = request.JenisKendaraan.Trim(),
            KapasitasMax = request.KapasitasMax,
            SatuanKapasitas = request.SatuanKapasitas.Trim(),
            Rute = request.Rute,
            AreaCoverage = request.AreaCoverage,
            Kontak = request.Kontak.Trim(),
            Pic = request.Pic.Trim(),
            Active = request.Active,
            Tarif = request.Tarifs.First().Tarif,
            SatuanTarif = request.Tarifs.First().SatuanTarif,
        };

        foreach (var t in request.Tarifs)
        {
            ValidateTarif(t.Produk, t.Tarif, t.SatuanTarif);
            mitra.Tarifs.Add(new MitraTarif { MitraId = mitra.Id, Produk = t.Produk, Tarif = t.Tarif, SatuanTarif = t.SatuanTarif });
        }

        db.MitraTso.Add(mitra);
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "CreateMitra",
            EntityType = "MitraTso",
            EntityId = mitra.Id,
            After = mitra.Nama,
        }, ct);

        return mitra;
    }

    public async Task<MitraTso> UpdateAsync(ClaimsPrincipal actor, string mitraId, UpdateMitraRequest request, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);
        ValidateMitraFields(request.Nama, request.JenisKendaraan, request.KapasitasMax, request.SatuanKapasitas, request.Rute, request.AreaCoverage, request.Kontak, request.Pic);

        var mitra = await db.MitraTso.Include(m => m.Tarifs).FirstOrDefaultAsync(m => m.Id == mitraId, ct)
            ?? throw new KeyNotFoundException($"Mitra '{mitraId}' tidak ditemukan.");

        var before = $"{mitra.Nama}|{mitra.Tarif}|{string.Join(",", mitra.AreaCoverage)}";
        mitra.Nama = request.Nama.Trim();
        mitra.JenisKendaraan = request.JenisKendaraan.Trim();
        mitra.KapasitasMax = request.KapasitasMax;
        mitra.SatuanKapasitas = request.SatuanKapasitas.Trim();
        mitra.Rute = request.Rute;
        mitra.AreaCoverage = request.AreaCoverage;
        mitra.Kontak = request.Kontak.Trim();
        mitra.Pic = request.Pic.Trim();
        mitra.Active = request.Active;

        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateMitra",
            EntityType = "MitraTso",
            EntityId = mitra.Id,
            Before = before,
            After = $"{mitra.Nama}|{string.Join(",", mitra.AreaCoverage)}",
        }, ct);

        return mitra;
    }

    public async Task<MitraTso> UpdateTarifAsync(ClaimsPrincipal actor, string mitraId, UpdateMitraTarifRequest request, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);
        ValidateTarif(request.Produk, request.Tarif, request.SatuanTarif);

        var mitra = await db.MitraTso.Include(m => m.Tarifs).FirstOrDefaultAsync(m => m.Id == mitraId, ct)
            ?? throw new KeyNotFoundException($"Mitra '{mitraId}' tidak ditemukan.");

        var existing = mitra.Tarifs.FirstOrDefault(t => t.Produk == request.Produk);
        var before = existing is null ? "null" : $"{existing.Tarif}/{existing.SatuanTarif}";
        if (existing is null)
        {
            var tarif = new MitraTarif { MitraId = mitraId, Produk = request.Produk, Tarif = request.Tarif, SatuanTarif = request.SatuanTarif };
            db.Add(tarif);
            mitra.Tarifs.Add(tarif);
        }
        else
        {
            existing.Tarif = request.Tarif;
            existing.SatuanTarif = request.SatuanTarif;
        }

        // sync legacy single tarif for backward compat
        mitra.Tarif = request.Tarif;
        mitra.SatuanTarif = request.SatuanTarif;

        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateMitraTarif",
            EntityType = "MitraTarif",
            EntityId = mitraId,
            Before = before,
            After = $"{request.Produk}:{request.Tarif}/{request.SatuanTarif}",
        }, ct);

        return mitra;
    }

    public async Task<IReadOnlyList<MitraTso>> ListAsync(CancellationToken ct = default)
        => await db.MitraTso.Include(m => m.Tarifs).AsNoTracking().OrderBy(m => m.Nama).ToListAsync(ct);

    public async Task<MitraTso?> GetAsync(string mitraId, CancellationToken ct = default)
        => await db.MitraTso.Include(m => m.Tarifs).AsNoTracking().FirstOrDefaultAsync(m => m.Id == mitraId, ct);

    private static void ValidateMitraFields(string nama, string jenis, decimal kapasitas, string satuanKapasitas, string[] rute, string[] area, string kontak, string pic)
    {
        if (string.IsNullOrWhiteSpace(nama)) throw new ArgumentException("Nama Mitra wajib diisi.");
        if (string.IsNullOrWhiteSpace(jenis)) throw new ArgumentException("Jenis Kendaraan wajib diisi.");
        if (kapasitas <= 0) throw new ArgumentOutOfRangeException(nameof(kapasitas), "Kapasitas harus > 0.");
        if (string.IsNullOrWhiteSpace(satuanKapasitas)) throw new ArgumentException("Satuan Kapasitas wajib diisi.");
        if (rute.Length == 0) throw new ArgumentException("Rute wajib diisi minimal 1.");
        if (area.Length == 0) throw new ArgumentException("Area Coverage wajib diisi minimal 1 wilayah.");
        foreach (var wilayah in area)
        {
            if (!WilayahInfo.All.Any(w => w.DisplayName().Equals(wilayah, StringComparison.OrdinalIgnoreCase) || w.ToString().Equals(wilayah, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Wilayah '{wilayah}' tidak dikenal.");
            }
        }
    }

    private static void ValidateTarif(Produk produk, decimal tarif, string satuanTarif)
    {
        if (tarif <= 0) throw new ArgumentOutOfRangeException(nameof(tarif), "Tarif harus > 0.");
        if (string.IsNullOrWhiteSpace(satuanTarif)) throw new ArgumentException("Satuan Tarif wajib diisi.");
        var allowed = produk == Produk.MinyakTanah ? AllowedSatuanMinyak : AllowedSatuanTabung;
        if (!allowed.Contains(satuanTarif, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Satuan Tarif '{satuanTarif}' tidak valid untuk produk {produk.DisplayName()}. Allowed: {string.Join(", ", allowed)}.");
        }
    }

    private static void RequireSuperadmin(ClaimsPrincipal actor)
    {
        if (!actor.IsInRole("Superadmin"))
        {
            throw new UnauthorizedAccessException("Hanya Superadmin yang dapat mengelola Mitra.");
        }
    }

    private static string? GetUserId(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.NameIdentifier);
    private static string? GetEmail(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.Email);
    private static string? GetActiveRole(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.Role);
}
