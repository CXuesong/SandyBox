using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer.JsonRpc
{
    [JsonRpcScope(MethodPrefix = "Sandbox.")]
    public class SandboxService : JsonRpcService
    {

        [JsonRpcMethod]
        public int Create(string sandboxName)
        {
            return RequestContext.Features.Get<HostingServiceContext>().CreateSandbox(sandboxName);
        }

        [JsonRpcMethod]
        public async Task LoadSource(int sandbox, string content)
        {
            var context = RequestContext.Features.Get<HostingServiceContext>();
            var sb = context.GetSandbox(sandbox);
            await sb.CompileAndLoadAsync(content);
        }

        [JsonRpcMethod]
        public JToken Invoke(int sandbox, string name, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            var sb = RequestContext.Features.Get<HostingServiceContext>().GetSandbox(sandbox);
            var resultBson = sb.Loader.InvokeBson(name, Utility.BsonSerialize(positionalParameters),
                Utility.BsonSerialize(namedParameters));
            return Utility.BsonDeserialize<JToken>(resultBson);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void Dispose(int sandbox)
        {
            RequestContext.Features.Get<HostingServiceContext>().TerminateSandbox(sandbox);
        }

    }
}
