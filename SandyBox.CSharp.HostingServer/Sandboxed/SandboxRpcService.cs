using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Host;

namespace SandyBox.CSharp.HostingServer.Sandboxed
{
    [JsonRpcScope(MethodPrefix = "Sandbox.")]
    public class SandboxRpcService : JsonRpcService
    {

        [JsonRpcMethod]
        public async Task<object> Invoke(string name, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            var loader = RequestContext.Features.Get<SandboxLoader>();
            try
            {
                var result = await loader.InvokeAsync(name, positionalParameters, namedParameters)
                    .ConfigureAwait(false);
                return result;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Laundry exception.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // Will not reach here.
                throw;
            }
        }

        [JsonRpcMethod(IsNotification = true)]
        public void Dispose()
        {
            RequestContext.Features.Get<SandboxLoader>().Dispose();
        }

    }
}
