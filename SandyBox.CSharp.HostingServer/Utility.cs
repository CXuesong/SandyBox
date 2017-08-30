using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer
{
    internal static class Utility
    {

        private static readonly JsonSerializer jsonSerializer = new JsonSerializer();

        private static readonly byte[] emptyBytes = { };

        public static byte[] BsonSerialize(object obj)
        {
            if (obj == null) return emptyBytes;
            using (var ms = new MemoryStream())
            using (var writer = new BsonDataWriter(ms))
            {
                // The root of BSON must be an object or array, so we use an array to wrap actual value/object.
                jsonSerializer.Serialize(writer, new[] { obj });
                ms.Flush();
                return ms.GetBuffer();
            }
        }

        public static T BsonDeserialize<T>(byte[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.Length == 0) return default(T);
            using (var ms = new MemoryStream(content, false))
            using (var reader = new BsonDataReader(ms))
            {
                reader.ReadRootValueAsArray = true;
                var wrapper = jsonSerializer.Deserialize<T[]>(reader);
                return wrapper.Single();
            }
        }
    }
}
