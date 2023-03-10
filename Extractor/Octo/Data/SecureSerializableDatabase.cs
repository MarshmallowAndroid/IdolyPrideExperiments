using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Octo.Data
{
    public abstract class SecureSerializableDatabase<T> : SecureFile where T : class, new()
    {
        public T Data { get; set; }

        public SecureSerializableDatabase(string path, AesCrypt crypt) : base(path, crypt)
        {
        }

        protected abstract T Deserialize(byte[] bytes);

        public void Deserialize()
        {
            Data = Deserialize(Load());
        }
    }
}
