using NAudio.Wave;
using SoundTouch;
using System;

namespace Vocality.Audio;
public class SoundTouchPitchProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly SoundTouch.SoundTouchProcessor soundTouch;
    private readonly WaveFormat waveFormat;
    private readonly float[] inputBuffer;
    private readonly float[] outputBuffer;

    private int outputBufferOffset = 0;
    private int outputBufferCount = 0;

    public float PitchSemitones { get; set; } = 0f;

    public SoundTouchPitchProvider(ISampleProvider sourceProvider)
    {
        source = sourceProvider;
        waveFormat = sourceProvider.WaveFormat;
        inputBuffer = new float[waveFormat.SampleRate];  // 1 second buffer  
        outputBuffer = new float[waveFormat.SampleRate];

        soundTouch = new SoundTouch.SoundTouchProcessor();
        soundTouch.SampleRate = waveFormat.SampleRate;
        soundTouch.Channels = waveFormat.Channels;
        soundTouch.PitchSemiTones = PitchSemitones;
        soundTouch.SetSetting(SettingId.SequenceDurationMs, 20);
        soundTouch.SetSetting(SettingId.SeekWindowDurationMs, 10);
        soundTouch.SetSetting(SettingId.OverlapDurationMs, 8);
        soundTouch.SetSetting(SettingId.UseAntiAliasFilter, 1);
    }

    public WaveFormat WaveFormat => waveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        if (outputBufferOffset >= outputBufferCount)
        {
            int samplesRead = source.Read(inputBuffer, 0, count);
            if (samplesRead == 0)
                return 0;

            soundTouch.PitchSemiTones = PitchSemitones;
            soundTouch.PutSamples(inputBuffer.AsSpan(0, samplesRead), samplesRead / waveFormat.Channels);

            outputBufferCount = soundTouch.ReceiveSamples(outputBuffer.AsSpan(), outputBuffer.Length / waveFormat.Channels) * waveFormat.Channels;
            outputBufferOffset = 0;
        }

        int samplesToCopy = Math.Min(count, outputBufferCount - outputBufferOffset);
        for (int i = 0; i < samplesToCopy; i++)
        {
            buffer[offset + i] = outputBuffer[outputBufferOffset + i];
        }

        outputBufferOffset += samplesToCopy;
        return samplesToCopy;
    }
}
