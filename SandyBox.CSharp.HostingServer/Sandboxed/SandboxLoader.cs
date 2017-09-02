using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Client;
using SandyBox.CSharp.HostingServer.Host;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer.Sandboxed
{
    /// <summary>
    /// A loader class used to bootstrap in the sandbox appdomain.
    /// </summary>
    internal sealed class SandboxLoader : MarshalByRefObject, IDisposable
    {

        private static readonly JsonRpcProxyBuilder proxyBuilder = new JsonRpcProxyBuilder();

        private readonly HostCallbackHandler hostCallback;
        private IModule _ClientModule;
        private readonly Dictionary<string, IList<MethodInfo>> nameMethodDict = new Dictionary<string, IList<MethodInfo>>();
        private IHostingClient hostingClient;
        private readonly string pipeName;
        private readonly TaskCompletionSource<bool> disposalTcs = new TaskCompletionSource<bool>();

        internal SandboxLoader(int sandboxId, string pipeName, HostCallbackHandler hostCallback)
        {
            SandboxId = sandboxId;
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            this.hostCallback = hostCallback ?? throw new ArgumentNullException(nameof(hostCallback));
            var forgetit = RunAsync();
        }

        public int SandboxId { get; }

        private static IJsonRpcServiceHost BuildServiceHost()
        {
            var builder = new JsonRpcServiceHostBuilder();
            builder.Register<SandboxRpcService>();
            return builder.Build();
        }

        [SecuritySafeCritical]
        private static IHostingClient BuildHostingClient(JsonRpcClient client)
        {
            Debug.Assert(client != null);
            return proxyBuilder.CreateProxy<IHostingClient>(client);
        }

        private async Task RunAsync()
        {
            var host = BuildServiceHost();
            var serverHandler = new StreamRpcServerHandler(host);
            var clientHandler = new StreamRpcClientHandler();
            var client = new JsonRpcClient(clientHandler);
            using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var connectTask = pipe.ConnectAsync();
                hostingClient = BuildHostingClient(client);
                serverHandler.DefaultFeatures.Set(this);
                var reader = new ByLineTextMessageReader(pipe) { LeaveReaderOpen = true };
                var writer = new ByLineTextMessageWriter(pipe) { LeaveWriterOpen = true };

                Ambient = new SandboxAmbient(hostingClient, SandboxId);

                await connectTask;
                using (reader)
                using (writer)
                using (serverHandler.Attach(reader, writer))
                using (clientHandler.Attach(reader, writer))
                {
                    // Started up
                    hostingClient.NotifyStarted();
                    // Wait for disposal
                    await disposalTcs.Task;
                }
            }
            // Dispose
            if (_ClientModule != null)
            {
                if (_ClientModule is IDisposable d) d.Dispose();
                _ClientModule = null;
            }
            // Final cleanup.
            // The owner will unload appdomain so a ThreadAbortException should be thrown here.
            hostCallback.NotifySandboxDisposed(SandboxId);
        }

        public IAmbient Ambient { get; private set; }

        public void LoadAssembly(string assemblyPath)
        {
            Assembly.LoadFrom(assemblyPath);
        }

        public void LoadModule(string assemblyPath)
        {
            if (assemblyPath == null) throw new ArgumentNullException(nameof(assemblyPath));
            Debug.Assert(_ClientModule == null);
            var assembly = Assembly.LoadFrom(assemblyPath);
            Type moduleType;
            {
                var matches = assembly.GetExportedTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract).Take(2).ToArray();
                if (matches.Length == 0) throw new MissingModuleException();
                if (matches.Length > 1) throw new AmbiguousModuleException();
                moduleType = matches[0];
            }
            IModule localModule = null;
            try
            {
                localModule = (IModule)Activator.CreateInstance(moduleType);
                localModule.Initialize(Ambient);
                foreach (var method in moduleType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                             BindingFlags.Public))
                {
                    if (!nameMethodDict.TryGetValue(method.Name, out var list))
                    {
                        list = new List<MethodInfo>();
                        nameMethodDict.Add(method.Name, list);
                    }
                    list.Add(method);
                }
                _ClientModule = localModule;
            }
            catch (Exception ex)
            {
                (localModule as IDisposable)?.Dispose();
                throw new ModuleLoaderException(ex);
            }
        }

        public async Task<JToken> InvokeAsync(string functionName, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(functionName));
            if (_ClientModule == null) throw new InvalidOperationException();
            IList<MethodInfo> candidates;
            try
            {
                candidates = nameMethodDict[functionName];
            }
            catch (KeyNotFoundException)
            {
                throw new MissingMethodException(_ClientModule.GetType().Name, functionName);
            }
            var method = ModuleFunctionBinder.BindMethod(candidates, _ClientModule.GetType(), functionName,
                positionalParameters, namedParameters);
            var parameters = ModuleFunctionBinder.BindParameters(method, positionalParameters, namedParameters);
            var result = method.Invoke(_ClientModule, parameters);
            if (method.ReturnParameter.ParameterType == typeof(void)) return null;
            if (result is Task t)
            {
                await t.ConfigureAwait(false);
                result = result.GetType().GetProperty("Result").GetValue(result);
            }
            return ModuleFunctionBinder.SerializeReturnValue(result);
        }

        public IModule ClientModule => _ClientModule;

        /// <summary>
        /// Notifies the loader the sandbox is to be cleaned up.
        /// </summary>
        public void Dispose()
        {
            disposalTcs.TrySetResult(true);
        }

    }

}
