using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.HostingServer.Ambient;
using SandyBox.CSharp.HostingServer.JsonRpc;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer
{
    internal sealed class Sandbox : IDisposable
    {
        private AppDomain _AppDomain;
        private readonly Sponsor loaderSponsor;
        private readonly ModuleCompiler compiler = new ModuleCompiler();
        private int assemblyCounter = 0;

        public Sandbox(string name, string workPath, IEnumerable<string> accessiblePaths, SandboxAmbient ambient)
        {
            if (string.IsNullOrEmpty(workPath))
                throw new ArgumentException("Value cannot be null or empty.", nameof(workPath));
            WorkPath = Path.GetFullPath(workPath);
            Name = name;
            var permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new SecurityPermission(
                SecurityPermissionFlag.Execution |
                SecurityPermissionFlag.RemotingConfiguration
            ));
            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
            };
            var trustedAssemblies = new List<Assembly>
            {
                typeof(SandboxLoader).Assembly,
                typeof(IModule).Assembly,
            };
            permissions.AddPermission(new FileIOPermission(
                FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read,
                trustedAssemblies.Select(a => a.Location).ToArray()));
            if (accessiblePaths != null)
            {
                permissions.AddPermission(new FileIOPermission(
                    FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read,
                    accessiblePaths.ToArray()));
            }
            permissions.AddPermission(
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read,
                    WorkPath));
            // Set up AppDomain
            _AppDomain = AppDomain.CreateDomain("Sandbox: " + name, null, setup, permissions,
                trustedAssemblies.Select(a => a.Evidence.GetHostEvidence<StrongName>()).ToArray());
            Ambient = ambient;
            // Create loader proxy
            // We will pass the proxy of SandboxAmbient into loader.
            Loader = (SandboxLoader) Activator.CreateInstanceFrom(_AppDomain,
                typeof(SandboxLoader).Assembly.Location,
                typeof(SandboxLoader).FullName, false,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] {Ambient}, null, null).Unwrap();
            var lifeTime = (ILease)Loader.InitializeLifetimeService();
            loaderSponsor = new Sponsor();
            lifeTime.Register(loaderSponsor);
        }

        public string Name { get; }

        public string WorkPath { get; }

        public async Task CompileAndLoadAsync(string moduleContent)
        {
            var assemblyName = "Module" + Interlocked.Increment(ref assemblyCounter) + ".dll";
            var outputPath = Path.Combine(WorkPath, assemblyName);
            await compiler.CompileAssemblyAsync(moduleContent, assemblyName, outputPath);
            Loader.LoadModule(outputPath);
        }

        /// <summary>
        /// Gets a proxy of the loader in the sandbox appdomain.
        /// </summary>
        public SandboxLoader Loader { get; }

        public SandboxAmbient Ambient { get; }

        public void Dispose()
        {
            if (_AppDomain != null)
            {
                Loader.Dispose();
                loaderSponsor.Release();
                AppDomain.Unload(_AppDomain);
                _AppDomain = null;
            }
        }

        private class Sponsor : MarshalByRefObject, ISponsor
        {
            private bool released;

            public void Release()
            {
                released = true;
            }

            public TimeSpan Renewal(ILease lease)
            {
                if (lease == null || lease.CurrentState != LeaseState.Renewing || released)
                    return TimeSpan.Zero;
                return TimeSpan.FromSeconds(60);
            }
        }

    }
}
