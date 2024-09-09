using System.Net;

namespace OverflowBackend.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass the context to the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Handle the exception and log it
                _logger.LogError(ex, "An unhandled exception was catched in middleware.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // You can customize the response here, depending on the exception type
            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "Internal Server Error",
                detailed = exception.Message // Optional: include detailed error information
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
