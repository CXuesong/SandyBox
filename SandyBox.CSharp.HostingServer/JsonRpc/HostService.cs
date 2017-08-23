using System.Collections.Generic;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer.JsonRpc
{
    [JsonRpcScope(MethodPrefix = "Host.")]
    public class HostService : JsonRpcService
    {

        [JsonRpcMethod]
        public void Shutdown()
        {
            RequestContext.Features.Get<HostingServiceContext>().Dispose();
        }

    }
}
