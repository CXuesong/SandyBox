using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandyBox.CSharp.HostingServer.Host
{
    internal sealed class HostCallbackHandler : MarshalByRefObject
    {

        private readonly SandboxHost host;

        public HostCallbackHandler(SandboxHost host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            this.host = host;
        }

        public void NotifySandboxDisposed(int sandboxId)
        {
            host.TerminateSandbox(sandboxId);
        }

    }
}
