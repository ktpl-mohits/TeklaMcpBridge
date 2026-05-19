using Contracts.Envelopes;
using Contracts.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Server.Hubs;
using Server.Services.Interface;

namespace Server.Services.Implementation
{
    public class TeklaRelayService : ITeklaRelayService
    {
        private readonly IHubContext<TeklaHub> _hubContext;
        private readonly IConnectionTracker tracker;

        public TeklaRelayService(IHubContext<TeklaHub> hubContext, IConnectionTracker tracker)
        {
            this._hubContext = hubContext;
            this.tracker = tracker;
        }
        public async Task<SharedResult> ExecuteToolCommandAsync<T>(string userId, string commandName, T payloadDto)
        {
            // Grab the current active connection ID for this user from the tracker
            var connectionId = tracker.GetConnectionId(userId);
            if (string.IsNullOrEmpty(connectionId))
            {
                return new SharedResult { Success = false, Message = $"User '{userId}' is not online." };
            }
            var request = new GenericEnvelope
            {
                CommandName = commandName,
                Payload = JsonConvert.SerializeObject(payloadDto)
            };
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var response = await _hubContext.Clients.Client(connectionId)
                    .InvokeAsync<GenericEnvelope>("ExecuteGenericCommand", request, cts.Token);

                if (response?.Payload == null)
                    return new SharedResult { Success = false, Message = "Local app returned empty payload." };

                return JsonConvert.DeserializeObject<SharedResult>(response.Payload)
                       ?? new SharedResult { Success = false, Message = "Failed to deserialize response." };
            }
            catch (OperationCanceledException)
            {
                return new SharedResult { Success = false, Message = "Timeout: Local PC did not respond." };
            }
            catch (Exception ex)
            {
                return new SharedResult { Success = false, Message = $"Connection Error: {ex.Message}" };
            }
        }
        public async Task<TResponse?> ExecuteAndReturnAsync<TRequest, TResponse>(string userId, string commandName, TRequest payloadDto)
        {
            var request = new GenericEnvelope
            {
                CommandName = commandName,
                Payload = JsonConvert.SerializeObject(payloadDto)
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var response = await ((ISingleClientProxy)_hubContext.Clients.User(userId))
                    .InvokeAsync<GenericEnvelope>("ExecuteGenericCommand", request, cts.Token);

                if (string.IsNullOrEmpty(response?.Payload))
                    return default; // Returns null for reference types if empty

                return JsonConvert.DeserializeObject<TResponse>(response.Payload);
            }
            catch (Exception ex)
            {
                // Log the error here if you have a logger
                Console.WriteLine($"Relay Error: {ex.Message}");
                return default;
            }
        }
    }
}
