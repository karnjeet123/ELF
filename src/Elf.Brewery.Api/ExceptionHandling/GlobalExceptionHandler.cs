using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }



    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = MapExceptionToResponse(exception);
        _logger.LogError(exception, "Unhandled {Exception} -> {Status}", exception.GetType().Name, statusCode);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Message) MapExceptionToResponse(Exception exception)
    {
        switch (exception)
        {
            case BreweryNotFoundException:
                return (StatusCodes.Status404NotFound, "Not found");
            case ValidationException:
                return (StatusCodes.Status400BadRequest, "Invalid request");
            case ExternalServiceException:
                return (StatusCodes.Status502BadGateway, "Upstream service unavailable");
            case OperationCanceledException:
                return (499, "Client closed request");
            default:
                return (StatusCodes.Status500InternalServerError, "Unexpected error");
        }
    }
}