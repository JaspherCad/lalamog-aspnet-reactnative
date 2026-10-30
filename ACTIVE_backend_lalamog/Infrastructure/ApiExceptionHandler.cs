using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Infrastructure
{
    public class ApiExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            bool conflict = exception is DbUpdateConcurrencyException;
            if (exception is DbUpdateException databaseException && databaseException.InnerException is SqlException sqlException)
            {
                if (sqlException.Number == 2601 || sqlException.Number == 2627)
                {
                    conflict = true;
                }
            }

            int status;
            if (exception is ApiException apiException)
            {
                status = apiException.StatusCode;
            }
            else if (conflict)
            {
                status = StatusCodes.Status409Conflict;
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
            }

            string title;
            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
                title = "An unexpected error occurred.";
            }
            else if (conflict)
            {
                title = "The record changed. Refresh and try again.";
            }
            else
            {
                title = exception.Message;
            }

            context.Response.StatusCode = status;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}
