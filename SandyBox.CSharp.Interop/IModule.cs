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
        public virtual void Dispose()
        {
            
        }

        public virtual void Initialize(IAmbient ambient)
        {

        }
    }

}
