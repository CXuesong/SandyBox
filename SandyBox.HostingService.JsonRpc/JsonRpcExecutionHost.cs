using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using SandyBox.HostingService.Interop;

namespace SandyBox.HostingService.JsonRpc
{
    public class JsonRpcExecutionHost : ExecutionHost
    {

        private readonly Lazy<Task<Process>> _Process;
        private List<IDisposable> disposables = new List<IDisposable>();
        private static readonly JsonRpcProxyBuilder proxyBuilder = new JsonRpcProxyBuilder();

        public JsonRpcExecutionHost(string executablePath, string workingDirectory) : this(executablePath, workingDirectory, null)
        {
        }

        public JsonRpcExecutionHost(string executablePath, string workingDirectory, string executableParameters)
        {
            ExecutablePath = Path.GetFullPath(executablePath);
            WorkingDirectory = Path.GetFullPath(workingDirectory);
            ExecutableParameters = executableParameters;
            _Process = new Lazy<Task<Process>>(StartProcessAsync);
        }

        public string ExecutablePath { get; }

        public string ExecutableParameters { get; }

        public string WorkingDirectory { get; }

        protected Process HostingServerProcess
        {
            get
            {
                if (!_Process.IsValueCreated) return null;
                if (_Process.Value.Status == TaskStatus.RanToCompletion) return _Process.Value.Result;
                return null;
            }
        }

        public JsonRpcClient RpcClient { get; private set; }

        public IHostingServerStub HostingServerStub { get; private set; }

        protected virtual Process StartExecutionProcess(AnonymousPipeServerStream txPipe, AnonymousPipeServerStream rxPipe)
        {
            var startInfo = new ProcessStartInfo(ExecutablePath)
            {
                Arguments = string.Format("-TxPipe:{0} -RxPipe:{1}",
                    rxPipe.GetClientHandleAsString(),
                    txPipe.GetClientHandleAsString()),
                WorkingDirectory = WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            if (!string.IsNullOrEmpty(ExecutableParameters))
                startInfo.Arguments += " " + ExecutableParameters;
            return Process.Start(startInfo);
        }

        protected virtual void ConfigServiceHost(JsonRpcServiceHostBuilder builder)
        {

        }

        private Task EnsureProcessStartedAsync()
        {
            return _Process.Value;
        }

        private async Task<Process> StartProcessAsync()
        {
            Directory.CreateDirectory(WorkingDirectory);
            Process process = null;
            var localDisposables = new List<IDisposable>();
            try
            {
                var txPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                localDisposables.Add(txPipe);
                var rxPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                localDisposables.Add(rxPipe);
                process = StartExecutionProcess(txPipe, rxPipe);
                localDisposables.Add(process);
                txPipe.DisposeLocalCopyOfClientHandle();
                rxPipe.DisposeLocalCopyOfClientHandle();

                var procReader = new ByLineTextMessageReader(rxPipe);
                localDisposables.Add(procReader);
                localDisposables.Remove(rxPipe);
                var procWriter = new ByLineTextMessageWriter(txPipe);
                localDisposables.Add(procWriter);
                localDisposables.Remove(txPipe);
                // Wait for host to start up.
                using (var cts = new CancellationTokenSource(5000))
                {
                    Message startedMessage = null;
                    try
                    {
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
                var clientHandler = new StreamRpcClientHandler();
                RpcClient = new JsonRpcClient(clientHandler);
                var hostBuilder = new JsonRpcServiceHostBuilder();
                ConfigServiceHost(hostBuilder);
                var host = hostBuilder.Build();
                var serverHandler = new StreamRpcServerHandler(host);
                var feature = new ExecutionHostFeature(this);
                serverHandler.DefaultFeatures.Set<IExecutionHostFeature>(feature);
                localDisposables.Add(serverHandler.Attach(procReader, procWriter));
                localDisposables.Add(clientHandler.Attach(procReader, procWriter));

                HostingServerStub = proxyBuilder.CreateProxy<IHostingServerStub>(RpcClient);
                return process;
            }
            catch (Exception ex)
            {
                if (process != null)
                {
                    try
                    {
                        if (!process.WaitForExit(1000)) process.Kill();
                    }
                    catch (Exception ex1)
                    {
                        throw new AggregateException(ex, ex1);
                    }
                }
                foreach (var d in localDisposables)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // StreamWriter will attempt to flush before disposal,
                    }
                    catch (Exception ex1)
                    {
                        throw new AggregateException(ex, ex1);
                    }
                }
                localDisposables = null;
                if (ex is ExecutionHostException) throw;
                throw new ExecutionHostException(Prompts.CannotStartExecutionHost, ex);
            }
            finally
            {
                if (localDisposables != null)
                {
                    lock (disposables)
                        disposables.AddRange(localDisposables);
                }
            }
        }

        public override async Task<Sandbox> CreateSandboxAsync(string name)
        {
            await EnsureProcessStartedAsync();
            var id = await HostingServerStub.CreateSandbox(name);
            return new JsonRpcSandbox(this, id, name);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            var process = HostingServerProcess;
            if (process != null)
            {
                if (!process.HasExited)
                {
                    // Signal for shutdown.
                    HostingServerStub.Shutdown();
                    // Allow for 5 sec.
                    if (!process.WaitForExit(5000)) process.Kill();
                }
                process.Dispose();
                process = null;
            }
            if (disposables != null)
            {
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
                disposables = null;
            }
        }
    }
}
