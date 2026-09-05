using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StockMonitorTso.Api.Endpoints;
using StockMonitorTso.Api.Middleware;
using StockMonitorTso.Api.Services;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Seed;
using StockMonitorTso.Infrastructure.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog: konfigurasi dari appsettings (Production memakai formatter JSON terstruktur).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Enum sebagai string di seluruh API (request & response) — kontrak stabil untuk frontend React.
// IgnoreCycles: navigasi balik (TransportOrderDetail.Order, MitraTarif.Mitra) tidak diserialisasi.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Defense-in-depth: kini factory juga hanya menerbitkan klaim role aktif
// (selaras dengan TokenService — hak akses mengikuti role aktif, bukan gabungan).
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
    ActiveRoleClaimsPrincipalFactory>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"] ?? "stockmonitor-api",
            ValidateAudience = true,
            ValidAudience = jwt["Audience"] ?? "stockmonitor-client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key belum dikonfigurasi (env: Jwt__Key)."))),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IAgenService, AgenService>();
builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<IMitraService, MitraService>();
builder.Services.AddScoped<ITransportOrderService, TransportOrderService>();
builder.Services.AddScoped<IStockDashboardService, StockDashboardService>();
builder.Services.AddScoped<IStockWriteService, StockWriteService>();

// /ready memakai tag "ready" (cek database); /health liveness tanpa dependensi.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadyHealthCheck>("database", tags: ["ready"]);

var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"] ?? "/app/keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
    .SetApplicationName("StockMonitorTso");

var publicBaseUrl = builder.Configuration["App:BaseUrl"];
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
if (Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUrl))
{
    app.UseMiddleware<PublicBaseUrlMiddleware>(baseUrl);
}

// Serilog: log request terstruktur (method, path, status, duration, RequestId)
// dipasang sedini mungkin agar mencakup seluruh pipeline.
app.UseSerilogRequestLogging();

if (!EF.IsDesignTime)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await SeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseAuthentication();
app.UseAuthorization();

// /health = liveness (tanpa dependensi) · /ready = readiness (cek database).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapStockEndpoints();
app.MapAgenEndpoints();
app.MapOutletEndpoints();
app.MapUserEndpoints();
app.MapTsoEndpoints();
app.MapMitraEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/debug/request", (HttpContext http) => Results.Ok(new
    {
        scheme = http.Request.Scheme,
        host = http.Request.Host.Value,
        isHttps = http.Request.IsHttps,
    }));
}

Log.Information("Aplikasi Stock Monitor dan TSO (API) dimulai.");
app.Run();

namespace StockMonitorTso.Api
{
    public partial class Program
    {
    }
}
