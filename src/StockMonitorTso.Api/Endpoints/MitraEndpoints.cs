using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class MitraEndpoints
{
    public static RouteGroupBuilder MapMitraEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mitra").WithTags("Mitra");

        group.MapGet("/", async (IMitraService service) =>
        {
            var list = await service.ListAsync();
            return Results.Ok(list);
        }).RequireAuthorization();

        group.MapGet("/{id}", async (string id, IMitraService service) =>
        {
            var mitra = await service.GetAsync(id);
            return mitra is null ? Results.NotFound() : Results.Ok(mitra);
        }).RequireAuthorization();

        group.MapPost("/", async (CreateMitraRequest req, IMitraService service, HttpContext http) =>
        {
            try
            {
                var mitra = await service.CreateAsync(http.User, req);
                return Results.Created($"/api/mitra/{mitra.Id}", mitra);
            }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentOutOfRangeException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
        }).RequireAuthorization();

        group.MapPut("/{id}", async (string id, UpdateMitraRequest req, IMitraService service, HttpContext http) =>
        {
            try
            {
                var mitra = await service.UpdateAsync(http.User, id, req);
                return Results.Ok(mitra);
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
        }).RequireAuthorization();

        group.MapPut("/{id}/tarif", async (string id, UpdateMitraTarifRequest req, IMitraService service, HttpContext http) =>
        {
            try
            {
                var mitra = await service.UpdateTarifAsync(http.User, id, req);
                return Results.Ok(mitra);
            }
            catch (KeyNotFoundException ex) { return Results.Problem(detail: ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(detail: ex.Message, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
            catch (ArgumentException ex) { return Results.Problem(detail: ex.Message, statusCode: 400); }
        }).RequireAuthorization();

        return group;
    }
}
