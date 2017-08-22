using System;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    public class UnitTestBase : IDisposable
    {

        public UnitTestBase(ITestOutputHelper output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
