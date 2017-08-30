using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.Interop
{
    /// <summary>
    /// Makes JToken Serializable.
    /// </summary>
    [Serializable]
    public class JTokenContainer
    {

        private readonly byte[] data;

        public JTokenContainer(JToken jtoken)
        {
            if (jtoken == null) throw new ArgumentNullException(nameof(jtoken));
            using (var ms = new MemoryStream())
            {
                using (var jwriter = new BsonDataWriter(ms))
                {
                    jwriter.WriteStartArray();
                    jtoken.WriteTo(jwriter);
                    jwriter.WriteEndArray();
                }
                ms.Flush();
                data = ms.ToArray();
            }
        }

        public JToken ToJToken()
        {
            using (var ms = new MemoryStream(data))
            {
                using (var jreader = new BsonDataReader(ms))
                {
                    jreader.ReadRootValueAsArray = true;
                    jreader.Read();
                    if (jreader.TokenType != JsonToken.StartArray) throw new InvalidOperationException("Invalid BSON.");
                    return JToken.Load(jreader);
                }
            }
        }

        // We have AmbientExtensions already.
        //public static implicit operator JTokenContainer(JToken rhs)
        //{
        //    if (rhs == null) return null;
        //    return new JTokenContainer(rhs);
        //}

        public static explicit operator JToken(JTokenContainer rhs)
        {
            if (rhs == null) return null;
            return rhs.ToJToken();
        }

    }
}
