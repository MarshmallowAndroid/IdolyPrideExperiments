using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator.Octo.Util
{
    internal static class Md5
    {
        private static readonly MD5 md5Crypto = MD5.Create();

        public static byte[] HashBytes(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<byte>();

            return HashBytes(Bytes.StringToByteArray(text));
        }

        public static byte[] HashBytes(MemoryStream memoryStream)
        {
            return md5Crypto.ComputeHash(memoryStream);
        }

        public static byte[] HashBytes(byte[] bytes)
        {
            return md5Crypto.ComputeHash(bytes);
        }
    }
}
