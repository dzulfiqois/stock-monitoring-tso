using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class StockEndpoints
{
    public sealed record TransactRequest(StockTransactionType Type, decimal Kuantitas, int? TujuanId, string? Catatan);

    public static RouteGroupBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock").WithTags("Stock");

        group.MapPost("/", async (
            RegisterStokRequest request,
            HttpContext http,
            IStockWriteService service) =>
        {
            try
            {
                var entity = await service.RegisterAsync(http.User, request, http.RequestAborted);
                return Results.Created($"/api/stock/{entity.Id}", new
                {
                    entity.Id,
                    entity.Wilayah,
                    entity.Produk,
                    entity.Tier,
                    entity.Stok,
                    entity.DOT,
                });
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPut("/{id:int}", async (
            int id,
            UpdateDetailRequest request,
            HttpContext http,
            IStockWriteService service) =>
        {
            try
            {
                var entity = await service.UpdateDetailAsync(http.User, id, request, http.RequestAborted);
                return Results.Ok(new { entity.Id, entity.DOT, entity.TanggalStokAwal, entity.Keterangan });
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPost("/{id:int}/transact", async (
            int id,
            TransactRequest request,
            HttpContext http,
            IStockWriteService service) =>
        {
            try
            {
                var entity = await service.TransactAsync(
                    http.User, id, request.Type, request.Kuantitas, request.TujuanId, request.Catatan,
                    http.RequestAborted);
                return Results.Ok(new { entity.Id, entity.Stok, entity.StokHabisTerjual });
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async (
            int id,
            HttpContext http,
            IStockWriteService service) =>
        {
            try
            {
                await service.DeleteAsync(http.User, id, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        return group;
    }
}
