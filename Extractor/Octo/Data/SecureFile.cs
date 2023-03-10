using Extractor.Octo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Octo.Data
{
    public class SecureFile
    {
        protected AesCrypt _crypt;

        public SecureFile(string path, AesCrypt crypt)
        {
            Path = path;
            _crypt = crypt;
        }

        public string Path { get; protected set; }

        public MemoryStream CreateMode1ReadStream(MemoryStream cipherStream)
        {
            byte[] decryptedBytes = _crypt.Decrypt(cipherStream);
            //File.WriteAllBytes("decrypted", decryptedBytes);
            MemoryStream memoryStream = new(decryptedBytes, false);

            byte[] buffer = new byte[16];
            memoryStream.Read(buffer, 0, buffer.Length);
            //File.WriteAllBytes("embedded_key", buffer);

            byte[] hashBytes = Md5.HashBytes(memoryStream);
            //File.WriteAllBytes("hashed_key", hashBytes);

            //if (!Bytes.ByteArraysEqual(buffer, hashBytes))
            //    throw new Exception("Secure file has been tampered with.");

            memoryStream.Seek(buffer.Length, SeekOrigin.Begin);
            return memoryStream;
        }

        public BinaryReader GetReader()
        {
            byte[] buffer = File.ReadAllBytes(Path);

            using MemoryStream memoryStream = new(buffer, false);
            int readByte = memoryStream.ReadByte();
            if (readByte == 0)
            {
                return new(memoryStream);
            }
            else
            {
                if (readByte == 1)
                {
                    //File.WriteAllBytes("unencrypted", buffer);
                    return new(CreateMode1ReadStream(memoryStream));
                }
                else
                {
                    memoryStream.Dispose();
                    throw new Exception("Unknown cipher mode: " + readByte);
                }
            }
        }

        public byte[] Load()
        {
            using BinaryReader reader = GetReader();
            using Stream baseStream = reader.BaseStream;

            return reader.ReadBytes((int)baseStream.Length);
        }
    }
}
