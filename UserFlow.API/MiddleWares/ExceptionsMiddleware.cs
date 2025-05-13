/// @file ExceptionsMiddleware.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Middleware to handle exceptions globally and provide consistent error responses.
/// @details
/// This middleware catches all unhandled exceptions during the request processing pipeline.
/// It logs the exception details and returns a JSON response to the client with a proper error message and HTTP status code.
/// In development mode, the response includes the exception message and stack trace, while in production, it hides internal details for security purposes.

using System.Net;
using System.Text.Json;

namespace UserFlow.API.Middleware
{
    /// <summary>
    /// 👉 ✨ Middleware to catch and handle all unhandled exceptions globally.
    /// </summary>
    public class ExceptionsMiddleware
    {
        /// <summary>
        /// 👉 ✨ The next delegate in the HTTP request pipeline.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 👉 ✨ Logger for writing structured exception messages.
        /// </summary>
        private readonly ILogger<ExceptionsMiddleware> _logger;

        /// <summary>
        /// 👉 ✨ Hosting environment to determine whether to expose stack traces.
        /// </summary>
        private readonly IHostEnvironment _env;

        /// <summary>
        /// 👉 ✨ JSON options for camelCase serialization.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 🐫 Ensure camelCase keys in JSON
        };

        /// <summary>
        /// 👉 ✨ Constructor for the middleware to inject dependencies.
        /// </summary>
        /// <param name="next">Next middleware in the pipeline.</param>
        /// <param name="logger">Logger for capturing exceptions.</param>
        /// <param name="env">Hosting environment to determine error detail visibility.</param>
        public ExceptionsMiddleware(RequestDelegate next, ILogger<ExceptionsMiddleware> logger, IHostEnvironment env)
        {
            _next = next;       // 🔗 Store the next delegate in the pipeline
            _logger = logger;   // 📝 Capture logger instance
            _env = env;         // 🌍 Store environment context
        }

        /// <summary>
        /// 👉 ✨ Intercepts the HTTP pipeline to handle exceptions.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>Task that completes when the middleware is done processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                /// 🚀 Pass control to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                /// ❌ Log the exception using Microsoft.Extensions.Logging
                _logger.LogError(ex, "An unexpected error occurred while processing the request.");

                /// 📦 Serilog structured error logging
                _logger.LogError(ex, "Unhandled Exception caught by global middleware");

                /// 📡 Set response content type and HTTP 500 status
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                /// 🧠 Show full details in development, hide them in production
                var response = _env.IsDevelopment()
                    ? new { message = ex.Message, stackTrace = ex.StackTrace ?? string.Empty }
                    : new { message = "Internal Server Error", stackTrace = string.Empty };

                /// 📤 Serialize error response and send to client
                var json = JsonSerializer.Serialize(response, _jsonOptions);
                await context.Response.WriteAsync(json);
            }
        }
    }
}

/// @remarks
/// Developer Notes:
/// - 🛡️ Centralized error handling improves consistency and security across the API.
/// - 📡 Returns all errors in a JSON format, making it easy for frontends to parse.
/// - 🔍 Includes full exception message and stack trace only in development mode.
/// - 🧼 Prevents internal exception details from leaking in production.
/// - 📦 Add more fields (e.g. errorId, timestamp) to the response object if needed for debugging or auditing.
