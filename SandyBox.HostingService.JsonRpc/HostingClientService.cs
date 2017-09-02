using System;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.JsonRpc
{

    public sealed class HostingClientService : JsonRpcService
    {

        [JsonRpcMethod]
        public Task<JToken> InvokeAmbient(string methodName, JToken parameters, int sandbox,
            CancellationToken cancellationToken)
        {
            var host = RequestContext.GetExecutionHost();
            var sb = host.TryGetSandbox(sandbox);
            var handler = sb.InvokeAmbientHandler ?? host.InvokeAmbientHandler;
            if (handler == null) throw new NotSupportedException("JsonRpcExecutionHost.InvokeAmbientHandler is null.");
            return handler(methodName, parameters, sb, cancellationToken);
        }

    }

}
