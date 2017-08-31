using System;
using System.Collections.Generic;
using System.Text;
using JsonRpc.Standard.Server;
using SandyBox.HostingService.Interop;

namespace SandyBox.HostingService.JsonRpc
{

    public interface ISandboxContextFeature
    {

        JsonRpcExecutionHost ExecutionHost { get; }

        JsonRpcSandbox Sandbox { get; }
        
    }

    internal class SandboxContextFeature : ISandboxContextFeature
    {
        public SandboxContextFeature(JsonRpcExecutionHost executionHost, JsonRpcSandbox sandbox)
        {
            ExecutionHost = executionHost ?? throw new ArgumentNullException(nameof(executionHost));
            Sandbox = sandbox;
        }

        public JsonRpcExecutionHost ExecutionHost { get; }

        public JsonRpcSandbox Sandbox { get; }
    }

    public static class RequestContextExtensions
    {

        public static JsonRpcExecutionHost GetExecutionHost(this RequestContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.Features.Get<ISandboxContextFeature>()?.ExecutionHost;
        }

    }

}
