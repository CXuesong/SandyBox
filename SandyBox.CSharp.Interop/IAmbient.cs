using System;

namespace SandyBox.CSharp.Interop
{
    public interface IAmbient : IServiceProvider
    {
        
        /// <summary>
        /// Gets the name of ambient container.
        /// </summary>
        string Name { get; }

    }

    public static class AmbientExtensions
    {

        public static T GetService<T>(this IAmbient ambient)
        {
            if (ambient == null) throw new ArgumentNullException(nameof(ambient));
            var inst = ambient.GetService(typeof(T));
            if (inst == null) return default(T);
            return (T) inst;
        }

    }
}