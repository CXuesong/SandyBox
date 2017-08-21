using System.IO;
using SandyBox.HostingService.JsonRpc;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using SandyBox.CSharp.Interop;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1.Tests
{
    public class CSharpTests : UnitTestBase
    {

        public CSharpTests(ITestOutputHelper output) : base(output)
        {

        }

        private JsonRpcExecutionHost CreateExecutionHost()
        {
            var host = new JsonRpcExecutionHost("CSHost/SandyBox.CSharp.HostingServer.exe", "CSTemp");
            return host;
        }

        [Fact]
        public async Task EmptyExecution()
        {
            var ex = await Assert.ThrowsAsync<JsonRpcRemoteException>(async () =>
            {
                using (var host = CreateExecutionHost())
                {
                    using (var sandbox = await host.CreateSandboxAsync("Empty"))
                    {
                        await sandbox.LoadFromAsync(Stream.Null);
                    }
                }
            });
            Assert.Equal(ex.RemoteException.ExceptionType, typeof(MissingModuleException).FullName);
        }
    }
}
