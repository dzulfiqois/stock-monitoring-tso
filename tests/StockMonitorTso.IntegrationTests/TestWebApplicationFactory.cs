using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StockMonitorTso.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"stockmonitor_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"DataSource={_dbPath};Cache=Shared");
        builder.UseSetting("Seed:SuperadminEmail", "superadmin@stockmonitor.local");
        builder.UseSetting("Seed:OperatorEmail", "operator@stockmonitor.local");
        builder.UseSetting("Seed:SupervisiEmail", "supervisi@stockmonitor.local");
        builder.UseSetting("Seed:TamuEmail", "tamu@stockmonitor.local");
        builder.UseSetting("Seed:MultiRoleEmail", "multi@stockmonitor.local");
        builder.UseSetting("Seed:SkipStock", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
                if (File.Exists(_dbPath + "-wal"))
                {
                    File.Delete(_dbPath + "-wal");
                }
                if (File.Exists(_dbPath + "-shm"))
                {
                    File.Delete(_dbPath + "-shm");
                }
            }
            catch (IOException)
            {
                // ignore: file still locked by OS
            }
        }
    }
}

/// <summary>Fixture untuk test yang meregister stok sendiri — seed stok dinonaktifkan agar tidak bentrok.</summary>
public sealed class TestWebApplicationFactoryNoStock : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Seed:SkipStock", "true");
    }
}
