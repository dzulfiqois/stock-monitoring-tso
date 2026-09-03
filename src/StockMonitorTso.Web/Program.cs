using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Api.Endpoints;
using StockMonitorTso.Infrastructure.Seed;
using StockMonitorTso.Infrastructure.Services;
using StockMonitorTso.Web.Components;
using StockMonitorTso.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(options =>
    {
        // Idle timeout 15 menit dengan sliding expiration (STOCK_MONITORING_SPEC.md §6.5).
        options.ApplicationCookie?.Configure(cookie =>
        {
            cookie.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            cookie.SlidingExpiration = true;
            // Single-URL architecture: login surface lives at "/" so unauthenticated
            // challenges must redirect there (default "/Account/Login" would 404).
            cookie.LoginPath = "/";
            // Single-URL architecture: strip the framework's automatic ?ReturnUrl=<original>
            // query so the address bar stays clean after an auth challenge.
            cookie.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    ctx.Response.Redirect(ctx.Request.PathBase + "/");
                    return Task.CompletedTask;
                },
            };
        });
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Switchable active role: hanya role aktif yang menjadi klaim role (STOCK §6.2).
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ActiveRoleClaimsPrincipalFactory>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IAgenService, AgenService>();
builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<IMitraService, MitraService>();
builder.Services.AddScoped<ITransportOrderService, TransportOrderService>();
builder.Services.AddScoped<IStockDashboardService, StockDashboardService>();
builder.Services.AddScoped<IStockWriteService, StockWriteService>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate database + seed roles/users pada startup (PLAN.md Phase 1).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapTsoEndpoints();
app.MapMitraEndpoints();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program
{
}
