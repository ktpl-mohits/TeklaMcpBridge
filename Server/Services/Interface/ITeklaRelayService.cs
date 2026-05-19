using Contracts.Models;

namespace Server.Services.Interface
{
    public interface ITeklaRelayService
    {
        Task<SharedResult> ExecuteToolCommandAsync<T>(String userId, string commandName, T payloadDto);
        Task<TResponse?>ExecuteAndReturnAsync<TRequest, TResponse>(string userId, string commandName, TRequest payloadDto);
    }
}
