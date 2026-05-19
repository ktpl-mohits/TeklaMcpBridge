using Microsoft.AspNetCore.SignalR;

namespace Server.Services.Implementation

{
    // provides userId used by IHubClients to invoke connections associated with a user (abstracts connection mapping)
    public class TeklaUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var httpContext = connection.GetHttpContext();
            if (httpContext == null) return null;

            // 1. Check Query String (Matches Middleware)
            string userId = httpContext.Request.Query["userId"].ToString();

            // 2. Fallback: Check Header (Matches Middleware)
            if (string.IsNullOrEmpty(userId))
            {
                userId = httpContext.Request.Headers["X-Tekla-User-Id"].ToString();
            }

            // SignalR treats null/empty as anonymous. 
            // Returning it here "labels" the socket for Clients.User(userId)
            return string.IsNullOrEmpty(userId) ? null : userId;
        }
    
}
}
