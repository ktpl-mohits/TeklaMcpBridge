using Server.Services.Interface;
using Server.Context;
namespace Server.Middleware
{
    public class McpUserMiddleware
    {
        private readonly RequestDelegate _next;
        public McpUserMiddleware(RequestDelegate next)

        {
            _next = next;
            
        }
        public async Task InvokeAsync(HttpContext context, MCPCallContext mcpContext)
        {
            // Extract the userId from the query string
            var userId = context.Request.Query["userId"].ToString();

            // 2. Fallback: Try the Header (Covers Local App if using headers)
            if (string.IsNullOrEmpty(userId))
            {
                userId = context.Request.Headers["X-Tekla-User-Id"];
            }

            if (string.IsNullOrEmpty(userId))
            {
                // Stop the request here and return a 401 or a custom message
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Error: userId is required to access Tekla tools.");
                return;
            }

            mcpContext.UserId = userId;
            await _next(context);
        }
    }
}
