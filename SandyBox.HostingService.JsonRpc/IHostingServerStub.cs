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

        [JsonRpcMethod]
        Task<int> CreateSandbox(string name, string pipe);

        /// <summary>
        /// Compiles and loads the source code into the specified sandbox.
        /// </summary>
        [JsonRpcMethod]
        Task LoadSource(int sandbox, string content, string fileName);

    }

    [JsonRpcScope(MethodPrefix = "Sandbox.")]
    public interface ISandboxStub
    {

        [JsonRpcMethod]
        Task<JToken> Invoke(string name, JArray positionalParameters, JObject namedParameters);

        [JsonRpcMethod(IsNotification = true)]
        void Dispose();

    }
}
