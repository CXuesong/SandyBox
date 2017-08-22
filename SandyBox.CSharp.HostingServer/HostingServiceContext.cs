using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using JsonRpc.Standard.Client;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer
{
    public class HostingServiceContext : IDisposable
    {
        private static readonly JsonRpcProxyBuilder proxyBuilder = new JsonRpcProxyBuilder();

        private int counter = 0;
        private ConcurrentDictionary<int, Sandbox> sandboxes = new ConcurrentDictionary<int, Sandbox>();
        private readonly TaskCompletionSource<bool> disposalTcs = new TaskCompletionSource<bool>();

        private static readonly List<string> preloadedLibraries;

        public HostingServiceContext(JsonRpcClient rpcClient, string sandboxWorkPath)
        {
            SandboxWorkPath = sandboxWorkPath;
            Client = proxyBuilder.CreateProxy<IHostingClient>(rpcClient);
        }

        static HostingServiceContext()
        {
            preloadedLibraries = new List<string>
            {
                typeof(Uri).Assembly.Location, // System
                typeof(HashSet<>).Assembly.Location, // System.Core
                typeof(Configuration).Assembly.Location,
                typeof(JToken).Assembly.Location,
                typeof(XmlDocument).Assembly.Location,
                typeof(XObject).Assembly.Location,
                typeof(Complex).Assembly.Location,
                typeof(XmlObjectSerializer).Assembly.Location,
                typeof(DataColumn).Assembly.Location,
            };
            // mscorlib & netstandard
            preloadedLibraries.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetType("System.Object") != null)
                .Select(a => a.Location));
        }

        public string SandboxWorkPath { get; }

        public Task Disposal => disposalTcs.Task;

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
            var sandbox = new Sandbox(sandboxName, workPath, preloadedLibraries);
            var result = sandboxes.TryAdd(id, sandbox);
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
