using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Solis.Common
{
    public static class MaskedHeaderStreamUtility
    {
        private static readonly byte[] unitySignature = new byte[] { (byte)'U', (byte)'n', (byte)'i', (byte)'t', (byte)'y' };

        public static bool IsEncryptedStream(Stream stream)
        {
            byte[] signatureBuffer = new byte[unitySignature.Length];
            stream.Read(signatureBuffer, 0, signatureBuffer.Length);
            stream.Seek(-unitySignature.Length, SeekOrigin.Current);

            for (int i = 0; i < signatureBuffer.Length; i++)
            {
                if (signatureBuffer[i] != unitySignature[i]) return true;
            }

            return false;
        }
    }
}
