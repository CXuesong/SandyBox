using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.Interop;

namespace SandyBox.CSharp.HostingServer.Host
{
    public class ModuleCompiler
    {

        private static readonly MetadataReference[] CoreReferences
            = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetType("System.Object") != null)
                .Select(a => (MetadataReference) MetadataReference.CreateFromFile(a.Location)).ToArray();

        public ModuleCompiler()
        {

        }

        public Task CompileAssemblyAsync(string moduleContent, string assemblyName, string outputPath)
        {
            if (moduleContent == null) throw new ArgumentNullException(nameof(moduleContent));
            return Task.Run(() =>
            {
                var assemblyPath = outputPath;
                var tree = CSharpSyntaxTree.ParseText(SourceText.From(moduleContent));
                // A single, immutable invocation to the compiler
                // to produce a library
                var compilation = CSharpCompilation.Create(assemblyName)
                    .WithOptions(
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(CoreReferences)
                    .AddReferences(
                        MetadataReference.CreateFromFile(typeof(XElement).GetTypeInfo().Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(JObject).GetTypeInfo().Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(IModule).GetTypeInfo().Assembly.Location))
                    .AddSyntaxTrees(tree);
                var path = Path.Combine(Directory.GetCurrentDirectory(), assemblyPath);
                var result = compilation.Emit(path);
                if (!result.Success)
                {
                    var error = result.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error) ??
                                result.Diagnostics.First();
                    throw new Exception("Compilation failure. " + error);
                }
            });
        }
    }

}
