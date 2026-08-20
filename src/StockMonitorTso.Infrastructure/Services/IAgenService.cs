using System.Security.Claims;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record CreateAgenRequest
{
    public required string Nama { get; init; }

    public required Wilayah Wilayah { get; init; }

    public string? Keterangan { get; init; }
}

public sealed record UpdateAgenRequest
{
    public required string Nama { get; init; }

    public string? Keterangan { get; init; }
}

public interface IAgenService
{
    /// <summary>Create identitas Agen (Superadmin + Supervisi — amend STOCK §3). Auto-create baris stok per produk (0).</summary>
    Task<Agen> CreateAsync(ClaimsPrincipal actor, CreateAgenRequest request, CancellationToken ct = default);

    /// <summary>Update identitas Agen (nama/keterangan) — Superadmin + Supervisi.</summary>
    Task<Agen> UpdateAsync(ClaimsPrincipal actor, int agenId, UpdateAgenRequest request, CancellationToken ct = default);

    /// <summary>Delete identitas Agen (soft delete + baris stok ikut soft delete) — Superadmin only.</summary>
    Task DeleteAsync(ClaimsPrincipal actor, int agenId, CancellationToken ct = default);

    /// <summary>
    /// Transfer stok dari Gudang Wilayah ke satu Agen. Superadmin + Supervisi.
    /// qtyPerProduk: kuantitas > 0 per SKU yang akan dikirim (loop Transfer per SKU, atomic).
    /// Overdraft per SKU ditolak; konservasi debit-kredit terjaga lewat StockWriteService.
    /// </summary>
    Task TransferFromWarehouseAsync(
        ClaimsPrincipal actor,
        Wilayah wilayah,
        int agenId,
        IReadOnlyDictionary<Produk, decimal> qtyPerProduk,
        string? catatan = null,
        CancellationToken ct = default);
}
