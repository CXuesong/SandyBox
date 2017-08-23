using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Ambient;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer
{
    /// <summary>
    /// A loader class used in the sandbox appdomain.
    /// </summary>
    internal sealed class SandboxLoader : MarshalByRefObject, IDisposable
    {
        private IModule _ClientModule;
        private readonly Dictionary<string, IList<MethodInfo>> nameMethodDict = new Dictionary<string, IList<MethodInfo>>();

        internal SandboxLoader()
        {

        }

        public void LoadAssembly(string assemblyPath)
        {
            Assembly.LoadFrom(assemblyPath);
        }

        public void LoadModule(string assemblyPath)
        {
            if (assemblyPath == null) throw new ArgumentNullException(nameof(assemblyPath));
            Debug.Assert(_ClientModule == null);
            var assembly = Assembly.LoadFrom(assemblyPath);
            Type moduleType;
            {
                var matches = assembly.GetExportedTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract).Take(2).ToArray();
                if (matches.Length == 0) throw new MissingModuleException();
                if (matches.Length > 1) throw new AmbiguousModuleException();
                moduleType = matches[0];
            }
            IModule localModule = null;
            try
            {
                localModule = (IModule)Activator.CreateInstance(moduleType);
                localModule.Initialize(new SandboxAmbient());
                foreach (var method in moduleType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                             BindingFlags.Public))
                {
                    if (!nameMethodDict.TryGetValue(method.Name, out var list))
                    {
                        list = new List<MethodInfo>();
                        nameMethodDict.Add(method.Name, list);
                    }
                    list.Add(method);
                }
                _ClientModule = localModule;
            }
            catch (Exception ex)
            {
                (localModule as IDisposable)?.Dispose();
                throw new ModuleLoaderException(ex);
            }
        }

        // This is remoting-friendly.
        public byte[] InvokeBson(string functionName, byte[] positionalParameters, byte[] namedParameters)
        {
            var result = Invoke(functionName, Utility.BsonDeserialize<IList<JToken>>(positionalParameters),
                Utility.BsonDeserialize<IDictionary<string, JToken>>(namedParameters));
            return Utility.BsonSerialize(result);
        }

        // JToken is not serializable
        public JToken Invoke(string functionName, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(functionName));
            if (_ClientModule == null) throw new InvalidOperationException();
            IList<MethodInfo> candidates;
            try
            {
                candidates = nameMethodDict[functionName];
            }
            catch (KeyNotFoundException)
            {
                throw new MissingMethodException(_ClientModule.GetType().Name, functionName);
            }
            var method = ModuleFunctionBinder.BindMethod(candidates, _ClientModule.GetType(), functionName,
                positionalParameters, namedParameters);
            var parameters = ModuleFunctionBinder.BindParameters(method, positionalParameters, namedParameters);
            var result = method.Invoke(_ClientModule, parameters);
            if (method.ReturnParameter.ParameterType == typeof(void)) return null;
            return ModuleFunctionBinder.SerializeReturnValue(result);
        }

        public IModule ClientModule => _ClientModule;

        public void Dispose()
        {
            if (_ClientModule != null)
            {
                if (_ClientModule is IDisposable d) d.Dispose();
                _ClientModule = null;
            }
        }
    }

}
