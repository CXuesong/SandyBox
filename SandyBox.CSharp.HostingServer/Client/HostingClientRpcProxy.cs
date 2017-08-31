using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer.Client
{
    /// <summary>
    /// A hand-crafted RPC client proxy class.
    /// Notice we cannot build dynamic assemblies in sandbox.
    /// </summary>
    internal sealed class HostingClientRpcProxy : IHostingClient
    {

        internal HostingClientRpcProxy(JsonRpcClient rpcClient)
        {
            RpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        }

        public JsonRpcClient RpcClient { get; }
        
        public void NotifyStarted()
        {
            var forgetIt = RpcClient.SendNotificationAsync(nameof(NotifyStarted), null, CancellationToken.None);
        }

        public async Task<JToken> InvokeAmbient(string methodName, JToken parameters, int sandbox, CancellationToken cancellationToken)
        {
            var p = new JObject
            {
                {nameof(methodName), methodName},
                {nameof(parameters), parameters},
                {nameof(sandbox), sandbox}
            };
            var response = await RpcClient.SendRequestAsync(nameof(InvokeAmbient), p, cancellationToken);
            if (response.Error != null) throw new JsonRpcException(response.Error);
            return response.Result;
        }

    }
}
