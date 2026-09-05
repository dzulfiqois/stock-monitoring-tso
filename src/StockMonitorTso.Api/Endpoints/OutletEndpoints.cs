using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class OutletEndpoints
{
    public sealed record TransferAgenRequest(
        int AgenId,
        int OutletId,
        Dictionary<Produk, decimal> Quantities,
        string? Catatan);

    public static RouteGroupBuilder MapOutletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/outlet").WithTags("Outlet");

        group.MapGet("/", async (int agenId, IStockDashboardService service) =>
            Results.Ok(await service.GetOutletInventarisAsync(agenId))).RequireAuthorization();

        group.MapGet("/{outletId:int}", async (int outletId, IStockDashboardService service) =>
        {
            var detail = await service.GetOutletDetailAsync(outletId);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapPost("/", async (
            CreateOutletRequest request,
            HttpContext http,
            IOutletService service) =>
        {
            try
            {
                var outlet = await service.CreateAsync(http.User, request, http.RequestAborted);
                return Results.Created($"/api/outlet/{outlet.Id}", Slim(outlet));
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPut("/{outletId:int}", async (
            int outletId,
            UpdateOutletRequest request,
            HttpContext http,
            IOutletService service) =>
        {
            try
            {
                var outlet = await service.UpdateAsync(http.User, outletId, request, http.RequestAborted);
                return Results.Ok(Slim(outlet));
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapDelete("/{outletId:int}", async (
            int outletId,
            HttpContext http,
            IOutletService service) =>
        {
            try
            {
                await service.DeleteAsync(http.User, outletId, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPost("/transfer-from-agen", async (
            TransferAgenRequest request,
            HttpContext http,
            IOutletService service) =>
        {
            try
            {
                await service.TransferFromAgenAsync(
                    http.User, request.AgenId, request.OutletId, request.Quantities, request.Catatan, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        return group;
    }

    private static object Slim(Outlet outlet) => new
    {
        outlet.Id,
        outlet.Nama,
        outlet.AgenId,
        outlet.Wilayah,
        outlet.TanggalDaftar,
        outlet.IsDeleted,
    };
}
