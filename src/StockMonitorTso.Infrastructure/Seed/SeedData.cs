using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Seed;

public static class SeedData
{
    public static readonly string[] Roles = ["Superadmin", "Operator", "Supervisi", "Tamu"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await SeedUserAsync(userManager, configuration, "Seed:SuperadminEmail", "superadmin@stockmonitor.local", "Superadmin", "Superadmin!2345");
        await SeedUserAsync(userManager, configuration, "Seed:OperatorEmail", "operator@stockmonitor.local", "Operator", "Operator!2345");
        await SeedUserAsync(userManager, configuration, "Seed:SupervisiEmail", "supervisi@stockmonitor.local", "Supervisi", "Supervisi!2345");
        await SeedUserAsync(userManager, configuration, "Seed:TamuEmail", "tamu@stockmonitor.local", "Tamu", "Tamu!2345");

        await SeedMultiRoleUserAsync(userManager, configuration);

        await SeedMitraTsoAsync(services, configuration);

        await SeedStockAsync(services, configuration);
    }

    private static async Task SeedStockAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (string.Equals(configuration["Seed:SkipStock"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var db = services.GetRequiredService<ApplicationDbContext>();

        if (await db.StokEntitas.AnyAsync())
        {
            // DB lama (data sudah ada): pastikan identitas agen/outlet mock ter-seed.
            await SeedAgenMockAsync(db);
            await SeedOutletMockAsync(db);
            return;
        }

        var lpgJsonPath = configuration["Seed:LpgJsonPath"] ?? ResolveSeedFilePath("lpg-stok.json");
        if (File.Exists(lpgJsonPath))
        {
            foreach (var row in LpgStokSeeder.Load(lpgJsonPath))
            {
                db.StokEntitas.Add(new StokEntitas
                {
                    Wilayah = row.Wilayah,
                    Produk = row.Produk,
                    Tier = row.Tier,
                    TanggalStokAwal = row.TanggalStokAwal,
                    Stok = row.Stok,
                    DOT = row.Dot,
                    RencanaKedatangan = row.RencanaKedatangan.Select(r => new RencanaKedatangan
                    {
                        Urutan = r.Urutan,
                        NextSupply = r.NextSupply,
                        ETA = r.Eta,
                    }).ToList(),
                });
            }
        }

        foreach (var row in StockSeedRows.LoadMinyakTanahSample())
        {
            db.StokEntitas.Add(new StokEntitas
            {
                Wilayah = row.Wilayah,
                Produk = row.Produk,
                Tier = row.Tier,
                TanggalStokAwal = row.TanggalStokAwal,
                Stok = row.Stok,
                DOT = row.Dot,
                StokHabisTerjual = row.StokHabisTerjual,
                StokIntransit = row.StokIntransit,
                Keterangan = row.Keterangan,
            });
        }

        await db.SaveChangesAsync();

        await SeedAgenMockAsync(db);
        await SeedOutletMockAsync(db);
    }

    /// <summary>
    /// Membuat identitas agen mock (2–3 per wilayah) + baris stok awal dari stok Gudang Wilayah.
    /// Total stok agen per (Wilayah × Produk) = 50% stok Gudang Wilayah, dibagi rata; DOT dibagi rata.
    /// Konservasi: stok gudang di-debit sejumlah yang dialihkan ke agen, tiap pengalihan dicatat
    /// sebagai transaksi Transfer (atomic debit-kredit, STOCK §2.c). Idempoten: dilewati bila ada agen.
    /// </summary>
    private static async Task SeedAgenMockAsync(ApplicationDbContext db)
    {
        if (await db.Agen.AnyAsync())
        {
            return;
        }

        var gudangRows = await db.StokEntitas
            .Where(e => e.Tier == Tier.GudangWilayah && !e.IsDeleted)
            .ToListAsync();

        var pendingTransfer = new List<(StokEntitas Gudang, StokEntitas AgenRow, decimal Qty)>();
        var daftarAgen = new List<Agen>();
        foreach (var wilayah in WilayahInfo.All)
        {
            var wilayahGudang = gudangRows.Where(e => e.Wilayah == wilayah).ToList();
            if (wilayahGudang.Count == 0)
            {
                continue;
            }

            var count = AgenMockSeeder.AgenCount(wilayah);
            for (var i = 1; i <= count; i++)
            {
                var agen = new Agen
                {
                    Nama = AgenMockSeeder.AgenName(wilayah, i),
                    Wilayah = wilayah,
                    TanggalDaftar = wilayahGudang[0].TanggalStokAwal,
                };

                foreach (var gudang in wilayahGudang)
                {
                    var stokSplits = AgenMockSeeder.SplitEqual(gudang.Stok * 0.5m, count);
                    var dotSplits = AgenMockSeeder.SplitEqual(gudang.DOT, count);
                    var agenRow = new StokEntitas
                    {
                        Wilayah = wilayah,
                        Produk = gudang.Produk,
                        Tier = Tier.Agen,
                        TanggalStokAwal = gudang.TanggalStokAwal,
                        Stok = stokSplits[i - 1],
                        DOT = dotSplits[i - 1],
                    };
                    agen.StokEntitas.Add(agenRow);
                    pendingTransfer.Add((gudang, agenRow, stokSplits[i - 1]));
                }

                daftarAgen.Add(agen);
            }
        }

        db.Agen.AddRange(daftarAgen);
        await db.SaveChangesAsync();

        foreach (var (gudang, agenRow, qty) in pendingTransfer)
        {
            var sebelum = gudang.Stok;
            gudang.Stok -= qty;
            db.StockTransactions.Add(new StockTransactionRecord
            {
                StokEntitasId = gudang.Id,
                StokEntitasTujuanId = agenRow.Id,
                Type = StockTransactionType.Transfer,
                Kuantitas = qty,
                Tanggal = gudang.TanggalStokAwal,
                Catatan = "Distribusi awal ke agen (mock 50%)",
                StokSumberSebelum = sebelum,
                StokSumberSesudah = gudang.Stok,
                StokTujuanSebelum = 0m,
                StokTujuanSesudah = qty,
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Membuat identitas outlet mock (2 per agen) + baris stok awal dari stok agen.
    /// Stok tiap outlet = 50% stok agen ÷ 2; DOT = agen DOT ÷ 2. Konservasi: stok agen didebit
    /// via Transfer. Outlet agregat lama (Tier.Outlet tanpa OutletId) di-soft-delete agar
    /// dashboard hanya menampilkan outlet bernama. Idempoten.
    /// </summary>
    private static async Task SeedOutletMockAsync(ApplicationDbContext db)
    {
        if (await db.Outlet.AnyAsync())
        {
            return;
        }

        var agenList = await db.Agen.Where(a => !a.IsDeleted).ToListAsync();
        if (agenList.Count == 0)
        {
            return;
        }

        // Soft-delete baris outlet agregat lama (jika masih ada) agar tidak double-count.
        var oldOutletRows = await db.StokEntitas
            .Where(e => e.Tier == Tier.Outlet && e.OutletId == null && !e.IsDeleted)
            .ToListAsync();
        foreach (var r in oldOutletRows)
        {
            r.IsDeleted = true;
        }

        var agenRows = await db.StokEntitas
            .Where(e => e.Tier == Tier.Agen && !e.IsDeleted)
            .ToListAsync();

        var pendingTransfer = new List<(StokEntitas AgenRow, StokEntitas OutletRow, decimal Qty)>();
        var daftarOutlet = new List<Outlet>();
        foreach (var agen in agenList)
        {
            for (var i = 1; i <= OutletMockSeeder.OutletPerAgen; i++)
            {
                var outlet = new Outlet
                {
                    Nama = OutletMockSeeder.OutletName(agen, i),
                    AgenId = agen.Id,
                    Wilayah = agen.Wilayah,
                    TanggalDaftar = agen.TanggalDaftar,
                };

                foreach (var produk in ProdukInfo.All)
                {
                    var agenRow = agenRows.FirstOrDefault(r => r.AgenId == agen.Id && r.Produk == produk);
                    if (agenRow is null)
                    {
                        continue;
                    }

                    var stokSplits = AgenMockSeeder.SplitEqual(agenRow.Stok * 0.5m, OutletMockSeeder.OutletPerAgen);
                    var dotSplits = AgenMockSeeder.SplitEqual(agenRow.DOT, OutletMockSeeder.OutletPerAgen);
                    var outletRow = new StokEntitas
                    {
                        Wilayah = agen.Wilayah,
                        Produk = produk,
                        Tier = Tier.Outlet,
                        TanggalStokAwal = agenRow.TanggalStokAwal,
                        Stok = stokSplits[i - 1],
                        DOT = dotSplits[i - 1],
                    };
                    outlet.StokEntitas.Add(outletRow);
                    pendingTransfer.Add((agenRow, outletRow, stokSplits[i - 1]));
                }

                daftarOutlet.Add(outlet);
            }
        }

        db.Outlet.AddRange(daftarOutlet);
        await db.SaveChangesAsync();

        foreach (var (agenRow, outletRow, qty) in pendingTransfer)
        {
            var sebelum = agenRow.Stok;
            agenRow.Stok -= qty;
            db.StockTransactions.Add(new StockTransactionRecord
            {
                StokEntitasId = agenRow.Id,
                StokEntitasTujuanId = outletRow.Id,
                Type = StockTransactionType.Transfer,
                Kuantitas = qty,
                Tanggal = agenRow.TanggalStokAwal,
                Catatan = "Distribusi awal ke outlet (mock 50%)",
                StokSumberSebelum = sebelum,
                StokSumberSesudah = agenRow.Stok,
                StokTujuanSebelum = 0m,
                StokTujuanSesudah = qty,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedMitraTsoAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var mitraPath = configuration["Seed:MitraPath"] ?? ResolveMitraPath();
        if (!File.Exists(mitraPath))
        {
            return;
        }

        var mitras = MitraTsoSeeder.Load(mitraPath);
        foreach (var mitra in mitras)
        {
            var existing = await db.MitraTso.Include(m => m.Tarifs).FirstOrDefaultAsync(m => m.Id == mitra.Id);
            if (existing is null)
            {
                db.MitraTso.Add(mitra);
            }
            else
            {
                existing.Nama = mitra.Nama;
                existing.JenisKendaraan = mitra.JenisKendaraan;
                existing.KapasitasMax = mitra.KapasitasMax;
                existing.SatuanKapasitas = mitra.SatuanKapasitas;
                existing.Rute = mitra.Rute;
                existing.AreaCoverage = mitra.AreaCoverage;
                existing.Kontak = mitra.Kontak;
                existing.Pic = mitra.Pic;
                existing.Active = mitra.Active;
                existing.Tarif = mitra.Tarif;
                existing.SatuanTarif = mitra.SatuanTarif;
                foreach (var tarif in mitra.Tarifs)
                {
                    var existTarif = existing.Tarifs.FirstOrDefault(t => t.Produk == tarif.Produk);
                    if (existTarif is null)
                    {
                        existing.Tarifs.Add(new MitraTarif { MitraId = existing.Id, Produk = tarif.Produk, Tarif = tarif.Tarif, SatuanTarif = tarif.SatuanTarif });
                    }
                    else
                    {
                        existTarif.Tarif = tarif.Tarif;
                        existTarif.SatuanTarif = tarif.SatuanTarif;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static string ResolveMitraPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "seeds", "mitra-tso.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine("seeds", "mitra-tso.json");
    }

    /// <summary>Cari `Monitoring Tabung RPM(1).xlsx` dari content root ke atas hingga repo root.</summary>
    /// <summary>Cari file seed di folder `seeds/` dari content root ke atas hingga repo root.</summary>
    private static string ResolveSeedFilePath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "seeds", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine("seeds", fileName);
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string emailKey,
        string defaultEmail,
        string role,
        string defaultPassword)
    {
        var email = configuration[emailKey] ?? defaultEmail;
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            ActiveRoleName = role,
        };

        var password = configuration["Seed:DefaultPassword"] ?? defaultPassword;
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedMultiRoleUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var email = configuration["Seed:MultiRoleEmail"] ?? "multi@stockmonitor.local";
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            ActiveRoleName = "Operator",
        };

        var password = configuration["Seed:DefaultPassword"] ?? "MultiRole!2345";
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Operator");
            await userManager.AddToRoleAsync(user, "Supervisi");
            await userManager.AddToRoleAsync(user, "Tamu");
        }
    }
}
