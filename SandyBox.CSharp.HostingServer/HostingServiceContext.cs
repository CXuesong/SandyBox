using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Client;

namespace SandyBox.CSharp.HostingServer
{
    public class HostingServiceContext : IDisposable
    {
        private static readonly JsonRpcProxyBuilder proxyBuilder = new JsonRpcProxyBuilder();

        private int counter = 0;
        private ConcurrentDictionary<int, Sandbox> sandboxes = new ConcurrentDictionary<int, Sandbox>();
        private readonly TaskCompletionSource<bool> disposalTcs = new TaskCompletionSource<bool>();

        public HostingServiceContext(JsonRpcClient rpcClient, string sandboxWorkPath)
        {
            SandboxWorkPath = sandboxWorkPath;
            Client = proxyBuilder.CreateProxy<IHostingClient>(rpcClient);
        }

        public string SandboxWorkPath { get; }

        public Task Disposal => disposalTcs.Task;

        private readonly ModuleCompiler compiler;

        public IHostingClient Client { get; }

        public int CreateSandbox(string sandboxName)
        {
            var id = Interlocked.Increment(ref counter);
            var folderName = $"Sandbox{GetHashCode()}#{counter}";
            int folderNameSuffix = 0;
            while (Directory.Exists(Path.Combine(SandboxWorkPath, folderName)))
            {
                folderNameSuffix++;
                folderName = $"Sandbox{GetHashCode()}#{counter}#{folderNameSuffix}";
            }
            var workPath = Path.Combine(SandboxWorkPath, folderName);
            Directory.CreateDirectory(workPath);
            var result = sandboxes.TryAdd(id, new Sandbox(sandboxName, workPath));
            Debug.Assert(result);
            return id;
        }

        public Sandbox GetSandbox(int id)
        {
            return sandboxes[id];
        }

        public void TerminateSandbox(int id)
        {
            if (!sandboxes.TryRemove(id, out var sb))
                throw new ArgumentException("Invalid id.", nameof(id));
            sb.Dispose();
        }

        public void Dispose()
        {
            if (!disposalTcs.TrySetResult(true)) return;
            var dict = Interlocked.Exchange(ref sandboxes, null);
            foreach (var sb in dict.Values)
                sb.Dispose();
        }
    }

}
