using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class TsoEndpoints
{
    public static RouteGroupBuilder MapTsoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tso").WithTags("TSO");

        group.MapPost("/", async (
            CreateTransportOrderRequest req,
            ITransportOrderService service,
            HttpContext http) =>
        {
            try
            {
                var order = await service.CreateAsync(http.User, req);
                return Results.Created($"/api/tso/{order.Id}", order);
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentOutOfRangeException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
        }).RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, ITransportOrderService service) =>
        {
            var order = await service.GetAsync(id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization();

        group.MapGet("/", async (ITransportOrderService service) =>
        {
            var list = await service.ListAsync();
            return Results.Ok(list);
        }).RequireAuthorization();

        group.MapPut("/{id:int}", async (
            int id,
            UpdateTransportOrderRequest req,
            ITransportOrderService service,
            HttpContext http) =>
        {
            try
            {
                var order = await service.UpdateAsync(http.User, id, req);
                return Results.Ok(order);
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
            catch (DbUpdateConcurrencyException ex) { return Results.Problem(detail: ex.Message, statusCode: 409); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentOutOfRangeException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async (
            int id,
            ITransportOrderService service,
            HttpContext http) =>
        {
            try
            {
                await service.DeleteAsync(http.User, id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
        }).RequireAuthorization();

        group.MapPost("/{id:int}/invoice", async (int id, ITransportOrderService service) =>
        {
            try
            {
                var pdf = await service.GenerateInvoiceAsync(id);
                return Results.File(pdf, "application/pdf", $"DraftInvoice-{id}.pdf");
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
        }).RequireAuthorization();

        group.MapPost("/{id:int}/resync", async (int id, ITransportOrderService service) =>
        {
            try
            {
                await service.ResyncStockImpactAsync(id);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
        }).RequireAuthorization();

        return group;
    }
}
