using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record DashboardSummary
{
    public decimal TotalStok { get; init; }

    public int ProdukKritis { get; init; }

    public DateTime? ExhaustTerdekat { get; init; }
}

public sealed record LpgDashboardRow
{
    public required Wilayah Wilayah { get; init; }

    public required Produk Produk { get; init; }

    public decimal StokGudang { get; init; }

    public decimal DotGudang { get; init; }

    public decimal? CdGudang { get; init; }

    public Status? StatusGudang { get; init; }

    public decimal StokAgen { get; init; }

    public decimal DotAgen { get; init; }

    public decimal? CdAgen { get; init; }

    public Status? StatusAgen { get; init; }

    public DateTime? ExhaustAgen { get; init; }

    public decimal StokOutlet { get; init; }

    public decimal DotOutlet { get; init; }

    public decimal? CdOutlet { get; init; }

    public Status? StatusOutlet { get; init; }

    public DateTime? ExhaustOutlet { get; init; }

    public DateTime? NextSupplyEta { get; init; }
}

public sealed record MinyakTanahDashboardRow
{
    public required Wilayah Wilayah { get; init; }

    public DateTime Tanggal { get; init; }

    public decimal? StokGudang { get; init; }

    public decimal? CdGudang { get; init; }

    public Status? StatusGudang { get; init; }

    public decimal? StokAgen { get; init; }

    public decimal? CdAgen { get; init; }

    public Status? StatusAgen { get; init; }

    public decimal? StokOutlet { get; init; }

    public decimal? CdOutlet { get; init; }

    public Status? StatusOutlet { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }

    public string? Keterangan { get; init; }
}

public sealed record AgenCardRow
{
    public required int AgenId { get; init; }

    public required string Nama { get; init; }

    public decimal TotalStok { get; init; }

    public Status? Status { get; init; }
}

public sealed record SalesAreaCardRow
{
    public required Wilayah Wilayah { get; init; }

    public required Produk Produk { get; init; }

    public DateTime Tanggal { get; init; }

    public decimal? StokGudang { get; init; }

    public decimal? StokAgen { get; init; }

    public decimal? StokOutlet { get; init; }

    public decimal TotalStok { get; init; }

    public Status? StatusTerburuk { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }

    public string? Keterangan { get; init; }

    public IReadOnlyList<int> EntityIds { get; init; } = new List<int>();

    public IReadOnlyList<AgenCardRow> AgenRows { get; init; } = new List<AgenCardRow>();

    public decimal? StokGudang55Kg { get; init; }

    public decimal? StokGudang12Kg { get; init; }

    public decimal? StokGudang50Kg { get; init; }
}

public sealed record SalesAreaDetailRow
{
    public required Tier Tier { get; init; }

    public required Produk Produk { get; init; }

    public int StokEntitasId { get; init; }

    public DateTime TanggalStokAwal { get; init; }

    public decimal Stok { get; init; }

    public decimal DOT { get; init; }

    public decimal? Cd { get; init; }

    public Status? Status { get; init; }

    public DateTime? ExhaustDate { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }
}

public sealed record SalesAreaDetail
{
    public required Wilayah Wilayah { get; init; }

    public required Produk Produk { get; init; }

    public decimal TotalStok { get; init; }

    public decimal? CdTerburuk { get; init; }

    public Status? StatusArea { get; init; }

    public IReadOnlyList<SalesAreaDetailRow> Rows { get; init; } = new List<SalesAreaDetailRow>();

    public IReadOnlyList<StockTransactionView> Transactions { get; init; } = new List<StockTransactionView>();
}

public sealed record AgenInventarisRow
{
    public required int AgenId { get; init; }

    public required string Nama { get; init; }

    public DateTime TanggalDaftar { get; init; }

    public decimal TotalStok { get; init; }

    public int JumlahProduk { get; init; }

    public Status? StatusTerburuk { get; init; }
}

public sealed record AgenProductTarget
{
    public required Produk Produk { get; init; }

    public int StokEntitasId { get; init; }
}

/// <summary>Target transfer warehouse→agen: agen + StokEntitasId per produk (resolve tujuan transfer).</summary>
public sealed record AgenTransferTargetRow
{
    public required int AgenId { get; init; }

    public required string Nama { get; init; }

    public IReadOnlyList<AgenProductTarget> Products { get; init; } = new List<AgenProductTarget>();
}

public sealed record AgenProdukRow
{
    public required Produk Produk { get; init; }

    public int StokEntitasId { get; init; }

    public DateTime TanggalStokAwal { get; init; }

    public decimal Stok { get; init; }

    public decimal DOT { get; init; }

    public decimal? Cd { get; init; }

    public Status? Status { get; init; }

    public DateTime? ExhaustDate { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }
}

public sealed record AgenDetail
{
    public required int AgenId { get; init; }

    public required string Nama { get; init; }

    public required Wilayah Wilayah { get; init; }

    public DateTime TanggalDaftar { get; init; }

    public decimal TotalStok { get; init; }

    public decimal TotalDot { get; init; }

    public decimal? CdTerburuk { get; init; }

    public Status? StatusArea { get; init; }

    public DateTime? ExhaustTerdekat { get; init; }

    public IReadOnlyList<AgenProdukRow> Rows { get; init; } = new List<AgenProdukRow>();

    public IReadOnlyList<StockTransactionView> Transactions { get; init; } = new List<StockTransactionView>();
}

public sealed record StockTransactionView
{
    public DateTime Tanggal { get; init; }

    public required string Type { get; init; }

    public decimal Kuantitas { get; init; }

    public string? Tujuan { get; init; }

    public string? Catatan { get; init; }

    public decimal StokSesudah { get; init; }
}

public sealed record SektorCard
{
    public required string Nama { get; init; }

    public decimal TotalStok { get; init; }

    public required string Unit { get; init; }

    public int OutletKritis { get; init; }

    public Status? StatusSektor { get; init; }
}

public sealed record ChartPoint
{
    public required string Label { get; init; }

    public decimal Agen { get; init; }

    public decimal Outlet { get; init; }

    public bool Critical { get; init; }
}

public sealed record RingkasanOperasional
{
    public required SektorCard Gas { get; init; }

    public required SektorCard Minyak { get; init; }

    public IReadOnlyList<ChartPoint> GasChart { get; init; } = new List<ChartPoint>();

    public IReadOnlyList<ChartPoint> MinyakChart { get; init; } = new List<ChartPoint>();

    public IReadOnlyList<MinyakTanahDashboardRow> MetrikMinyak { get; init; } = new List<MinyakTanahDashboardRow>();
}

public enum DashboardFilter
{
    Semua,
    MinyakTanah,
    GasLpg,
}

public interface IStockDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);

    Task<RingkasanOperasional> GetRingkasanAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LpgDashboardRow>> GetLpgRowsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MinyakTanahDashboardRow>> GetMinyakTanahRowsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SalesAreaCardRow>> GetSalesAreaCardsAsync(DashboardFilter filter = DashboardFilter.Semua, CancellationToken ct = default);

    Task<SalesAreaDetail?> GetDetailAsync(Wilayah wilayah, Produk produk, CancellationToken ct = default);

    /// <summary>Detail gabungan 3 ukuran LPG (baris per Ukuran × Tier Gudang Wilayah/Outlet) untuk satu wilayah.</summary>
    Task<SalesAreaDetail?> GetLpgDetailAsync(Wilayah wilayah, CancellationToken ct = default);

    Task<IReadOnlyList<AgenInventarisRow>> GetAgenInventarisAsync(Wilayah wilayah, CancellationToken ct = default);

    Task<AgenDetail?> GetAgenDetailAsync(int agenId, CancellationToken ct = default);

    Task<IReadOnlyList<AgenTransferTargetRow>> GetAgenTransferTargetsAsync(Wilayah wilayah, CancellationToken ct = default);
}
