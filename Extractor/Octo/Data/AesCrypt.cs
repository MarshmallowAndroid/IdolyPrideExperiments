using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Extractor.Octo.Util;

namespace Extractor.Octo.Data
{
    public class AesCrypt
    {
        private static readonly List<int> validKeyLengths = new() { 128, 192, 256 };
        private static readonly string IV = "LvAUtf+tnz";

        private RijndaelManaged aesAlgo;

        public AesCrypt(byte[] key, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            int keyLength = key.Length << 3;

            if (!validKeyLengths.Contains(keyLength))
                throw new ArgumentException("Key must be 128/192/256 bytes long.");

            aesAlgo = new()
            {
                Mode = mode,
                KeySize = keyLength,
                BlockSize = 128,
                Padding = padding,
                Key = (byte[])key.Clone(),
                IV = Md5.HashBytes(IV)
            };
        }

        public AesCrypt(string key, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
            : this(Md5.HashBytes(key), mode, padding)
        {
        }

        public byte[] Decrypt(MemoryStream memoryStream)
        {
            long length = memoryStream.Length;
            ICryptoTransform transform = aesAlgo.CreateDecryptor();

            using CryptoStream cryptoStream = new(memoryStream, transform, CryptoStreamMode.Read);
            using BinaryReader binaryReader = new(cryptoStream);

            byte[] buffer = new byte[length];
            int newSize = binaryReader.Read(buffer, 0, buffer.Length);
            //Array.Resize(ref buffer, newSize);

            return buffer;
        }
    }
}
