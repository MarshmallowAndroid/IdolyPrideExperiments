using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator.Octo.Util
{
    internal static class Bytes
    {
        public static byte[] StringToByteArray(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) return false;

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }

            return true;
        }
    }
}
