using System;
using System.Security;

namespace SandyBox.CSharp.Interop
{
    public interface IModule
    {

        void Initialize(IAmbient ambient);

    }

    public class Module : IModule, IDisposable
    {

        public IAmbient Ambient { get; private set; }

        public virtual void Initialize(IAmbient ambient)
        {
            if (ambient == null) throw new ArgumentNullException(nameof(ambient));
            Ambient = ambient;
        }

        public virtual void Dispose()
        {

        }

    }

}
