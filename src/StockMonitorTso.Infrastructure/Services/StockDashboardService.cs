using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Domain.Services;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class StockDashboardService(ApplicationDbContext db) : IStockDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var entities = await db.StokEntitas.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);

        decimal totalStok = entities.Sum(e => e.Stok);
        var kritis = entities
            .Select(e => StockCalculator.StatusFor(StockCalculator.CoverageDays(e.Stok, e.DOT)))
            .Count(s => s == Status.Kritis);
        var exhaustDates = entities
            .Select(e => StockCalculator.ExhaustDate(e.TanggalStokAwal, StockCalculator.CoverageDays(e.Stok, e.DOT)))
            .Where(d => d.HasValue)
            .Select(d => d!.Value);

        return new DashboardSummary
        {
            TotalStok = totalStok,
            ProdukKritis = kritis,
            ExhaustTerdekat = exhaustDates.Any() ? exhaustDates.Min() : null,
        };
    }

    public async Task<IReadOnlyList<LpgDashboardRow>> GetLpgRowsAsync(CancellationToken ct = default)
    {
        var produkLpg = new[] { Produk.Lpg5_5Kg, Produk.Lpg12Kg, Produk.Lpg50Kg };
        var entities = await db.StokEntitas
            .AsNoTracking()
            .Include(e => e.RencanaKedatangan)
            .Where(e => produkLpg.Contains(e.Produk) && !e.IsDeleted)
            .ToListAsync(ct);

        var rows = new List<LpgDashboardRow>();
        foreach (var wilayah in WilayahInfo.All)
        {
            foreach (var produk in produkLpg)
            {
                var gudang = entities.FirstOrDefault(e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.GudangWilayah);
                var agenRows = entities.Where(e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.Agen).ToList();
                var outletRows = entities.Where(e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.Outlet).ToList();

                var cdGudang = gudang is null ? null : StockCalculator.CoverageDays(gudang.Stok, gudang.DOT);
                var stokAgen = agenRows.Sum(r => r.Stok);
                var dotAgen = agenRows.Sum(r => r.DOT);
                var cdAgen = StockCalculator.CoverageDays(stokAgen, dotAgen);
                var stokOutlet = outletRows.Sum(r => r.Stok);
                var dotOutlet = outletRows.Sum(r => r.DOT);
                var cdOutlet = StockCalculator.CoverageDays(stokOutlet, dotOutlet);
                var rencana = (gudang ?? outletRows.FirstOrDefault())?.RencanaKedatangan.OrderBy(r => r.Urutan).FirstOrDefault();

                rows.Add(new LpgDashboardRow
                {
                    Wilayah = wilayah,
                    Produk = produk,
                    StokGudang = gudang?.Stok ?? 0,
                    DotGudang = gudang?.DOT ?? 0,
                    CdGudang = cdGudang,
                    StatusGudang = StockCalculator.StatusFor(cdGudang),
                    StokAgen = stokAgen,
                    DotAgen = dotAgen,
                    CdAgen = cdAgen,
                    StatusAgen = StockCalculator.StatusFor(cdAgen),
                    ExhaustAgen = agenRows.Count == 0
                        ? null
                        : StockCalculator.ExhaustDate(agenRows.Min(r => r.TanggalStokAwal), cdAgen),
                    StokOutlet = stokOutlet,
                    DotOutlet = dotOutlet,
                    CdOutlet = cdOutlet,
                    StatusOutlet = StockCalculator.StatusFor(cdOutlet),
                    ExhaustOutlet = outletRows.Count == 0
                        ? null
                        : StockCalculator.ExhaustDate(outletRows.Min(r => r.TanggalStokAwal), cdOutlet),
                    NextSupplyEta = rencana?.ETA,
                });
            }
        }

        return rows;
    }

    public async Task<IReadOnlyList<MinyakTanahDashboardRow>> GetMinyakTanahRowsAsync(CancellationToken ct = default)
    {
        var entities = await db.StokEntitas
            .AsNoTracking()
            .Where(e => e.Produk == Produk.MinyakTanah && !e.IsDeleted)
            .ToListAsync(ct);

        var rows = new List<MinyakTanahDashboardRow>();
        foreach (var wilayah in WilayahInfo.All)
        {
            var gudang = entities.FirstOrDefault(e => e.Wilayah == wilayah && e.Tier == Tier.GudangWilayah);
            var agenRows = entities.Where(e => e.Wilayah == wilayah && e.Tier == Tier.Agen).ToList();
            var outletRows = entities.Where(e => e.Wilayah == wilayah && e.Tier == Tier.Outlet).ToList();

            var cdGudang = gudang is null ? null : StockCalculator.CoverageDays(gudang.Stok, gudang.DOT);
            var stokAgen = agenRows.Sum(r => r.Stok);
            var dotAgen = agenRows.Sum(r => r.DOT);
            var cdAgen = StockCalculator.CoverageDays(stokAgen, dotAgen);
            var stokOutlet = outletRows.Sum(r => r.Stok);
            var dotOutlet = outletRows.Sum(r => r.DOT);
            var cdOutlet = StockCalculator.CoverageDays(stokOutlet, dotOutlet);

            rows.Add(new MinyakTanahDashboardRow
            {
                Wilayah = wilayah,
                Tanggal = gudang?.TanggalStokAwal ?? outletRows.FirstOrDefault()?.TanggalStokAwal ?? default,
                StokGudang = gudang?.Stok,
                CdGudang = cdGudang,
                StatusGudang = StockCalculator.StatusFor(cdGudang),
                StokAgen = agenRows.Count == 0 ? null : stokAgen,
                CdAgen = cdAgen,
                StatusAgen = StockCalculator.StatusFor(cdAgen),
                StokOutlet = outletRows.Count == 0 ? null : stokOutlet,
                CdOutlet = cdOutlet,
                StatusOutlet = StockCalculator.StatusFor(cdOutlet),
                StokHabisTerjual = gudang?.StokHabisTerjual,
                StokIntransit = gudang?.StokIntransit,
                Keterangan = gudang?.Keterangan,
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<SalesAreaCardRow>> GetSalesAreaCardsAsync(DashboardFilter filter = DashboardFilter.Semua, CancellationToken ct = default)
    {
        var query = db.StokEntitas.AsNoTracking().Where(e => !e.IsDeleted);
        var produkLpg = new[] { Produk.Lpg5_5Kg, Produk.Lpg12Kg, Produk.Lpg50Kg };
        if (filter == DashboardFilter.MinyakTanah)
        {
            query = query.Where(e => e.Produk == Produk.MinyakTanah);
        }
        else if (filter == DashboardFilter.GasLpg)
        {
            query = query.Where(e => produkLpg.Contains(e.Produk));
        }

        var entities = await query.ToListAsync(ct);
        var agenByWilayah = await BuildAgenCardsAsync(ct);

        var cards = new List<SalesAreaCardRow>();
        foreach (var wilayah in WilayahInfo.All)
        {
            var wilayahEntities = entities.Where(e => e.Wilayah == wilayah).ToList();
            var minyakGudang = wilayahEntities.FirstOrDefault(e => e.Produk == Produk.MinyakTanah && e.Tier == Tier.GudangWilayah);
            var minyakAgenRows = wilayahEntities.Where(e => e.Produk == Produk.MinyakTanah && e.Tier == Tier.Agen).ToList();
            var minyakOutletRows = wilayahEntities.Where(e => e.Produk == Produk.MinyakTanah && e.Tier == Tier.Outlet).ToList();
            if (minyakGudang is not null || minyakAgenRows.Count > 0 || minyakOutletRows.Count > 0)
            {
                cards.Add(BuildSalesAreaCard(
                    wilayah, Produk.MinyakTanah, minyakGudang, minyakAgenRows, minyakOutletRows, agenByWilayah));
            }

            // LPG dikelompokkan menjadi SATU card per wilayah dengan rincian per ukuran (5.5/12/50).
            var lpgPerSku = produkLpg.Select(produk => new
            {
                Produk = produk,
                Gudang = wilayahEntities.FirstOrDefault(e => e.Produk == produk && e.Tier == Tier.GudangWilayah),
                AgenRows = wilayahEntities.Where(e => e.Produk == produk && e.Tier == Tier.Agen).ToList(),
                OutletRows = wilayahEntities.Where(e => e.Produk == produk && e.Tier == Tier.Outlet).ToList(),
            }).ToList();
            if (lpgPerSku.Any(x => x.Gudang is not null || x.AgenRows.Count > 0 || x.OutletRows.Count > 0))
            {
                var gudangIds = lpgPerSku.Select(x => x.Gudang?.Id).Where(id => id.HasValue).Select(id => id!.Value);
                var outletIds = lpgPerSku.SelectMany(x => x.OutletRows).Select(o => o.Id);
                var allAgenRows = lpgPerSku.SelectMany(x => x.AgenRows).ToList();
                var allOutletRows = lpgPerSku.SelectMany(x => x.OutletRows).ToList();
                var cdGudangs = lpgPerSku.Select(x => x.Gudang).Where(g => g is not null)
                    .Select(g => StockCalculator.CoverageDays(g!.Stok, g.DOT));
                var cdAgenAll = allAgenRows.Select(r => StockCalculator.CoverageDays(r.Stok, r.DOT));
                var cdOutletAll = allOutletRows.Select(r => StockCalculator.CoverageDays(r.Stok, r.DOT));
                var allCd = cdGudangs.Concat(cdAgenAll).Concat(cdOutletAll);
                var statuses = allCd.Select(StockCalculator.StatusFor)
                    .Where(s => s.HasValue).Select(s => s!.Value).ToList();

                cards.Add(new SalesAreaCardRow
                {
                    Wilayah = wilayah,
                    Produk = Produk.Lpg5_5Kg,
                    Tanggal = lpgPerSku.Select(x => x.Gudang?.TanggalStokAwal ?? x.OutletRows.FirstOrDefault()?.TanggalStokAwal ?? default)
                        .Where(d => d != default).DefaultIfEmpty(default).Min(),
                    StokGudang = lpgPerSku.Select(x => x.Gudang?.Stok ?? 0).Sum(),
                    StokAgen = allAgenRows.Sum(r => r.Stok),
                    StokOutlet = allOutletRows.Sum(r => r.Stok),
                    TotalStok = lpgPerSku.Select(x => x.Gudang?.Stok ?? 0).Sum()
                        + allAgenRows.Sum(r => r.Stok)
                        + allOutletRows.Sum(r => r.Stok),
                    StatusTerburuk = statuses.Count == 0 ? null : WorstStatus(statuses),
                    EntityIds = gudangIds.Concat(outletIds).ToList(),
                    AgenRows = agenByWilayah.TryGetValue(wilayah, out var agens) ? agens : new List<AgenCardRow>(),
                    StokGudang55Kg = lpgPerSku.First(x => x.Produk == Produk.Lpg5_5Kg).Gudang?.Stok,
                    StokGudang12Kg = lpgPerSku.First(x => x.Produk == Produk.Lpg12Kg).Gudang?.Stok,
                    StokGudang50Kg = lpgPerSku.First(x => x.Produk == Produk.Lpg50Kg).Gudang?.Stok,
                });
            }
        }

        return cards;
    }

    private static SalesAreaCardRow BuildSalesAreaCard(
        Wilayah wilayah,
        Produk produk,
        StokEntitas? gudang,
        IReadOnlyList<StokEntitas> agenRows,
        IReadOnlyList<StokEntitas> outletRows,
        Dictionary<Wilayah, List<AgenCardRow>> agenByWilayah)
    {
        var cdGudang = gudang is null ? null : StockCalculator.CoverageDays(gudang.Stok, gudang.DOT);
        var stokOutlet = outletRows.Sum(r => r.Stok);
        var statuses = new[] { cdGudang }
            .Concat(agenRows.Select(r => StockCalculator.CoverageDays(r.Stok, r.DOT)))
            .Concat(outletRows.Select(r => StockCalculator.CoverageDays(r.Stok, r.DOT)))
            .Select(StockCalculator.StatusFor)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .ToList();

        return new SalesAreaCardRow
        {
            Wilayah = wilayah,
            Produk = produk,
            Tanggal = gudang?.TanggalStokAwal ?? outletRows.FirstOrDefault()?.TanggalStokAwal ?? default,
            StokGudang = gudang?.Stok,
            StokAgen = agenRows.Count == 0 ? null : agenRows.Sum(r => r.Stok),
            StokOutlet = outletRows.Count == 0 ? null : stokOutlet,
            TotalStok = (gudang?.Stok ?? 0) + agenRows.Sum(r => r.Stok) + stokOutlet,
            StatusTerburuk = statuses.Count == 0 ? null : WorstStatus(statuses),
            StokHabisTerjual = gudang?.StokHabisTerjual,
            StokIntransit = gudang?.StokIntransit,
            Keterangan = gudang?.Keterangan,
            EntityIds = new[] { gudang?.Id }.Where(id => id.HasValue).Select(id => id!.Value).Concat(outletRows.Select(r => r.Id)).ToList(),
            AgenRows = agenByWilayah.TryGetValue(wilayah, out var agens) ? agens : new List<AgenCardRow>(),
        };
    }

    public async Task<SalesAreaDetail?> GetDetailAsync(Wilayah wilayah, Produk produk, CancellationToken ct = default)
    {
        var entities = await db.StokEntitas
            .AsNoTracking()
            .Include(e => e.RencanaKedatangan)
            .Where(e => e.Wilayah == wilayah && e.Produk == produk && !e.IsDeleted)
            .Where(e => e.Tier == Tier.GudangWilayah)
            .ToListAsync(ct);
        if (entities.Count == 0)
        {
            return null;
        }

        var rows = BuildDetailRows(entities);
        var cds = rows.Select(r => r.Cd).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        var transactions = await BuildTransactionViewsAsync(entities.Select(e => e.Id).ToList(), ct);

        return new SalesAreaDetail
        {
            Wilayah = wilayah,
            Produk = produk,
            TotalStok = rows.Sum(r => r.Stok),
            CdTerburuk = cds.Count == 0 ? null : cds.Min(),
            StatusArea = StockCalculator.StatusFor(cds.Count == 0 ? null : cds.Min()),
            Rows = rows,
            Transactions = transactions,
        };
    }

    public async Task<SalesAreaDetail?> GetLpgDetailAsync(Wilayah wilayah, CancellationToken ct = default)
    {
        var produkLpg = new[] { Produk.Lpg5_5Kg, Produk.Lpg12Kg, Produk.Lpg50Kg };
        var entities = await db.StokEntitas
            .AsNoTracking()
            .Include(e => e.RencanaKedatangan)
            .Where(e => e.Wilayah == wilayah && produkLpg.Contains(e.Produk) && !e.IsDeleted)
            .Where(e => e.Tier == Tier.GudangWilayah)
            .ToListAsync(ct);
        if (entities.Count == 0)
        {
            return null;
        }

        var rows = BuildDetailRows(entities);
        var cds = rows.Select(r => r.Cd).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        var transactions = await BuildTransactionViewsAsync(entities.Select(e => e.Id).ToList(), ct);

        return new SalesAreaDetail
        {
            Wilayah = wilayah,
            Produk = Produk.Lpg5_5Kg,
            TotalStok = rows.Sum(r => r.Stok),
            CdTerburuk = cds.Count == 0 ? null : cds.Min(),
            StatusArea = StockCalculator.StatusFor(cds.Count == 0 ? null : cds.Min()),
            Rows = rows,
            Transactions = transactions,
        };
    }

    private static List<SalesAreaDetailRow> BuildDetailRows(IReadOnlyList<StokEntitas> entities)
    {
        var rows = new List<SalesAreaDetailRow>();
        foreach (var entity in entities.OrderBy(e => e.Tier).ThenBy(e => e.Produk))
        {
            var cd = StockCalculator.CoverageDays(entity.Stok, entity.DOT);
            rows.Add(new SalesAreaDetailRow
            {
                Tier = entity.Tier,
                Produk = entity.Produk,
                StokEntitasId = entity.Id,
                TanggalStokAwal = entity.TanggalStokAwal,
                Stok = entity.Stok,
                DOT = entity.DOT,
                Cd = cd,
                Status = StockCalculator.StatusFor(cd),
                ExhaustDate = StockCalculator.ExhaustDate(entity.TanggalStokAwal, cd),
                StokHabisTerjual = entity.StokHabisTerjual,
                StokIntransit = entity.StokIntransit,
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<AgenInventarisRow>> GetAgenInventarisAsync(Wilayah wilayah, CancellationToken ct = default)
    {
        var agens = await db.Agen.AsNoTracking()
            .Where(a => a.Wilayah == wilayah && !a.IsDeleted)
            .OrderBy(a => a.Nama)
            .ToListAsync(ct);
        var rows = await db.StokEntitas.AsNoTracking()
            .Where(e => e.Tier == Tier.Agen && !e.IsDeleted)
            .ToListAsync(ct);

        var result = new List<AgenInventarisRow>();
        foreach (var agen in agens)
        {
            var agenRows = rows.Where(r => r.AgenId == agen.Id).ToList();
            var statuses = agenRows
                .Select(r => StockCalculator.StatusFor(StockCalculator.CoverageDays(r.Stok, r.DOT)))
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            result.Add(new AgenInventarisRow
            {
                AgenId = agen.Id,
                Nama = agen.Nama,
                TanggalDaftar = agen.TanggalDaftar,
                TotalStok = agenRows.Sum(r => r.Stok),
                JumlahProduk = agenRows.Count(r => r.Produk == Produk.MinyakTanah || r.Stok > 0 || r.DOT > 0),
                StatusTerburuk = statuses.Count == 0 ? null : WorstStatus(statuses),
            });
        }

        return result;
    }

    public async Task<AgenDetail?> GetAgenDetailAsync(int agenId, CancellationToken ct = default)
    {
        var agen = await db.Agen.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agenId && !a.IsDeleted, ct);
        if (agen is null)
        {
            return null;
        }

        var entities = await db.StokEntitas.AsNoTracking()
            .Include(e => e.RencanaKedatangan)
            .Where(e => e.AgenId == agenId && !e.IsDeleted)
            .ToListAsync(ct);

        var rows = new List<AgenProdukRow>();
        foreach (var entity in entities.OrderBy(e => e.Produk))
        {
            var cd = StockCalculator.CoverageDays(entity.Stok, entity.DOT);
            rows.Add(new AgenProdukRow
            {
                Produk = entity.Produk,
                StokEntitasId = entity.Id,
                TanggalStokAwal = entity.TanggalStokAwal,
                Stok = entity.Stok,
                DOT = entity.DOT,
                Cd = cd,
                Status = StockCalculator.StatusFor(cd),
                ExhaustDate = StockCalculator.ExhaustDate(entity.TanggalStokAwal, cd),
                StokHabisTerjual = entity.StokHabisTerjual,
                StokIntransit = entity.StokIntransit,
            });
        }

        var cds = rows.Select(r => r.Cd).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        var transactions = await BuildTransactionViewsAsync(entities.Select(e => e.Id).ToList(), ct);

        return new AgenDetail
        {
            AgenId = agen.Id,
            Nama = agen.Nama,
            Wilayah = agen.Wilayah,
            TanggalDaftar = agen.TanggalDaftar,
            TotalStok = rows.Sum(r => r.Stok),
            TotalDot = rows.Sum(r => r.DOT),
            CdTerburuk = cds.Count == 0 ? null : cds.Min(),
            StatusArea = StockCalculator.StatusFor(cds.Count == 0 ? null : cds.Min()),
            ExhaustTerdekat = rows.Select(r => r.ExhaustDate).Where(d => d.HasValue).Select(d => d!.Value).Cast<DateTime?>().DefaultIfEmpty(null).Min(),
            Rows = rows,
            Transactions = transactions,
        };
    }

    public async Task<IReadOnlyList<AgenTransferTargetRow>> GetAgenTransferTargetsAsync(Wilayah wilayah, CancellationToken ct = default)
    {
        var agens = await db.Agen.AsNoTracking()
            .Where(a => a.Wilayah == wilayah && !a.IsDeleted)
            .OrderBy(a => a.Nama)
            .ToListAsync(ct);
        var rows = await db.StokEntitas.AsNoTracking()
            .Where(e => e.Tier == Tier.Agen && !e.IsDeleted)
            .ToListAsync(ct);

        var result = new List<AgenTransferTargetRow>();
        foreach (var agen in agens)
        {
            var products = rows
                .Where(r => r.AgenId == agen.Id)
                .Select(r => new AgenProductTarget { Produk = r.Produk, StokEntitasId = r.Id })
                .ToList();
            result.Add(new AgenTransferTargetRow
            {
                AgenId = agen.Id,
                Nama = agen.Nama,
                Products = products,
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<OutletInventarisRow>> GetOutletInventarisAsync(int agenId, CancellationToken ct = default)
    {
        var outlets = await db.Outlet.AsNoTracking()
            .Where(o => o.AgenId == agenId && !o.IsDeleted)
            .OrderBy(o => o.Nama)
            .ToListAsync(ct);
        var rows = await db.StokEntitas.AsNoTracking()
            .Where(e => e.Tier == Tier.Outlet && !e.IsDeleted)
            .ToListAsync(ct);

        var result = new List<OutletInventarisRow>();
        foreach (var outlet in outlets)
        {
            var outletRows = rows.Where(r => r.OutletId == outlet.Id).ToList();
            var statuses = outletRows
                .Select(r => StockCalculator.StatusFor(StockCalculator.CoverageDays(r.Stok, r.DOT)))
                .Where(s => s.HasValue).Select(s => s!.Value).ToList();
            result.Add(new OutletInventarisRow
            {
                OutletId = outlet.Id,
                Nama = outlet.Nama,
                TanggalDaftar = outlet.TanggalDaftar,
                TotalStok = outletRows.Sum(r => r.Stok),
                JumlahProduk = outletRows.Count(r => r.Produk == Produk.MinyakTanah || r.Stok > 0 || r.DOT > 0),
                StatusTerburuk = statuses.Count == 0 ? null : WorstStatus(statuses),
            });
        }

        return result;
    }

    public async Task<OutletDetail?> GetOutletDetailAsync(int outletId, CancellationToken ct = default)
    {
        var outlet = await db.Outlet.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == outletId && !o.IsDeleted, ct);
        if (outlet is null)
        {
            return null;
        }

        var agen = await db.Agen.AsNoTracking().FirstOrDefaultAsync(a => a.Id == outlet.AgenId, ct);
        var entities = await db.StokEntitas.AsNoTracking()
            .Include(e => e.RencanaKedatangan)
            .Where(e => e.OutletId == outletId && !e.IsDeleted)
            .ToListAsync(ct);

        var rows = new List<OutletProdukRow>();
        foreach (var entity in entities.OrderBy(e => e.Produk))
        {
            var cd = StockCalculator.CoverageDays(entity.Stok, entity.DOT);
            rows.Add(new OutletProdukRow
            {
                Produk = entity.Produk,
                StokEntitasId = entity.Id,
                TanggalStokAwal = entity.TanggalStokAwal,
                Stok = entity.Stok,
                DOT = entity.DOT,
                Cd = cd,
                Status = StockCalculator.StatusFor(cd),
                ExhaustDate = StockCalculator.ExhaustDate(entity.TanggalStokAwal, cd),
                StokHabisTerjual = entity.StokHabisTerjual,
                StokIntransit = entity.StokIntransit,
            });
        }

        var cds = rows.Select(r => r.Cd).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        var transactions = await BuildTransactionViewsAsync(entities.Select(e => e.Id).ToList(), ct);

        return new OutletDetail
        {
            OutletId = outlet.Id,
            Nama = outlet.Nama,
            AgenId = outlet.AgenId,
            Wilayah = outlet.Wilayah,
            TanggalDaftar = outlet.TanggalDaftar,
            TotalStok = rows.Sum(r => r.Stok),
            TotalDot = rows.Sum(r => r.DOT),
            CdTerburuk = cds.Count == 0 ? null : cds.Min(),
            StatusArea = StockCalculator.StatusFor(cds.Count == 0 ? null : cds.Min()),
            ExhaustTerdekat = rows.Select(r => r.ExhaustDate).Where(d => d.HasValue).Select(d => d!.Value).Cast<DateTime?>().DefaultIfEmpty(null).Min(),
            Rows = rows,
            Transactions = transactions,
        };
    }

    public async Task<IReadOnlyList<OutletTransferTargetRow>> GetOutletTransferTargetsAsync(int agenId, CancellationToken ct = default)
    {
        var outlets = await db.Outlet.AsNoTracking()
            .Where(o => o.AgenId == agenId && !o.IsDeleted)
            .OrderBy(o => o.Nama)
            .ToListAsync(ct);
        var rows = await db.StokEntitas.AsNoTracking()
            .Where(e => e.Tier == Tier.Outlet && !e.IsDeleted)
            .ToListAsync(ct);

        var result = new List<OutletTransferTargetRow>();
        foreach (var outlet in outlets)
        {
            var products = rows.Where(r => r.OutletId == outlet.Id)
                .Select(r => new OutletProductTarget { Produk = r.Produk, StokEntitasId = r.Id })
                .ToList();
            result.Add(new OutletTransferTargetRow
            {
                OutletId = outlet.Id,
                Nama = outlet.Nama,
                Products = products,
            });
        }

        return result;
    }

    public async Task<RingkasanOperasional> GetRingkasanAsync(CancellationToken ct = default)
    {
        var lpgRows = await GetLpgRowsAsync(ct);
        var minyakRows = await GetMinyakTanahRowsAsync(ct);

        decimal totalGas = lpgRows.Sum(r => r.StokGudang + r.StokAgen + r.StokOutlet);
        var outletKritisGas = lpgRows.Count(r => r.StatusOutlet == Status.Kritis);

        decimal totalMinyak = minyakRows.Sum(r => (r.StokGudang ?? 0) + (r.StokAgen ?? 0) + (r.StokOutlet ?? 0));
        var outletKritisMinyak = minyakRows.Count(r => r.StatusOutlet == Status.Kritis);

        var gasStatus = lpgRows.Select(r => r.StatusGudang)
            .Concat(lpgRows.Select(r => r.StatusAgen))
            .Concat(lpgRows.Select(r => r.StatusOutlet))
            .Where(s => s.HasValue).Select(s => s!.Value).ToList();
        var minyakStatus = minyakRows.Select(r => r.StatusGudang)
            .Concat(minyakRows.Select(r => r.StatusAgen))
            .Concat(minyakRows.Select(r => r.StatusOutlet))
            .Where(s => s.HasValue).Select(s => s!.Value).ToList();

        var gasChart = new List<ChartPoint>();
        var minyakChart = new List<ChartPoint>();
        foreach (var wilayah in WilayahInfo.All)
        {
            var gasRows = lpgRows.Where(r => r.Wilayah == wilayah).ToList();
            if (gasRows.Count > 0)
            {
                var status = gasRows.Select(r => r.StatusGudang)
                    .Concat(gasRows.Select(r => r.StatusAgen))
                    .Concat(gasRows.Select(r => r.StatusOutlet))
                    .Where(s => s.HasValue).Select(s => s!.Value).ToList();
                gasChart.Add(new ChartPoint
                {
                    Label = ShortName(wilayah),
                    Agen = gasRows.Sum(r => r.StokAgen),
                    Outlet = gasRows.Sum(r => r.StokOutlet),
                    Critical = status.Contains(Status.Kritis),
                });
            }

            var w = minyakRows.FirstOrDefault(r => r.Wilayah == wilayah);
            if (w is not null)
            {
                minyakChart.Add(new ChartPoint
                {
                    Label = ShortName(wilayah),
                    Agen = w.StokAgen ?? 0,
                    Outlet = w.StokOutlet ?? 0,
                    Critical = w.StatusGudang == Status.Kritis || w.StatusAgen == Status.Kritis || w.StatusOutlet == Status.Kritis,
                });
            }
        }

        return new RingkasanOperasional
        {
            Gas = new SektorCard
            {
                Nama = "Sektor Gas Tabung",
                TotalStok = totalGas,
                Unit = "Tabung",
                OutletKritis = outletKritisGas,
                StatusSektor = gasStatus.Count == 0 ? null : WorstStatus(gasStatus),
            },
            Minyak = new SektorCard
            {
                Nama = "Sektor Minyak Tanah",
                TotalStok = totalMinyak,
                Unit = "Kiloliter",
                OutletKritis = outletKritisMinyak,
                StatusSektor = minyakStatus.Count == 0 ? null : WorstStatus(minyakStatus),
            },
            GasChart = gasChart,
            MinyakChart = minyakChart,
            MetrikMinyak = minyakRows,
        };
    }

    private async Task<Dictionary<Wilayah, List<AgenCardRow>>> BuildAgenCardsAsync(CancellationToken ct)
    {
        var agens = await db.Agen.AsNoTracking().Where(a => !a.IsDeleted).ToListAsync(ct);
        var rows = await db.StokEntitas.AsNoTracking().Where(e => e.Tier == Tier.Agen && !e.IsDeleted).ToListAsync(ct);

        var map = new Dictionary<Wilayah, List<AgenCardRow>>();
        foreach (var wilayah in WilayahInfo.All)
        {
            var list = new List<AgenCardRow>();
            foreach (var agen in agens.Where(a => a.Wilayah == wilayah).OrderBy(a => a.Nama))
            {
                var agenRows = rows.Where(r => r.AgenId == agen.Id).ToList();
                var statuses = agenRows
                    .Select(r => StockCalculator.StatusFor(StockCalculator.CoverageDays(r.Stok, r.DOT)))
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();
                list.Add(new AgenCardRow
                {
                    AgenId = agen.Id,
                    Nama = agen.Nama,
                    TotalStok = agenRows.Sum(r => r.Stok),
                    Status = statuses.Count == 0 ? null : WorstStatus(statuses),
                });
            }

            map[wilayah] = list;
        }

        return map;
    }

    private async Task<IReadOnlyList<StockTransactionView>> BuildTransactionViewsAsync(IReadOnlyList<int> entityIds, CancellationToken ct)
    {
        if (entityIds.Count == 0)
        {
            return new List<StockTransactionView>();
        }

        var transactions = await db.StockTransactions
            .AsNoTracking()
            .Where(t => entityIds.Contains(t.StokEntitasId) || (t.StokEntitasTujuanId != null && entityIds.Contains(t.StokEntitasTujuanId.Value)))
            .OrderByDescending(t => t.Id)
            .Take(50)
            .ToListAsync(ct);

        var allEntities = await db.StokEntitas.AsNoTracking().ToListAsync(ct);
        var agens = await db.Agen.AsNoTracking().ToListAsync(ct);

        return transactions.Select(t =>
        {
            var tujuan = t.StokEntitasTujuanId is null
                ? null
                : TujuanLabel(allEntities, agens, t.StokEntitasTujuanId.Value);
            return new StockTransactionView
            {
                Tanggal = t.Tanggal,
                Type = t.Type.ToString(),
                Kuantitas = t.Kuantitas,
                Tujuan = tujuan,
                Catatan = t.Catatan,
                StokSesudah = t.StokSumberSesudah,
            };
        }).ToList();
    }

    private static string? TujuanLabel(IReadOnlyList<StokEntitas> entities, IReadOnlyList<Agen> agens, int tujuanId)
    {
        var tujuan = entities.FirstOrDefault(e => e.Id == tujuanId);
        if (tujuan is null)
        {
            return null;
        }

        if (tujuan.AgenId is not null)
        {
            var agen = agens.FirstOrDefault(a => a.Id == tujuan.AgenId.Value);
            return agen is null ? tujuan.Tier.DisplayName() : $"{tujuan.Tier.DisplayName()} {agen.Nama}";
        }

        return tujuan.Tier.DisplayName();
    }

    private static Status WorstStatus(List<Status> statuses)
    {
        if (statuses.Contains(Status.Kritis))
        {
            return Status.Kritis;
        }

        return statuses.Contains(Status.Warning) ? Status.Warning : Status.Aman;
    }

    private static string ShortName(Wilayah wilayah) => wilayah.DisplayName()
        .Replace("Papua Selatan-Pegunungan", "Papua Selatan")
        .Replace("Papua Barat Daya", "P. Barat Daya")
        .Replace("Papua Barat", "P. Barat");
}
