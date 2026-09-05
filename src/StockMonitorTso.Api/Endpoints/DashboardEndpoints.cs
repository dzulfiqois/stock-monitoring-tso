using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/summary", async (IStockDashboardService service) =>
            Results.Ok(await service.GetSummaryAsync())).RequireAuthorization();

        group.MapGet("/ringkasan", async (IStockDashboardService service) =>
            Results.Ok(await service.GetRingkasanAsync())).RequireAuthorization();

        group.MapGet("/lpg-rows", async (IStockDashboardService service) =>
            Results.Ok(await service.GetLpgRowsAsync())).RequireAuthorization();

        group.MapGet("/minyak-rows", async (IStockDashboardService service) =>
            Results.Ok(await service.GetMinyakTanahRowsAsync())).RequireAuthorization();

        group.MapGet("/cards", async (string? filter, IStockDashboardService service) =>
        {
            var parsed = DashboardFilter.Semua;
            if (!string.IsNullOrWhiteSpace(filter) && !Enum.TryParse(filter, ignoreCase: true, out parsed))
            {
                return Results.Problem($"Filter '{filter}' tidak dikenal (Semua/MinyakTanah/GasLpg).", statusCode: 400);
            }

            return Results.Ok(await service.GetSalesAreaCardsAsync(parsed));
        }).RequireAuthorization();

        group.MapGet("/sales-area/{wilayah}/{produk}", async (
            Wilayah wilayah, Produk produk, IStockDashboardService service) =>
        {
            var detail = await service.GetDetailAsync(wilayah, produk);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapGet("/sales-area-lpg/{wilayah}", async (Wilayah wilayah, IStockDashboardService service) =>
        {
            var detail = await service.GetLpgDetailAsync(wilayah);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapGet("/agen-inventaris/{wilayah}", async (Wilayah wilayah, IStockDashboardService service) =>
            Results.Ok(await service.GetAgenInventarisAsync(wilayah))).RequireAuthorization();

        group.MapGet("/agen/{agenId:int}", async (int agenId, IStockDashboardService service) =>
        {
            var detail = await service.GetAgenDetailAsync(agenId);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapGet("/agen-transfer-targets/{wilayah}", async (Wilayah wilayah, IStockDashboardService service) =>
            Results.Ok(await service.GetAgenTransferTargetsAsync(wilayah))).RequireAuthorization();

        group.MapGet("/outlet-inventaris/{agenId:int}", async (int agenId, IStockDashboardService service) =>
            Results.Ok(await service.GetOutletInventarisAsync(agenId))).RequireAuthorization();

        group.MapGet("/outlet/{outletId:int}", async (int outletId, IStockDashboardService service) =>
        {
            var detail = await service.GetOutletDetailAsync(outletId);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapGet("/outlet-transfer-targets/{agenId:int}", async (int agenId, IStockDashboardService service) =>
            Results.Ok(await service.GetOutletTransferTargetsAsync(agenId))).RequireAuthorization();

        return group;
    }
}
