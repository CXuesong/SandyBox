using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using Newtonsoft.Json.Linq;
using SandyBox.HostingService.Interop;

namespace SandyBox.HostingService.JsonRpc
{
    public class JsonRpcSandbox : Sandbox
    {

        private enum SandboxState
        {
            Created = 0,
            Started = 1,
            Stopped = 2,
            Disposed = 3,
        }

        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private SandboxState state = SandboxState.Created;

        protected internal JsonRpcSandbox(JsonRpcExecutionHost owner, string name) : base(name)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public JsonRpcExecutionHost Owner { get; }

        public int Id { get; private set; }

        public JsonRpcClient RpcClient { get; private set; }

        public ISandboxStub SandboxStub { get; private set; }

        // If initialization failed, the whole instance should just be discarded.
        internal async Task InitializeAsync()
        {
            if (state != SandboxState.Created) throw new InvalidOperationException();
            var pipeName = "SandyBox." + Guid.NewGuid();
            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            disposables.Add(pipe);
            Id = await Owner.HostStub.CreateSandbox(Name, pipeName);
            var procReader = new ByLineTextMessageReader(pipe) { LeaveReaderOpen = true };
            disposables.Add(procReader);
            var procWriter = new ByLineTextMessageWriter(pipe) { LeaveWriterOpen = true };
            disposables.Add(procWriter);
            // Wait for sandbox to start up.
            using (var cts = new CancellationTokenSource(5000))
            {
                Message startedMessage = null;
                try
                {
                    await pipe.WaitForConnectionAsync(cts.Token);
                    startedMessage = await procReader.ReadAsync(m =>
                            m is RequestMessage rm && rm.Method == "NotifyStarted",
                        cts.Token);
                }
                catch (OperationCanceledException)
                {

                }
                if (startedMessage == null)
                    throw new ExecutionHostException(Prompts.CannotStartExecutionHost_MissingNotifyStarted);
            }
            // HOST
            var hostBuilder = new JsonRpcServiceHostBuilder();
            hostBuilder.Register<HostingClientService>();
            var host = hostBuilder.Build();
            var serverHandler = new StreamRpcServerHandler(host);
            serverHandler.DefaultFeatures.Set<ISandboxContextFeature>(new SandboxContextFeature(Owner, this));
            disposables.Add(serverHandler.Attach(procReader, procWriter));

            // CLIENT
            var clientHandler = new StreamRpcClientHandler();
            RpcClient = new JsonRpcClient(clientHandler);
            disposables.Add(clientHandler.Attach(procReader, procWriter));
            SandboxStub = JsonRpcExecutionHost.ProxyBuilder.CreateProxy<ISandboxStub>(RpcClient);

            disposables.Reverse();      // Dispose in the reversed order.
            state = SandboxState.Started;
        }

        public override async Task LoadFromAsync(Stream sourceStream, string fileName)
        {
            if (sourceStream == null) throw new ArgumentNullException(nameof(sourceStream));
            using (var reader = new StreamReader(sourceStream))
            {
                var s = await reader.ReadToEndAsync();
                await Owner.HostStub.LoadSource(Id, s, fileName);
            }
        }

        public override async Task<JToken> ExecuteAsync(string functionName, JArray positionalParameters, JObject namedParameters)
        {
            return await SandboxStub.Invoke(functionName, positionalParameters, namedParameters);
        }

        protected override void Dispose(bool disposing)
        {
            if (state == SandboxState.Disposed) return;
            if (disposing)
            {
                if (state == SandboxState.Started)
                {
                    // Notify the client.
                    SandboxStub.Dispose();
                }
                foreach (var d in disposables)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }
                disposables.Clear();
            }
        }
    }
}
