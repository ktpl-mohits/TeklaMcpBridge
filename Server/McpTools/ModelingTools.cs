using ModelContextProtocol.Server;
using Server.Services.Implementation;
using Server.Context;
using Server.Services.Interface;
using System.ComponentModel;

namespace Server.McpTools
{
    [McpServerToolType]
    public class ModelingTools
    {
        private readonly IModelingService _modelingService;
        private readonly MCPCallContext mcpContext;
    

        public ModelingTools(IModelingService modelingService,MCPCallContext mcpContext)
        {
            _modelingService = modelingService;
            this.mcpContext = mcpContext;
            
      
        }
        [McpServerTool, Description("Designs and creates a beam in Tekla based on span and load")]
        public async Task<string> CreateBeam( double span, double load)
        {

         
            var result = await _modelingService.DesignAndCreateBeamAsync(mcpContext.UserId, span, load);

            return result.Success
                ? $"Success! Beam ID: {result.CreatedObjectGuid}"
                : $"Failed: {result.Message}";
        }

        [McpServerTool, Description("Creates a beam in Tekla")]
        public async Task<string> CreateSimpleBeam()
        {


            var result = await _modelingService.CreateSimpleBeamAsync(mcpContext.UserId);

            return result.Success
                ? $"Success! Beam ID: {result.CreatedObjectGuid}"
                : $"Failed: {result.Message}";
        }

        [McpServerTool, Description("Delete a beam in Tekla")]
        public async Task<string> DeleteBeam()
        {


            var result = await _modelingService.DeleteBeamAsync(mcpContext.UserId);

            return result.Success
                ? $"Success! {result.Message}"
                : $"Failed: {result.Message}";
        }
        [McpServerTool, Description("Finds all beams with a specific profile and updates their Class attribute.")]
        public async Task<string> UpdateBeamClassByProfile(
        [Description("The profile string to filter for, e.g., 'ISMB300'")] string profileName,
        [Description("The new class number to assign, e.g., 5")] int newClass)
        {

            var result = await _modelingService.UpdateBeamClassByProfileAsync(mcpContext.UserId, profileName, newClass);

            return result.Success
                ? $"Success! {result.Message}"
                : $"Failed: {result.Message}";
        }

    }
}
