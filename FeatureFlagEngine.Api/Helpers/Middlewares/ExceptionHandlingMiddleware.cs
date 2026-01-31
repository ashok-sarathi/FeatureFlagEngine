using FeatureFlagEngine.Domain.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace FeatureFlagEngine.Api.Helpers.Middlewares
{
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception occurred");

                string message = ex.Message;

                HttpStatusCode statusCode = ex switch
                {
                    BadRequestException => HttpStatusCode.BadRequest,
                    NotFoundException => HttpStatusCode.NotFound,
                    _ => HttpStatusCode.InternalServerError
                };
                
                var response = new
                {
                    error = message,
                    statusCode = (int)statusCode,
                    traceId = context.TraceIdentifier
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)statusCode;

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
