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
        Task<JTokenContainer> InvokeAsync(string name, JTokenContainer parameters);

    }

    public static class AmbientExtensions
    {

        public static Task<JToken> InvokeAsync(this IAmbient ambient, string name, JToken parameters)
        {
            return InvokeAsync(ambient, name, parameters, CancellationToken.None);
        }

        public static async Task<JToken> InvokeAsync(this IAmbient ambient, string name, JToken parameters, CancellationToken cancellationToken)
        {
            if (ambient == null) throw new ArgumentNullException(nameof(ambient));
            return (JToken) await ambient.InvokeAsync(name, new JTokenContainer(parameters));
        }

    }

}