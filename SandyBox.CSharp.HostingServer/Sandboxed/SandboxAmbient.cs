using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Client;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer.Sandboxed
{

    internal sealed class SandboxAmbient : MarshalByRefObject, IAmbient
    {

        private readonly IHostingClient hostingClient;

        internal SandboxAmbient(IHostingClient hostingClient, int sandboxId)
        {
            Debug.Assert(AppDomain.CurrentDomain.IsFullyTrusted, "This class should be instantiated in the main AppDomain.");
            this.hostingClient = hostingClient ?? throw new ArgumentNullException(nameof(hostingClient));
            SandboxId = sandboxId;
            Name = "C# Sandbox, " + sandboxId;
        }

        public string Name { get; }

        public int SandboxId { get; }

        public async Task<JToken> InvokeAsync(string name, JToken parameters)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            var result = await hostingClient.InvokeAmbient(name, parameters, SandboxId, CancellationToken.None);
            return result;
        }

    }
}
