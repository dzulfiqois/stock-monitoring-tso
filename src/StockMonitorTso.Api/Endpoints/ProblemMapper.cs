using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace StockMonitorTso.Api.Endpoints;

/// <summary>Pemetaan exception service → ProblemDetails (RFC 7807): 409/403/404/400/500.</summary>
public static class ProblemMapper
{
    public static IResult From(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException ex => Results.Problem(ex.Message, statusCode: 409),
        UnauthorizedAccessException ex => Results.Problem(ex.Message, statusCode: 403),
        KeyNotFoundException ex => Results.Problem(ex.Message, statusCode: 404),
        ArgumentOutOfRangeException ex => Results.Problem(ex.Message, statusCode: 400),
        ArgumentException ex => Results.Problem(ex.Message, statusCode: 400),
        InvalidOperationException ex => Results.Problem(ex.Message, statusCode: 400),
        _ => Results.Problem("Terjadi kesalahan tak terduga.", statusCode: 500),
    };
}
