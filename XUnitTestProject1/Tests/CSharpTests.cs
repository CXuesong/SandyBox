using System;
using System.IO;
using SandyBox.HostingService.JsonRpc;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1.Tests
{
    public class CSharpTests : UnitTestBase
    {

        private readonly JsonRpcExecutionHost executionHost =
            new JsonRpcExecutionHost("CSHost/SandyBox.CSharp.HostingServer.exe", "CSTemp");

        public CSharpTests(ITestOutputHelper output) : base(output)
        {

        }

        [Fact]
        public async Task EmptyExecution()
        {
            var ex = await Assert.ThrowsAsync<JsonRpcRemoteException>(async () =>
            {
                using (var sandbox = await executionHost.CreateSandboxAsync("Empty"))
                {
                    await sandbox.LoadFromAsync(Stream.Null, null);
                }
            });
            Assert.Equal("SandyBox.CSharp.HostingServer.MissingModuleException", ex.RemoteException.ExceptionType);
        }

        [Fact]
        public async Task BasicExecution()
        {
            using (var sandbox = await executionHost.CreateSandboxAsync("BasicExecution"))
            {
                await sandbox.LoadFromStringAsync(@"
using System;
using SandyBox.CSharp.Interop;

public class MyModule : Module
{

    public double Add(double x, double y) => x + y;

    public string Add(string x, string y) => x + y;

}
");
                Assert.Equal(await sandbox.ExecuteAsync("Add", new JArray(10.23, 20.45), null),
                    new JValue(30.68));
                Assert.Equal(await sandbox.ExecuteAsync("Add", new JArray("abcdef", "ABC"), null),
                    new JValue("abcdefABC"));
                Assert.Equal(await sandbox.ExecuteAsync("Add", new JArray("abc", null), null),
                    new JValue("abc"));
                var ex = await Assert.ThrowsAsync<JsonRpcRemoteException>(() =>
                    sandbox.ExecuteAsync("Add", new JArray(10, null), null));
                Assert.Equal(typeof(MissingMethodException).FullName, ex.RemoteException.ExceptionType);
            }
        }

        [Fact]
        public async Task InvokeAmbientExecution()
        {
            using (var sandbox = await executionHost.CreateSandboxAsync("InvokeAmbientExecution"))
            {
                sandbox.InvokeAmbientHandler = async (name, parameters, sb, token) =>
                {
                    if (name == "Concat") return (string)parameters[0] + (string)parameters[1];
                    throw new NotSupportedException();
                };
                await sandbox.LoadFromStringAsync(@"
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandyBox.CSharp.Interop;

public class MyModule : Module {

    public int Concat(int x, int y) {
        var result = Ambient.Invoke(""Concat"", new JArray(x, y));
        return int.Parse((string) result);
    }

    public void InvokeMissingMethod() {
        Ambient.Invoke(""ABC"", null);
    }

}
");
                Assert.Equal(await sandbox.ExecuteAsync("Concat", new JArray(123, 456), null),
                    new JValue(123456));
                Assert.Equal(await sandbox.ExecuteAsync("InvokeMissingMethod", null, null), new JValue(123456));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                executionHost.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
