using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator
{
    internal class AudioEngine
    {
        private readonly WaveOutEvent waveOutEvent = new();
        private readonly MixingSampleProvider mix;

        public AudioEngine()
        {
            mix = new(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2))
            {
                ReadFully = true
            };
            waveOutEvent.Init(mix);
            waveOutEvent.Play();
        }

        public IEnumerable<ISampleProvider> MixerInputs => mix.MixerInputs;

        public void Play(ISampleProvider? input)
        {
            if (input == null) return;
            mix.AddMixerInput(input);
        }

        public void Reset() => mix.RemoveAllMixerInputs();

        public static AudioEngine Instance { get; } = new();
    }
}
