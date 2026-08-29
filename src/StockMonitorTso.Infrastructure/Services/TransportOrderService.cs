using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class TransportOrderService(
    ApplicationDbContext db,
    IAuditLogService auditLog) : ITransportOrderService
{
    public async Task<TransportOrder> CreateAsync(ClaimsPrincipal actor, CreateTransportOrderRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Operator");

        var mitra = await db.MitraTso.Include(m => m.Tarifs).FirstOrDefaultAsync(m => m.Id == request.MitraId && m.Active, ct)
            ?? throw new KeyNotFoundException($"Mitra TSO '{request.MitraId}' tidak terdaftar.");
        if (!mitra.AreaCoverage.Contains(request.WilayahTujuan.DisplayName(), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Mitra '{mitra.Nama}' tidak melayani wilayah {request.WilayahTujuan.DisplayName()}.");
        }

        var detailsInput = request.Details is not null && request.Details.Count > 0
            ? request.Details
            : new[] { new TransportOrderDetailDto { Produk = request.Produk, Kuantitas = request.Kuantitas } };

        foreach (var d in detailsInput)
        {
            if (d.Kuantitas <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(d.Kuantitas), "Kuantitas harus > 0.");
            }

            var tarif = mitra.Tarifs.FirstOrDefault(t => t.Produk == d.Produk);
            var satuanTarif = tarif?.SatuanTarif ?? mitra.SatuanTarif;
            ValidateSatuanForProduk(d.Produk, satuanTarif);
            if (satuanTarif.Contains("kilometer", StringComparison.OrdinalIgnoreCase) && (request.JarakKm is null || request.JarakKm <= 0))
            {
                throw new ArgumentException($"JarakKm wajib diisi > 0 untuk satuan '{satuanTarif}' pada produk {d.Produk.DisplayName()}.");
            }
        }

        var today = DateTime.Today;
        if (request.TanggalKeberangkatan.Date < today)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TanggalKeberangkatan), "Tanggal Keberangkatan tidak boleh mendahului hari ini.");
        }

        // Idempotensi T1/F9: cek duplikat dalam 1 menit terakhir (header-level, cukup untuk MVP)
        var windowStart = DateTime.UtcNow.AddMinutes(-1);
        var primaryForDedup = detailsInput[0];
        var totalForDedup = detailsInput.Sum(d => d.Kuantitas);
        var duplicate = await db.TransportOrders.AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.MitraId == request.MitraId
                && o.WilayahTujuan == request.WilayahTujuan
                && o.Produk == primaryForDedup.Produk
                && o.Kuantitas == totalForDedup
                && o.TanggalKeberangkatan.Date == request.TanggalKeberangkatan.Date
                && o.CreatedAt >= windowStart
                && !o.IsDeleted, ct);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var orderNo = await GenerateOrderNoAsync(ct);
        var now = DateTime.UtcNow;
        var eta = request.TanggalKeberangkatan.Date.AddDays(7);
        var primary = detailsInput[0];
        var satuan = primary.Produk.Satuan();
        decimal totalEstimasi = 0;
        var detailEntities = new List<TransportOrderDetail>();
        foreach (var d in detailsInput)
        {
            var tarifRow = mitra.Tarifs.FirstOrDefault(t => t.Produk == d.Produk);
            var tarif = tarifRow?.Tarif ?? mitra.Tarif;
            var satuanTarif = tarifRow?.SatuanTarif ?? mitra.SatuanTarif;
            var estimasi = satuanTarif.Contains("kilometer", StringComparison.OrdinalIgnoreCase) && request.JarakKm.HasValue
                ? tarif * d.Kuantitas * request.JarakKm.Value
                : tarif * d.Kuantitas;
            totalEstimasi += estimasi;
            detailEntities.Add(new TransportOrderDetail
            {
                Produk = d.Produk,
                Kuantitas = d.Kuantitas,
                TarifSnapshot = tarif,
                SatuanTarifSnapshot = satuanTarif,
                EstimasiBiayaSnapshot = estimasi,
            });
        }

        var order = new TransportOrder
        {
            OrderNo = orderNo,
            MitraId = mitra.Id,
            MitraNamaSnapshot = mitra.Nama,
            TarifSnapshot = detailEntities[0].TarifSnapshot,
            SatuanTarifSnapshot = detailEntities[0].SatuanTarifSnapshot,
            EstimasiBiayaSnapshot = totalEstimasi,
            WilayahTujuan = request.WilayahTujuan,
            RuteAsal = request.RuteAsal ?? "Pusat",
            RuteTujuan = request.RuteTujuan ?? $"Gudang Wilayah {request.WilayahTujuan.DisplayName()}",
            JarakKm = request.JarakKm,
            Produk = primary.Produk,
            Kuantitas = detailsInput.Sum(d => d.Kuantitas),
            Satuan = satuan,
            TanggalKeberangkatan = request.TanggalKeberangkatan.Date,
            Eta = eta,
            Status = TransportOrderStatus.Committed,
            CreatedAt = now,
            CreatedBy = GetEmail(actor),
            InvoiceNo = orderNo,
            RowVersion = Guid.NewGuid().ToByteArray(),
            Details = detailEntities,
        };

        db.TransportOrders.Add(order);
        await db.SaveChangesAsync(ct);

        // T5 dampak stok: buat RencanaKedatangan di Gudang Wilayah tujuan (± F7)
        try
        {
            await CreateRencanaKedatanganAsync(order, ct);
            order.Status = TransportOrderStatus.StockImpacted;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            order.Status = TransportOrderStatus.FlagTertunda;
            await db.SaveChangesAsync(ct);
        }

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "CreateTso",
            EntityType = "TransportOrder",
            EntityId = order.Id.ToString(),
            After = $"{order.OrderNo}|{order.MitraId}|{order.WilayahTujuan}|{order.Produk}|{order.Kuantitas}",
        }, ct);

        return order;
    }

    public async Task<TransportOrder?> GetAsync(int id, CancellationToken ct = default)
        => await db.TransportOrders.AsNoTracking().Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct);

    public async Task<IReadOnlyList<TransportOrder>> ListAsync(CancellationToken ct = default)
        => await db.TransportOrders.AsNoTracking().Include(o => o.Details).Where(o => !o.IsDeleted).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<MitraTso>> ListMitraAsync(CancellationToken ct = default)
        => await db.MitraTso.AsNoTracking().Include(m => m.Tarifs).Where(m => m.Active).OrderBy(m => m.Nama).ToListAsync(ct);

    public async Task<TransportOrder> UpdateAsync(ClaimsPrincipal actor, int id, UpdateTransportOrderRequest request, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin", "Supervisi");

        var order = await db.TransportOrders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order tidak ditemukan.");

        // Optimistic concurrency F8
        if (!order.RowVersion.SequenceEqual(request.RowVersion))
        {
            throw new DbUpdateConcurrencyException("Data telah diperbarui pihak lain, muat ulang.");
        }

        var mitra = await db.MitraTso.Include(m => m.Tarifs).FirstOrDefaultAsync(m => m.Id == request.MitraId && m.Active, ct)
            ?? throw new KeyNotFoundException($"Mitra TSO '{request.MitraId}' tidak terdaftar.");
        if (!mitra.AreaCoverage.Contains(request.WilayahTujuan.DisplayName(), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Mitra '{mitra.Nama}' tidak melayani wilayah {request.WilayahTujuan.DisplayName()}.");
        }

        var detailsInput = request.Details is not null && request.Details.Count > 0
            ? request.Details
            : new[] { new TransportOrderDetailDto { Produk = request.Produk, Kuantitas = request.Kuantitas } };

        foreach (var d in detailsInput)
        {
            if (d.Kuantitas <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(d.Kuantitas), "Kuantitas harus > 0.");
            }

            var tarif = mitra.Tarifs.FirstOrDefault(t => t.Produk == d.Produk);
            var satuanTarif = tarif?.SatuanTarif ?? mitra.SatuanTarif;
            ValidateSatuanForProduk(d.Produk, satuanTarif);
            if (satuanTarif.Contains("kilometer", StringComparison.OrdinalIgnoreCase) && (request.JarakKm is null || request.JarakKm <= 0))
            {
                throw new ArgumentException($"JarakKm wajib diisi > 0 untuk satuan '{satuanTarif}' pada produk {d.Produk.DisplayName()}.");
            }
        }

        if (request.TanggalKeberangkatan.Date < DateTime.Today)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TanggalKeberangkatan), "Tanggal Keberangkatan tidak boleh mendahului hari ini.");
        }

        var before = $"{order.MitraId}|{order.WilayahTujuan}|{order.Produk}|{order.Kuantitas}|{order.TanggalKeberangkatan:yyyy-MM-dd}";
        var primary = detailsInput[0];
        decimal totalEstimasi = 0;
        foreach (var d in detailsInput)
        {
            var tarifRow = mitra.Tarifs.FirstOrDefault(t => t.Produk == d.Produk);
            var tarif = tarifRow?.Tarif ?? mitra.Tarif;
            var satuanTarif = tarifRow?.SatuanTarif ?? mitra.SatuanTarif;
            var estimasi = satuanTarif.Contains("kilometer", StringComparison.OrdinalIgnoreCase) && request.JarakKm.HasValue
                ? tarif * d.Kuantitas * request.JarakKm.Value
                : tarif * d.Kuantitas;
            totalEstimasi += estimasi;
        }

        order.MitraId = mitra.Id;
        order.MitraNamaSnapshot = mitra.Nama;
        order.TarifSnapshot = mitra.Tarifs.FirstOrDefault(t => t.Produk == primary.Produk)?.Tarif ?? mitra.Tarif;
        order.SatuanTarifSnapshot = mitra.Tarifs.FirstOrDefault(t => t.Produk == primary.Produk)?.SatuanTarif ?? mitra.SatuanTarif;
        order.EstimasiBiayaSnapshot = totalEstimasi;
        order.WilayahTujuan = request.WilayahTujuan;
        order.RuteAsal = request.RuteAsal ?? order.RuteAsal;
        order.RuteTujuan = request.RuteTujuan ?? order.RuteTujuan;
        order.JarakKm = request.JarakKm;
        order.Produk = primary.Produk;
        order.Kuantitas = detailsInput.Sum(d => d.Kuantitas);
        order.Satuan = primary.Produk.Satuan();
        order.TanggalKeberangkatan = request.TanggalKeberangkatan.Date;
        order.Eta = request.TanggalKeberangkatan.Date.AddDays(7);
        await db.Entry(order).Collection(o => o.Details).LoadAsync(ct);
        db.TransportOrderDetails.RemoveRange(order.Details);
        order.Details.Clear();
        foreach (var d in detailsInput)
        {
            var tarifRow = mitra.Tarifs.FirstOrDefault(t => t.Produk == d.Produk);
            var tarif = tarifRow?.Tarif ?? mitra.Tarif;
            var satuanTarif = tarifRow?.SatuanTarif ?? mitra.SatuanTarif;
            var estimasi = satuanTarif.Contains("kilometer", StringComparison.OrdinalIgnoreCase) && request.JarakKm.HasValue
                ? tarif * d.Kuantitas * request.JarakKm.Value
                : tarif * d.Kuantitas;
            order.Details.Add(new TransportOrderDetail
            {
                OrderId = order.Id,
                Produk = d.Produk,
                Kuantitas = d.Kuantitas,
                TarifSnapshot = tarif,
                SatuanTarifSnapshot = satuanTarif,
                EstimasiBiayaSnapshot = estimasi,
            });
        }

        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = GetEmail(actor);
        order.RowVersion = Guid.NewGuid().ToByteArray();

        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "UpdateTso",
            EntityType = "TransportOrder",
            EntityId = order.Id.ToString(),
            Before = before,
            After = $"{order.MitraId}|{order.WilayahTujuan}|{order.Produk}|{order.Kuantitas}|{order.TanggalKeberangkatan:yyyy-MM-dd}",
        }, ct);

        return order;
    }

    public async Task DeleteAsync(ClaimsPrincipal actor, int id, CancellationToken ct = default)
    {
        RequireAnyRole(actor, "Superadmin");

        var order = await db.TransportOrders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order tidak ditemukan.");

        order.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = GetUserId(actor),
            ActorEmail = GetEmail(actor),
            ActorRole = GetActiveRole(actor),
            Action = "DeleteTso",
            EntityType = "TransportOrder",
            EntityId = order.Id.ToString(),
            After = order.OrderNo,
        }, ct);
    }

    public async Task ResyncStockImpactAsync(int id, CancellationToken ct = default)
    {
        var order = await db.TransportOrders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order tidak ditemukan.");

        if (order.Status == TransportOrderStatus.StockImpacted)
        {
            return;
        }

        await CreateRencanaKedatanganAsync(order, ct);
        order.Status = TransportOrderStatus.StockImpacted;
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GenerateInvoiceAsync(int id, CancellationToken ct = default)
    {
        var order = await db.TransportOrders.AsNoTracking().Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order tidak ditemukan.");

        var generator = new InvoiceGenerator();
        var pdf = generator.Generate(order);

        // T9 idempoten: Generate hanya read, tidak ubah order. InvoiceGeneratedAt set sekali.
        if (order.InvoiceGeneratedAt is null)
        {
            var tracked = await db.TransportOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (tracked is not null && tracked.InvoiceGeneratedAt is null)
            {
                tracked.InvoiceGeneratedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        return pdf;
    }

    private async Task CreateRencanaKedatanganAsync(TransportOrder order, CancellationToken ct)
    {
        // Load details if not already
        await db.Entry(order).Collection(o => o.Details).LoadAsync(ct);
        var details = order.Details.Count > 0 ? order.Details : new List<TransportOrderDetail>
        {
            new() { Produk = order.Produk, Kuantitas = order.Kuantitas }
        };

        foreach (var detail in details)
        {
            var stok = await db.StokEntitas
                .FirstOrDefaultAsync(e => e.Wilayah == order.WilayahTujuan && e.Produk == detail.Produk && e.Tier == Tier.GudangWilayah && !e.IsDeleted, ct)
                ?? throw new InvalidOperationException($"Gudang Wilayah {order.WilayahTujuan} / {detail.Produk} tidak ditemukan.");

            var existingCount = await db.RencanaKedatangan.CountAsync(r => r.StokEntitasId == stok.Id, ct);
            if (existingCount >= 3)
            {
                throw new InvalidOperationException($"Rencana Kedatangan untuk {detail.Produk.DisplayName()} sudah penuh (3 slot).");
            }

            var rencana = new RencanaKedatangan
            {
                StokEntitasId = stok.Id,
                Urutan = existingCount + 1,
                NextSupply = detail.Kuantitas,
                ETA = order.Eta,
            };
            db.RencanaKedatangan.Add(rencana);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateOrderNoAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TSO-{today}-";
        var countToday = await db.TransportOrders.CountAsync(o => o.OrderNo.StartsWith(prefix), ct);
        return $"{prefix}{(countToday + 1):D4}";
    }

    private static void ValidateSatuanForProduk(Produk produk, string satuanTarif)
    {
        var allowed = produk == Produk.MinyakTanah
            ? new[] { "per_kiloliter", "per_kilometer" }
            : new[] { "per_tabung", "per_kilometer" };
        if (!allowed.Contains(satuanTarif, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Satuan Tarif '{satuanTarif}' tidak valid untuk produk {produk.DisplayName()}. Allowed: {string.Join(", ", allowed)}.");
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
