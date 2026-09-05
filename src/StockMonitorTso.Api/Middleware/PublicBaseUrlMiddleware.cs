namespace StockMonitorTso.Api.Middleware;

/// <summary>
/// Pin scheme/host request ke alamat publik yang dideklarasikan (`App:BaseUrl`, env `App__BaseUrl`).
/// Untuk deploy di balik reverse-proxy TLS: semua URL absolut (redirect auth, cookie flags,
/// antiforgery) jadi deterministik, tidak lagi bergantung pada scheme yang dilihat Kestrel.
/// Dipasang setelah UseForwardedHeaders; bila env tidak diset, middleware ini tidak dipakai.
/// </summary>
public sealed class PublicBaseUrlMiddleware(RequestDelegate next, Uri baseUrl)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Scheme = baseUrl.Scheme;
        context.Request.Host = HostString.FromUriComponent(baseUrl);
        await next(context);
    }
}
