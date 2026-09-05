using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StockMonitorTso.Api;

namespace StockMonitorTso.IntegrationTests;

/// <summary>Host API murni (JWT bearer) — skip seed stok (test yang butuh data seed pakai WithStock).</summary>
public class TestApiWebApplicationFactory : WebApplicationFactory<Api.Program>
{
    private string? _connectionString;
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"sm_keys_{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connectionString = TestDatabase.CreateDatabaseAsync().GetAwaiter().GetResult();
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("DataProtection:KeyPath", _keyPath);
        builder.UseSetting("Seed:SuperadminEmail", "superadmin@stockmonitor.local");
        builder.UseSetting("Seed:OperatorEmail", "operator@stockmonitor.local");
        builder.UseSetting("Seed:SupervisiEmail", "supervisi@stockmonitor.local");
        builder.UseSetting("Seed:TamuEmail", "tamu@stockmonitor.local");
        builder.UseSetting("Seed:MultiRoleEmail", "multi@stockmonitor.local");
        builder.UseSetting("Seed:SkipStock", "true");
        builder.UseSetting("Jwt:Key", "test-only-signing-key-stockmonitor-local-0123456789abcdef");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _connectionString is not null)
        {
            TestDatabase.DropDatabaseAsync(_connectionString).GetAwaiter().GetResult();
        }
    }
}

/// <summary>Api host dengan seed stok penuh (untuk test dashboard yang butuh data seed).</summary>
public sealed class TestApiWebApplicationFactoryWithStock : TestApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Seed:SkipStock", "false");
    }
}

/// <summary>Api host dengan APP_BASE_URL dipin — untuk test middleware PublicBaseUrl.</summary>
public sealed class TestApiWebApplicationFactoryWithBaseUrl : TestApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("App:BaseUrl", "https://public.example:8443");
    }
}
