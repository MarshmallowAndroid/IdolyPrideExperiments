using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator.Octo.Proto
{
    [ProtoContract]
    public class Database : Extensible
    {
        [ProtoMember(1)]
        public int Revision { get; set; }

        [ProtoMember(2)]
        public List<Data> AssetBundleList { get; } = new();

        [ProtoMember(3)]
        public List<string> TagName { get; } = new();

        [ProtoMember(4)]
        public List<Data> ResourceList { get; } = new();

        [ProtoMember(5)]
        public string UrlFormat { get; set; } = "";

    }
}
