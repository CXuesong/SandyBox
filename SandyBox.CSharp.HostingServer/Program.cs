using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using Microsoft.Extensions.Logging;
using SandyBox.CSharp.HostingServer.CommandLine;
using SandyBox.CSharp.HostingServer.Host;

namespace SandyBox.CSharp.HostingServer
{
    internal static class Program
    {

        public static readonly ProgramCommandLineArguments CommandLineArguments = new ProgramCommandLineArguments();

        static void Main(string[] args)
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-us");
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            CommandLineArguments.ParseFrom(args);
            if (CommandLineArguments.WaitForDebugger)
            {
                while (!Debugger.IsAttached) Thread.Sleep(1000);
                Debugger.Break();
            }
            var host = BuildServiceHost();
            var serverHandler = new StreamRpcServerHandler(host);
            var clientHandler = new StreamRpcClientHandler();
            var client = new JsonRpcClient(clientHandler);
            var serviceContext = new SandboxHost(client, CommandLineArguments.SandboxPath);
            serverHandler.DefaultFeatures.Set(serviceContext);
            // Messages come from Console
            ByLineTextMessageReader reader;
            ByLineTextMessageWriter writer;
            if (!string.IsNullOrEmpty(CommandLineArguments.RxHandle))
            {
                var pipeClient = new AnonymousPipeClientStream(PipeDirection.In, CommandLineArguments.RxHandle);
                reader = new ByLineTextMessageReader(pipeClient);
            }
            else
            {
                reader = new ByLineTextMessageReader(Console.In);
            }
            if (!string.IsNullOrEmpty(CommandLineArguments.TxHandle))
            {
                var pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, CommandLineArguments.TxHandle);
                writer = new ByLineTextMessageWriter(pipeClient);
            }
            else
            {
                writer = new ByLineTextMessageWriter(Console.Out);
            }
            try
            {
                using (reader)
                using (writer)
                using (serverHandler.Attach(reader, writer))
                using (clientHandler.Attach(reader, writer))
                {
                    // Started up.
                    serviceContext.HostingClient.NotifyStarted();
                    // Wait for exit
                    serviceContext.Disposal.Wait();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static IJsonRpcServiceHost BuildServiceHost()
        {
            var builder = new JsonRpcServiceHostBuilder();
            builder.Register(typeof(Program).Assembly);
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddDebug(LogLevel.Trace);

            builder.LoggerFactory = loggerFactory;
            return builder.Build();
        }
    }

    class ProgramCommandLineArguments
    {

        public string SandboxPath { get; set; } = "SandboxTemp";

        public string TxHandle { get; set; }

        public string RxHandle { get; set; }

        public bool WaitForDebugger { get; set; }

        public void ParseFrom(IEnumerable<string> arguments)
        {
            foreach (var sarg in arguments)
            {
                var arg = CommandLineParser.ParseArgument(sarg);
                switch (arg.Name?.ToUpperInvariant())
                {
                    case "SANDBOXPATH":
                        SandboxPath = arg.Value;
                        break;
                    case "TXPIPE":
                        TxHandle = arg.Value;
                        break;
                    case "RXPIPE":
                        RxHandle = arg.Value;
                        break;
                    case "WAITFORDEBUGGER":
                        WaitForDebugger = (bool)arg;
                        break;
                }
            }
        }
    }
}