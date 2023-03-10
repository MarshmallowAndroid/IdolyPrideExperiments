using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator
{
    internal class SoundEffect : ISampleProvider
    {
        private readonly WaveFormat waveFormat;
        private readonly float[] sampleBuffer;

        private long totalSamplesRead;

        public SoundEffect(WaveStream waveStream)
        {
            waveStream.Position = 0;
            waveFormat = waveStream.WaveFormat;
            sampleBuffer = new float[(int)(waveStream.Length / (waveFormat.BitsPerSample / 8))];

            ISampleProvider temp = waveStream.ToSampleProvider();
            temp.Read(sampleBuffer, 0, sampleBuffer.Length);
        }

        public WaveFormat WaveFormat => waveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;

            for (int i = 0; i < count; i++)
            {
                if (i + totalSamplesRead >= sampleBuffer.Length - 1) break;
                buffer[i] = sampleBuffer[i + totalSamplesRead];
                samplesRead++;
            }

            totalSamplesRead += samplesRead;

            return samplesRead;
        }
    }
}
