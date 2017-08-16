using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SandyBox.HostingService.Interop
{
    public abstract class ExecutionHost : IDisposable
    {

        public abstract Task<Sandbox> CreateSandboxAsync(string name);
        
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

    }

}
