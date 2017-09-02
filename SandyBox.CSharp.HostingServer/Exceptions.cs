using System;
using System.Runtime.Serialization;
using System.Security;

namespace SandyBox.CSharp.HostingServer
{
    [Serializable]
    public class ModuleLoaderException : Exception
    {
        public ModuleLoaderException() : this(null, null)
        {
        }

        public ModuleLoaderException(string message) : this(message, null)
        {
        }

        public ModuleLoaderException(Exception inner) : this(null, inner)
        {
        }

        public ModuleLoaderException(string message, Exception inner) : base(message ?? Prompts.ModuleLoaderException, inner)
        {
        }

        [SecuritySafeCritical]
        protected ModuleLoaderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    [Serializable]
    public class MissingModuleException : ModuleLoaderException
    {

        public MissingModuleException() : this(null, null)
        {
        }

        public MissingModuleException(string message) : base(message, null)
        {
        }

        public MissingModuleException(string message, Exception inner) : base(message ?? Prompts.MissingModuleException, inner)
        {
        }

        [SecuritySafeCritical]
        protected MissingModuleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class AmbiguousModuleException : ModuleLoaderException
    {

        public AmbiguousModuleException() : this(null, null)
        {
        }

        public AmbiguousModuleException(string message) : base(message, null)
        {
        }

        public AmbiguousModuleException(string message, Exception inner) : base(message ?? Prompts.AmbiguousModuleException, inner)
        {
        }

        [SecuritySafeCritical]
        protected AmbiguousModuleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

}
