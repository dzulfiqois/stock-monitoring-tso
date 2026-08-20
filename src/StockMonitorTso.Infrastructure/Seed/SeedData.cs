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
            // DB lama (data sudah ada): pastikan identitas agen mock ter-seed dari baris Gudang Wilayah.
            await SeedAgenMockAsync(db);
            return;
        }

        var excelPath = configuration["Seed:ExcelPath"] ?? ResolveExcelPath();
        if (File.Exists(excelPath))
        {
            var rows = ExcelStockSeeder.LoadLpgGudangWilayah(excelPath)
                .Concat(ExcelStockSeeder.LoadLpgOutlet(excelPath));
            foreach (var row in rows)
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

        foreach (var row in ExcelStockSeeder.LoadMinyakTanahSample())
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

    /// <summary>Cari `Monitoring Tabung RPM(1).xlsx` dari content root ke atas hingga repo root.</summary>
    private static string ResolveExcelPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Monitoring Tabung RPM(1).xlsx");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "Monitoring Tabung RPM(1).xlsx";
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
