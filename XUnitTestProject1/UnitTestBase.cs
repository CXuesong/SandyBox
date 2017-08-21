using System;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    public class UnitTestBase
    {

        public UnitTestBase(ITestOutputHelper output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            Output = output;
        }

        public ITestOutputHelper Output { get; }

    }
}
