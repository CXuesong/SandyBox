using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;

namespace SandyBox.CSharp.HostingServer.Host
{
    [JsonRpcScope(MethodPrefix = "Host.")]
    public class HostRpcService : JsonRpcService
    {

        [JsonRpcMethod]
        public int CreateSandbox(string name, string pipe)
        {
            var context = RequestContext.Features.Get<SandboxHost>();
            return context.CreateSandbox(name, pipe);
        }

        [JsonRpcMethod]
        public async Task LoadSource(int sandbox, string content, string fileName)
        {
            var context = RequestContext.Features.Get<SandboxHost>();
            var sb = context.GetSandbox(sandbox);
            await sb.CompileAndLoadAsync(content).ConfigureAwait(false);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void Shutdown()
        {
            RequestContext.Features.Get<SandboxHost>().Dispose();
        }

    }
}
