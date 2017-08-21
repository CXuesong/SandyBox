using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.JsonRpc
{
    public interface IHostingServerStub
    {

        [JsonRpcMethod]
        Task<int> CreateSandbox(string sandboxName);

        [JsonRpcMethod]
        Task LoadSource(int sandbox, string content);

        [JsonRpcMethod]
        Task<JToken> InvokeFunction(int sandbox, string name, JArray positionalParameters, JObject namedParameters);

        [JsonRpcMethod(IsNotification = true)]
        void DisposeSandbox(int sandbox);

        [JsonRpcMethod(IsNotification = true)]
        void Shutdown();

    }
}
