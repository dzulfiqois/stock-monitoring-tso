using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class AgenEndpoints
{
    public sealed record TransferWarehouseRequest(
        Wilayah Wilayah,
        Dictionary<Produk, decimal> Quantities,
        string? Catatan);

    public static RouteGroupBuilder MapAgenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agen").WithTags("Agen");

        group.MapGet("/", async (Wilayah wilayah, IStockDashboardService service) =>
            Results.Ok(await service.GetAgenInventarisAsync(wilayah))).RequireAuthorization();

        group.MapGet("/{agenId:int}", async (int agenId, IStockDashboardService service) =>
        {
            var detail = await service.GetAgenDetailAsync(agenId);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        }).RequireAuthorization();

        group.MapPost("/", async (
            CreateAgenRequest request,
            HttpContext http,
            IAgenService service) =>
        {
            try
            {
                var agen = await service.CreateAsync(http.User, request, http.RequestAborted);
                return Results.Created($"/api/agen/{agen.Id}", Slim(agen));
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPut("/{agenId:int}", async (
            int agenId,
            UpdateAgenRequest request,
            HttpContext http,
            IAgenService service) =>
        {
            try
            {
                var agen = await service.UpdateAsync(http.User, agenId, request, http.RequestAborted);
                return Results.Ok(Slim(agen));
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapDelete("/{agenId:int}", async (
            int agenId,
            HttpContext http,
            IAgenService service) =>
        {
            try
            {
                await service.DeleteAsync(http.User, agenId, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPost("/{agenId:int}/transfer-from-warehouse", async (
            int agenId,
            TransferWarehouseRequest request,
            HttpContext http,
            IAgenService service) =>
        {
            try
            {
                await service.TransferFromWarehouseAsync(
                    http.User, request.Wilayah, agenId, request.Quantities, request.Catatan, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        return group;
    }

    private static object Slim(Agen agen) => new
    {
        agen.Id,
        agen.Nama,
        agen.Wilayah,
        agen.TanggalDaftar,
        agen.IsDeleted,
    };
}
