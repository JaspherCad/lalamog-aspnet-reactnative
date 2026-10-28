using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace Lalamog.Api.Infrastructure;
public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var conflict = exception is DbUpdateConcurrencyException
            || exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } };
        var status = exception is ApiException error ? error.StatusCode : conflict ? 409 : 500;
        if (status == 500) logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 500 ? "An unexpected error occurred."
                : conflict ? "The record changed. Refresh and try again." : exception.Message,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}