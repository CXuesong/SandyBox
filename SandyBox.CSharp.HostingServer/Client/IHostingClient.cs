using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer.Client
{
    public interface IHostingClient
    {

        /// <summary>
        /// Notifies the client that the host process is started up and ready for request.
        /// </summary>
        [JsonRpcMethod(IsNotification = true)]
        void NotifyStarted();

        /// <summary>
        /// Invoke client-defined ambient method.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="sandbox">The sandbox initiated the request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Invocation result.</returns>
        Task<JToken> InvokeAmbient(string methodName, JToken parameters, int sandbox, CancellationToken cancellationToken);

    }
}
