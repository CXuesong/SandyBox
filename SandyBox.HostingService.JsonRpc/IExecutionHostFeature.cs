using System;
using System.Collections.Generic;
using System.Text;
using JsonRpc.Standard.Server;
using SandyBox.HostingService.Interop;

namespace SandyBox.HostingService.JsonRpc
{
    public interface IExecutionHostFeature
    {

        JsonRpcExecutionHost ExecutionHost { get; }

    }

    public class ExecutionHostFeature : IExecutionHostFeature
    {
        public ExecutionHostFeature(JsonRpcExecutionHost executionHost)
        {
            ExecutionHost = executionHost ?? throw new ArgumentNullException(nameof(executionHost));
        }

        public JsonRpcExecutionHost ExecutionHost { get; }
    }

    public static class RequestContextExtensions
    {

        public static THost GetExecutionHost<THost>(this RequestContext context) where THost : JsonRpcExecutionHost
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return (THost) context.Features.Get<IExecutionHostFeature>()?.ExecutionHost;
        }

    }

}
