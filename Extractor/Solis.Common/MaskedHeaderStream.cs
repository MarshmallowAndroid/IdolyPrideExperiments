using System.Runtime.InteropServices;

namespace Extractor.Solis.Common
{
    internal class MaskedHeaderStream : Stream
    {
        private static byte[]? _sharedByteBuffer;

        private readonly long _headerLength;
        private readonly byte[]? _maskBytes;
        private readonly long _maskLength;
        private readonly string? _maskString;
        private readonly Stream _baseStream;

        public MaskedHeaderStream(Stream baseStream, byte[] maskBytes)
            : this(baseStream, long.MaxValue, maskBytes, maskBytes.Length)
        {
        }

        public MaskedHeaderStream(Stream baseStream, long headerLength, byte[] maskBytes, long maskLength)
        {
            _baseStream = baseStream;
            _headerLength = headerLength;
            _maskBytes = maskBytes;
            _maskLength = maskLength;
        }

        public MaskedHeaderStream(Stream baseStream, long headerLength, string maskString)
        {
            _maskString = maskString;
            _baseStream = baseStream;
            _headerLength = headerLength;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            long streamPos = _baseStream.Position;

            int read = _baseStream.Read(buffer, offset, count);
            if (_maskString != null)
                CryptByString(buffer, offset, count, streamPos, _headerLength, _maskString);
            else
                CryptByByteArray(buffer, offset, count, streamPos, _headerLength, _maskBytes, _maskLength);

            return read;

        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            long streamPos = _baseStream.Position;

            if (_maskString != null)
                CryptByString(buffer, offset, count, streamPos, _headerLength, _maskString);
            else
                CryptByByteArray(buffer, offset, count, streamPos, _headerLength, _maskBytes, _maskLength);

            _baseStream.Write(buffer, offset, count);
        }

        internal static byte[] StringToMaskBytes(string maskString)
        {
            int bytesLength = maskString.Length << 1;

            byte[] destination = new byte[bytesLength];
            unsafe
            {
                fixed (byte* maskBytes = new byte[bytesLength])
                {
                    StringToMaskBytes(maskString, maskBytes, bytesLength);
                    Marshal.Copy(*maskBytes, destination, 0, bytesLength);
                }
            }

            return destination;
        }

        private unsafe static void StringToMaskBytes(string maskString, byte* maskBytes, int bytesLength)
        {
            uint stringLength = (uint)maskString.Length;
            if (stringLength > 0)
            {
                ulong stringIndex = 0;
                int bytesIndex = 0;
                int bytesIndexReverse = bytesLength - 1;
                do
                {
                    ushort c = maskString[(int)stringIndex];
                    stringIndex += 1;
                    maskBytes[bytesIndex] = (byte)c;
                    bytesIndex += 2;
                    maskBytes[bytesIndexReverse] = (byte)~(byte)c;
                    bytesIndexReverse -= 2;
                } while (stringLength != stringIndex);
            }
            if (bytesLength > 0)
            {
                ulong counter1 = (ulong)bytesLength;
                ulong counter2 = counter1;
                stringLength = 0x9b;
                byte* maskBytesPointer = maskBytes;
                do
                {
                    counter2 -= 1;
                    stringLength = (uint)(((int)(stringLength & 1) << 7 | (int)stringLength >> 1) ^ (uint)*maskBytesPointer);
                    maskBytesPointer += 1;
                } while (counter2 != 0);

                if (bytesLength > 0)
                {
                    do
                    {
                        counter1 -= 1;
                        *maskBytes = (byte)(*maskBytes ^ (byte)stringLength);
                        maskBytes += 1;
                    } while (counter1 != 0);
                }
            }
        }

        internal static void CryptByByteArray(byte[] buffer, int offset, int count, long streamPos, long headerLength, byte[]? maskBytes, long maskLength)
        {
            if (maskBytes == null) return;

            if (maskLength != 0 && streamPos < headerLength)
            {
                int i = 0;
                do
                {
                    if (i >= count) return;
                    int a = offset + i;
                    if (a >= buffer.Length) return;
                    int b = 0;
                    if (maskLength != 0)
                        b = (int)((streamPos + i) / maskLength);
                    b = (int)(streamPos + i - b * maskLength);

                    buffer[i + offset] = (byte)(maskBytes[b] ^ buffer[i + offset]);
                    i++;
                } while (streamPos + i < headerLength);
            }
        }

        internal static void CryptByString(byte[] buffer, int offset, int count, long streamPos, long headerLength, string maskString)
        {
            if (streamPos < headerLength)
            {
                if (offset < buffer.Length)
                {
                    int bytesLength = maskString.Length << 1;

                    unsafe
                    {
                        fixed (byte* maskBytes = new byte[bytesLength])
                        {
                            StringToMaskBytes(maskString, maskBytes, bytesLength);

                            long i = 0;
                            do
                            {
                                if (i >= count) return;
                                long a = offset + i;
                                if (a >= buffer.Length) return;
                                long b = 0;
                                if (bytesLength != 0)
                                    b = (streamPos + i) / bytesLength;

                                buffer[i + offset] = (byte)(maskBytes[streamPos + i - b * bytesLength] ^ buffer[i + offset]);
                                i++;
                            } while (streamPos + i < headerLength);
                        }
                    }
                }
            }
        }
    }
}
