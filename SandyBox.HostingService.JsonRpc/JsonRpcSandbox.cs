using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SandyBox.HostingService.Interop;

namespace SandyBox.HostingService.JsonRpc
{
    public class JsonRpcSandbox : Sandbox
    {
        public JsonRpcExecutionHost Owner { get; }

        public int Id { get; }

        protected internal JsonRpcSandbox(JsonRpcExecutionHost owner, int id, string name) : base(name)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            Id = id;
            Owner = owner;
        }

        public override async Task LoadFromAsync(Stream sourceStream)
        {
            if (sourceStream == null) throw new ArgumentNullException(nameof(sourceStream));
            using (var reader = new StreamReader(sourceStream))
            {
                var s = await reader.ReadToEndAsync();
                await Owner.SandboxStub.LoadSource(Id, s);
            }
        }

        public override async Task<JToken> ExecuteAsync(string functionName, JArray positionalParameters, JObject namedParameters)
        {
            return await Owner.SandboxStub.Invoke(Id, functionName, positionalParameters, namedParameters);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Owner.SandboxStub.Dispose(Id);
            }
        }
    }
}
