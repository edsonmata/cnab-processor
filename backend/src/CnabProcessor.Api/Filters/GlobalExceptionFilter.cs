using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace CnabProcessor.Api.Filters
{
    /// <summary>
    /// Global exception filter that handles unhandled exceptions across the API.
    /// Provides consistent error responses and logging.
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            _logger.LogError(
                exception,
                "Unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.HttpContext.Request.Path,
                context.HttpContext.Request.Method);

            // Determine status code and error message based on exception type
            var (statusCode, title, detail) = MapExceptionToResponse(exception);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.HttpContext.Request.Path
            };

            // Add trace ID for correlation
            if (context.HttpContext.TraceIdentifier != null)
            {
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = statusCode
            };

            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Maps exception types to appropriate HTTP status codes and messages.
        /// </summary>
        private (int statusCode, string title, string detail) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException argEx => (
                    (int)HttpStatusCode.BadRequest,
                    "Invalid argument",
                    $"Required parameter '{argEx.ParamName}' was not provided"
                ),

                ArgumentException argEx => (
                    (int)HttpStatusCode.BadRequest,
                    "Invalid argument",
                    argEx.Message
                ),

                InvalidOperationException invalidOpEx => (
                    (int)HttpStatusCode.BadRequest,
                    "Invalid operation",
                    invalidOpEx.Message
                ),

                UnauthorizedAccessException _ => (
                    (int)HttpStatusCode.Forbidden,
                    "Access denied",
                    "You do not have permission to access this resource"
                ),

                FormatException formatEx => (
                    (int)HttpStatusCode.BadRequest,
                    "Format error",
                    formatEx.Message
                ),

                TimeoutException _ => (
                    (int)HttpStatusCode.RequestTimeout,
                    "Request timeout",
                    "The request took too long to process"
                ),

                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "Internal server error",
                    "An unexpected error occurred while processing your request"
                )
            };
        }
    }
}