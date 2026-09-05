using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.Api.Endpoints;

public static class UserEndpoints
{
    public sealed record CreateUserBody(string Email, string Password, IReadOnlyList<string> Roles, string ActiveRole);

    public sealed record RoleBody(string Role);

    public sealed record PasswordBody(string NewPassword);

    public sealed record UserView(string Id, string? Email, string? ActiveRole, IReadOnlyList<string> Roles);

    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users")
            .RequireAuthorization(policy => policy.RequireRole("Superadmin"));

        group.MapGet("/", async (
            HttpContext http,
            IUserAdminService service) =>
        {
            try
            {
                var users = await service.ListUsersAsync(http.RequestAborted);
                var views = new List<UserView>();
                foreach (var user in users)
                {
                    views.Add(new UserView(user.Id, user.Email, user.ActiveRoleName, await service.GetUserRolesAsync(user.Id, http.RequestAborted)));
                }

                return Results.Ok(views);
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPost("/", async (
            CreateUserBody request,
            HttpContext http,
            IUserAdminService service) =>
        {
            try
            {
                var userId = await service.CreateUserAsync(
                    http.User, request.Email, request.Password, request.Roles, request.ActiveRole, http.RequestAborted);
                return Results.Created($"/api/users/{userId}", new { Id = userId, request.Email });
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapGet("/{userId}/roles", async (
            string userId,
            IUserAdminService service) =>
        {
            try
            {
                return Results.Ok(await service.GetUserRolesAsync(userId));
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPut("/{userId}/roles", async (
            string userId,
            RoleBody request,
            HttpContext http,
            IUserAdminService service) =>
        {
            try
            {
                await service.AssignRoleAsync(http.User, userId, request.Role, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapDelete("/{userId}/roles/{role}", async (
            string userId,
            string role,
            HttpContext http,
            IUserAdminService service) =>
        {
            try
            {
                await service.RemoveRoleAsync(http.User, userId, role, http.RequestAborted);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ProblemMapper.From(ex);
            }
        }).RequireAuthorization();

        group.MapPut("/{userId}/password", async (
            string userId,
            PasswordBody request,
            HttpContext http,
            IUserAdminService service) =>
        {
            try
            {
                await service.SetPasswordAsync(http.User, userId, request.NewPassword, http.RequestAborted);
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
