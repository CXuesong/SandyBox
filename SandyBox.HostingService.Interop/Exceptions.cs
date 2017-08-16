using System;
using System.Collections.Generic;
using System.Text;

namespace SandyBox.HostingService.Interop
{
    public class ExecutionHostException : Exception
    {

        public ExecutionHostException()
        {
        }

        public ExecutionHostException(string message) : base(message)
        {
        }

        public ExecutionHostException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
