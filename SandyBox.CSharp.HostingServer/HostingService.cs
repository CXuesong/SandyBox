using System.Collections.Generic;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer
{
    public class HostingService : JsonRpcService
    {

        [JsonRpcMethod]
        public int CreateSandbox(string sandboxName)
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
        public JToken InvokeFunction(int sandbox, string name, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            var sb = RequestContext.Features.Get<HostingServiceContext>().GetSandbox(sandbox);
            return sb.Loader.Invoke(name, positionalParameters, namedParameters);
        }

        [JsonRpcMethod]
        public void DisposeSandbox(int sandbox)
        {
            RequestContext.Features.Get<HostingServiceContext>().TerminateSandbox(sandbox);
        }

        [JsonRpcMethod]
        public void Shutdown()
        {
            RequestContext.Features.Get<HostingServiceContext>().Dispose();
        }

    }
}
