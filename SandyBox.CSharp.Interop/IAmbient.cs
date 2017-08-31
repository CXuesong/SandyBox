using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.Interop
{

    public interface IAmbient
    {

        /// <summary>
        /// Gets the name of ambient container.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Invokes ambient-specific functions.
        /// </summary>
        Task<JToken> InvokeAsync(string name, JToken parameters);

    }

    public static class AmbientExtensions
    {

        public static JToken Invoke(this IAmbient ambient, string name, JToken parameters)
        {
            if (ambient == null) throw new ArgumentNullException(nameof(ambient));
            return ambient.InvokeAsync(name, parameters).GetAwaiter().GetResult();
        }

    }

}