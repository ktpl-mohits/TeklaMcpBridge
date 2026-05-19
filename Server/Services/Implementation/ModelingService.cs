using Contracts.Envelopes;
using Contracts.Interfaces;
using Contracts.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Server.Hubs;
using Server.Services.Interface;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Server.Services.Implementation
{
    public class ModelingService : IModelingService
    {
        
        private readonly ITeklaRelayService _relay;

        public ModelingService(ITeklaRelayService relay)
        {
          
            _relay = relay;
        }

        public async Task<SharedResult> CreateSimpleBeamAsync(string userId)
        {

            Random rnd = new Random();
            string profile = "ISMB400";
            double x = rnd.Next(0, 10001), y = rnd.Next(0, 10001), h = rnd.Next(2000, 5000);
            var dto = new SimpleBeamCreateDto
            {
                Profile = profile,
                Material = "43A",
                StartX = x,
                StartY = y,
                StartH = 0,
                EndX = x,
                EndY = y,
                EndH = h,
            };
            // call the relay service


            return await _relay.ExecuteToolCommandAsync(userId, "CreateSimpleBeam", dto);
            
        }

        public async Task<SharedResult> DeleteBeamAsync(string userId)
        {
            
            return await _relay.ExecuteToolCommandAsync(userId, "DeleteRandomBeam", "{}");


        }

        public async Task<SharedResult> DesignAndCreateBeamAsync(string userId, double span, double load)
        {

            string profile = (span * load > 50000) ? "ISMB400" : "ISMB300";

            var dto = new BeamCreateDto
            {
                Profile = profile,
                Material = "S355JR",
                StartX = 0,
                EndX = 1000,
            };

            ;
            return await _relay.ExecuteToolCommandAsync(userId, "CreateBeam", dto);

        }

        public async Task<SharedResult> UpdateBeamClassByProfileAsync(string userId, string profileName, int newClass)
        {

            //string connectionId = _tracker.GetConnectionId(userId);

            //if (string.IsNullOrEmpty(connectionId))
            //{
            //    return new SharedResult { Success = false, Message = $"Local PC for user '{userId}' is not connected." };
            //}
            //;

            //// 1. FETCH STATE FROM LOCAL APP
            //var getRequest = new GenericEnvelope { CommandName = "GetAllBeamsSummary", Payload = "{}" };
            //var getResponse = await _hubContext.Clients.Client(connectionId).ExecuteGenericCommand(getRequest);

            //var allBeams = JsonConvert.DeserializeObject<List<BeamSummaryDto>>(getResponse.Payload);

            var allBeams = await _relay.ExecuteAndReturnAsync<object, List< BeamSummaryDto >> (userId, "GetAllBeamsSummary", new { });

            // Guard clause if the fetch failed or timed out
            if (allBeams == null)
            {
                return new SharedResult { Success = false, Message = "Failed to fetch beams from local Tekla instance. Is it running?" };
            }
            // 2. SERVER-SIDE BUSINESS LOGIC (The Brain)
            var guidsToUpdate = allBeams
            .Where(b => b.Profile.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Guid)
            .ToList();
            if (!guidsToUpdate.Any())
            {
                return new SharedResult { Success = false, Message = $"No beams found with profile {profileName}." };
            }

            // 3. SEND COMMAND TO LOCAL APP
            var updateDto = new UpdateBeamsByGuidDto { TargetGuids = guidsToUpdate, NewClass = newClass };
            //var updateRequest = new GenericEnvelope
            //{
            //    CommandName = "UpdateBeamProfile",
            //    Payload = JsonConvert.SerializeObject(updateDto)
            //};

            //var updateResponse = await _hubContext.Clients.Client(connectionId).ExecuteGenericCommand(updateRequest);
            //return JsonConvert.DeserializeObject<SharedResult>(updateResponse.Payload);
            return await _relay.ExecuteToolCommandAsync(userId, "UpdateBeamProfile", updateDto);


        }
    }
}
