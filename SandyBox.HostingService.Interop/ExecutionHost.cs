using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.Interop
{
    public abstract class ExecutionHost : IDisposable
    {

        public abstract Task<Sandbox> CreateSandboxAsync(string name);

        public abstract Sandbox TryGetSandbox(int id);
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExecutionHost()
        {
            Dispose(false);
        }

        /// <summary>
        /// The default used to process InvokeAmbient request from the sandbox.
        /// </summary>
        public InvokeAmbientAsyncHandler InvokeAmbientHandler { get; set; }

    }

    public delegate Task<JToken> InvokeAmbientAsyncHandler(string methodName, JToken parameters,
        Sandbox sandbox, CancellationToken cancellationToken);

}
