using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Octo.Proto
{
    [ProtoContract]
    public class Data : Extensible
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string FilePath { get; set; } = "";

        [ProtoMember(3)]
        public string Name { get; set; } = "";

        [ProtoMember(4)]
        public int Size { get; set; }

        [ProtoMember(5)]
        public uint Crc { get; set; }

        [ProtoMember(10)]
        public string Md5 { get; set; } = "";

        [ProtoMember(6)]
        public int Priority { get; set; } = 0;

        [ProtoMember(7)]
        public List<int> TagId { get; } = new();

        [ProtoMember(8)]
        public List<int> Dependencies { get; } = new();

        [ProtoMember(11)]
        public string ObjectName { get; set; } = "";

        [ProtoMember(12)]
        public ulong Generation { get; set; } = 0;

        [ProtoMember(13)]
        public int UploadVersionId { get; set; } = 0;

        [ProtoMember(9)]
        public State DataState { get; set; }

        [ProtoContract]
        public enum State
        {
            [ProtoEnum]
            NONE,
            [ProtoEnum]
            ADD,
            [ProtoEnum]
            UPDATE,
            [ProtoEnum]
            LATEST,
            [ProtoEnum]
            DELETE
        }
    }
}
