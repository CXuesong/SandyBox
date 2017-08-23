using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.JsonRpc;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer.Ambient
{

    internal sealed class SandboxAmbient : MarshalByRefObject, IAmbient
    {

        private readonly IHostingClient hostingClient;

        internal SandboxAmbient(IHostingClient hostingClient, int sandboxId)
        {
            Debug.Assert(AppDomain.CurrentDomain.IsFullyTrusted, "This class should be instantiated in the main AppDomain.");
            this.hostingClient = hostingClient ?? throw new ArgumentNullException(nameof(hostingClient));
            SandboxId = sandboxId;
            Name = "C# Sandbox";
        }

        public string Name { get; }

        public int SandboxId { get; }

        public async Task<JToken> InvokeAsync(string name, JToken parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            try
            {
                return await hostingClient.InvokeAmbient(name, parameters, SandboxId, cancellationToken);
            }
            catch (JsonRpcException ex)
            {
                throw new RemotingException(ex.ToString());
            }
        }

    }
}
