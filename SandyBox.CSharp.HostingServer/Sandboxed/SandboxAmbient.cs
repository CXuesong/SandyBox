using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Client;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer.Sandboxed
{

    internal sealed class SandboxAmbient : IAmbient
    {

        private readonly IHostingClient hostingClient;

        internal SandboxAmbient(IHostingClient hostingClient, int sandboxId)
        {
            this.hostingClient = hostingClient ?? throw new ArgumentNullException(nameof(hostingClient));
            SandboxId = sandboxId;
            Name = "C# Sandbox, " + sandboxId;
        }

        public string Name { get; }

        public int SandboxId { get; }

        // Cannot use SecuritySafeCriticalAttribute with async
        // See https://github.com/dotnet/roslyn/issues/15244 .
        [SecuritySafeCritical]
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        public Task<JToken> InvokeAsync(string name, JToken parameters)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            return hostingClient.InvokeAmbient(name, parameters, SandboxId, CancellationToken.None);
        }

    }
}
