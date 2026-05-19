using Microsoft.AspNetCore.SignalR;
using Contracts.Interfaces;
using Server.Services.Interface;

namespace Server.Hubs
{
    public class TeklaHub : Hub<ITeklaClient>
    {
        private readonly IConnectionTracker tracker;

        public TeklaHub(IConnectionTracker tracker)
        {
            this.tracker = tracker;
        }
        public async override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // 1. Find if there is an existing old connection ID
                var oldConnectionId = tracker.GetConnectionId(userId);
                if (!string.IsNullOrEmpty(oldConnectionId))
                {
                    // Target ONLY the old connection directly, NOT Clients.User()
                    await Clients.Client(oldConnectionId).ForceDisconnect("New instance started.");
                    await Task.Delay(500);
                }

                // 2. Track the brand-new connection ID
                tracker.Register(userId, Context.ConnectionId);
            }

            Console.WriteLine($"[Hub] User '{Context.UserIdentifier}' connected with ID '{Context.ConnectionId}'.");
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                tracker.Unregister(userId, Context.ConnectionId);
            }
            return base.OnDisconnectedAsync(exception);
        }

    }
}
