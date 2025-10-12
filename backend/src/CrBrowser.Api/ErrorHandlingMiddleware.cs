using System.Net;
using System.Text.Json;

namespace CrBrowser.Api;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception occurred");
            await HandleHttpRequestExceptionAsync(context, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request cancelled");
            await HandleTaskCanceledExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleHttpRequestExceptionAsync(HttpContext context, HttpRequestException exception)
    {
        var statusCode = exception.StatusCode switch
        {
            HttpStatusCode.NotFound => 404,
            HttpStatusCode.TooManyRequests => 429,
            HttpStatusCode.BadGateway => 502,
            HttpStatusCode.ServiceUnavailable => 503,
            HttpStatusCode.GatewayTimeout => 504,
            _ => 502
        };

        var errorCode = exception.StatusCode switch
        {
            HttpStatusCode.NotFound => "NotFound",
            HttpStatusCode.TooManyRequests => "RateLimited",
            _ => "UpstreamError"
        };

        var retryable = statusCode >= 500 || statusCode == 429;

        var response = new ErrorResponse(errorCode, $"Upstream error: {exception.Message}", retryable);
        await WriteJsonResponseAsync(context, response, statusCode);
    }

    private static async Task HandleTaskCanceledExceptionAsync(HttpContext context, TaskCanceledException exception)
    {
        var response = new ErrorResponse("RequestTimeout", "Request was cancelled or timed out", true);
        await WriteJsonResponseAsync(context, response, 408);
    }

    private static async Task HandleUnhandledExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse("InternalError", "An internal error occurred", false);
        await WriteJsonResponseAsync(context, response, 500);
    }

    private static async Task WriteJsonResponseAsync(HttpContext context, ErrorResponse errorResponse, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse, options);
    }
}
