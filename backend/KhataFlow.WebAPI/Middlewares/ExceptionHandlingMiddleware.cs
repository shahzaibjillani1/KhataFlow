using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IStringLocalizer<SharedResource> localizer)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Type}: {Message}",
                ex.GetType().Name, ex.Message);

            await HandleExceptionAsync(context, ex, localizer);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, Exception ex, IStringLocalizer<SharedResource> localizer)
    {
        var (statusCode, message) = ex switch
        {
            BusinessNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, e.Message),
            DomainException e => (StatusCodes.Status400BadRequest, ResolveDomainMessage(e, localizer)),
            KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),
            _ => (StatusCodes.Status500InternalServerError,
                                            "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            result = false,
            message,
            statusCode
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static string ResolveDomainMessage(DomainException ex, IStringLocalizer<SharedResource> localizer)
    {
        if (!ex.IsResourceKey || ex.ResourceKey is null)
            return ex.Message;

        var localized = localizer[ex.ResourceKey, ex.Args];

        return localized.ResourceNotFound ? ex.ResourceKey : localized.Value;
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(
        this IApplicationBuilder builder)
        => builder.UseMiddleware<ExceptionHandlingMiddleware>();
}