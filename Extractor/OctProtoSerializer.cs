using Extractor.Octo.Proto;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor
{
    internal class OctProtoSerializer : TypeModel
    {
        private static readonly Type[] knownTypes = new Type[]
        {
            typeof(Data),
            typeof(Database)
        };

        private static Database Read(Database p0, ProtoReader p1)
        {
            return null;
        }

        private static Data Read(Data p0, ProtoReader p1)
        {
            int field = p1.ReadFieldHeader();

            return null;
        }

        protected internal new object Deserialize(int p0, object p1, ProtoReader p2)
        {
            switch (p0)
            {

            }

            return null;
        }
    }
}
