using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SandyBox.HostingService.Interop;

namespace XUnitTestProject1
{
    internal static class Utility
    {

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static async Task LoadFromStringAsync(this Sandbox sb, string s)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            if (s == null) throw new ArgumentNullException(nameof(s));
            var buffer = Utf8NoBom.GetBytes(s);
            using (var ms = new MemoryStream(buffer))
            {
                await sb.LoadFromAsync(ms, null);
            }
        }

    }
}
