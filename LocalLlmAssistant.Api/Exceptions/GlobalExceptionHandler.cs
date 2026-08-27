using Microsoft.AspNetCore.Diagnostics;

namespace LocalLlmAssistant.Api.Exceptions;
//IExceptionHandler this interface for handling exceptions globally in the application. 
//It can be implemented to provide custom exception handling logic, such as logging, returning specific error responses, or performing other actions when an unhandled exception occurs.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    //asp.net core calls this method when an unhandled exception occurs during the processing of an HTTP request.
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception details for debugging and monitoring purposes
        _logger.LogError(
            exception,
            "An unhandled exception occurred.");



        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                error = "An unexpected error occurred."
            },
            cancellationToken);

        return true;
    }
}