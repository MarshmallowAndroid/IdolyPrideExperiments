using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Octo.Proto
{
    internal static class StaticSerializer
    {
        private static OctProtoSerializer _serializer = new();

        public static T Deserialize<T>(MemoryStream stream) where T : class
        {
            ProtoReader reader = ProtoReader.Create(stream, _serializer, null);
            reader.InternStrings = false;
            Serializer.Deserialize<T>(stream);
            return (T)_serializer.Deserialize(reader, null, typeof(T));
        }

        public static T Deserialize<T>(byte[] binary) where T : class
        {
            return Deserialize<T>(new MemoryStream(binary));
        }
    }
}
