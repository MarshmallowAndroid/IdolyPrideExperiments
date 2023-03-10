using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator
{
    public class LoopWaveStream : WaveStream
    {
        private readonly WaveStream sourceStream;

        private readonly int channels;
        private readonly int sampleRate;
        private readonly int bytesPerSample;

        private readonly long startPosition;
        private readonly long endPosition;

        public LoopWaveStream(WaveStream waveStream, float start, float end)
        {
            sourceStream = waveStream;

            channels = waveStream.WaveFormat.Channels;
            sampleRate = waveStream.WaveFormat.SampleRate;
            bytesPerSample = waveStream.WaveFormat.BitsPerSample / 8;

            // Convert time to bytes
            //
            // 1s = (SampleRate * BytesPerSample * Channels) bytes
            startPosition = (long)(start * sampleRate * bytesPerSample * channels);
            endPosition = (long)(end * sampleRate * bytesPerSample * channels);

            if (startPosition < 0) startPosition = 0;
            if (endPosition <= 0) endPosition = Length;
        }

        public bool Loop { get; set; } = false;

        public float LoopStartTime => (float)startPosition / sampleRate / bytesPerSample;

        public float LoopEndTime => (float)endPosition / sampleRate / bytesPerSample;

        public override WaveFormat WaveFormat => sourceStream.WaveFormat;

        public override long Length => sourceStream.Length;

        public override long Position { get => sourceStream.Position; set => sourceStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            int advanced = (int)(sourceStream.Position + count); // Get read-ahead position

            // Keep on reading until the appropriate byte count has been read
            while (totalBytesRead < count)
            {
                // Check if next position passes over the end position
                if (advanced > endPosition && Loop)
                {
                    // Edge case
                    if (endPosition > startPosition)
                    {
                        // Get remaining bytes between the current position and the loop end
                        int byteDifference = (int)(endPosition - sourceStream.Position);

                        // Read the remaining bytes into the buffer
                        if (byteDifference > 0) totalBytesRead += sourceStream.Read(buffer, offset, byteDifference);

                        // Set position back to the beginning
                        sourceStream.Position = startPosition;

                        LoopTriggered?.Invoke();
                    }
                }

                // Read and account for read bytes that is less than expected
                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                // No bytes read means we reached end of stream
                if (bytesRead == 0)
                {
                    // Reset stream to start position
                    if (Loop) { sourceStream.Position = startPosition; }
                    else break;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public delegate void OnLoopTriggered();

        public event OnLoopTriggered? LoopTriggered;
    }
}
