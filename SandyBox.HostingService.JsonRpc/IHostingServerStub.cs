using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.JsonRpc
{
    [JsonRpcScope(MethodPrefix = "Host.")]
    public interface IHostStub
    {

        [JsonRpcMethod(IsNotification = true)]
        void Shutdown();

    }

    [JsonRpcScope(MethodPrefix = "Sandbox.")]
    public interface ISandboxStub
    {

        [JsonRpcMethod]
        Task<int> Create(string sandboxName);

        [JsonRpcMethod]
        Task LoadSource(int sandbox, string content);

        [JsonRpcMethod]
        Task<JToken> Invoke(int sandbox, string name, JArray positionalParameters, JObject namedParameters);

        [JsonRpcMethod(IsNotification = true)]
        void Dispose(int sandbox);

    }
}
