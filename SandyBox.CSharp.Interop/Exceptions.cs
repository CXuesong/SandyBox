using System;
using System.Runtime.Serialization;

namespace SandyBox.CSharp.Interop
{
    [Serializable]
    public class ModuleLoaderException : Exception
    {
        public ModuleLoaderException() : this(null,null)
        {
        }

        public ModuleLoaderException(string message) : this(message, null)
        {
        }

        public ModuleLoaderException(Exception inner) : base(null, inner)
        {
        }

        public ModuleLoaderException(string message, Exception inner) : base(message ?? Prompts.ModuleLoaderException, inner)
        {
        }

        protected ModuleLoaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
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

        protected MissingModuleException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
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

        protected AmbiguousModuleException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

}
