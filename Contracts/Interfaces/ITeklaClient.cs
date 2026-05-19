using Contracts.Envelopes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interfaces
{
    public interface ITeklaClient
    {
        Task ForceDisconnect(string reason);
        Task<GenericEnvelope> ExecuteGenericCommand(GenericEnvelope request);  // only method SignalR pipe needs
    }
}
