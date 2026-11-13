using System;
using System.IO;
using System.Text;
using UnityEngine;


/// <summary>
///     Writes 16-bit PCM .wav data.
///     From Simblend: https://github.com/SimblendGames/Simblend-Editor
/// </summary>
public static class WavUtility
{
    /// <summary>
    ///     Size of the RIFF/WAVE header written in front of the sample data, in bytes.
    /// </summary>
    public const int HeaderSize = 44;

    private const int BytesPerSample = sizeof(short);


    /// <summary>
    ///     Encodes all sample data of the clip as a 16-bit PCM .wav file.
    ///     The clip must not be a streaming clip, because GetData does not work on those.
    /// </summary>
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);

        return FromSamples(data, clip.channels, clip.frequency);
    }


    /// <summary>
    ///     Encodes interleaved float samples (-1 to 1) as a 16-bit PCM .wav file. Values outside -1 to 1 are clipped.
    /// </summary>
    public static byte[] FromSamples(float[] samples, int channels, int frequency)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be positive.");
        }

        if (frequency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequency), "Frequency must be positive.");
        }

        var dataLength = samples.Length * BytesPerSample;
        var bytes = new byte[HeaderSize + dataLength];

        using (var stream = new MemoryStream(bytes))
        using (var writer = new BinaryWriter(stream))
        {
            WriteHeader(writer, channels, frequency, dataLength);

            foreach (var sample in samples)
            {
                writer.Write(ToInt16(sample));
            }
        }

        return bytes;
    }


    private static void WriteHeader(BinaryWriter writer, int channels, int frequency, int dataLength)
    {
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(HeaderSize - 8 + dataLength); // RIFF chunk size: everything after this field
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // fmt chunk size for PCM
        writer.Write((short) 1); // Audio format: PCM
        writer.Write((short) channels);
        writer.Write(frequency);
        writer.Write(frequency * channels * BytesPerSample); // Byte rate
        writer.Write((short) (channels * BytesPerSample)); // Block align
        writer.Write((short) (BytesPerSample * 8)); // Bits per sample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
    }


    private static short ToInt16(float sample)
    {
        const int rescaleFactor = 32767; // to convert float to Int16

        if (float.IsNaN(sample))
        {
            return 0;
        }

        var clipped = sample < -1f ? -1f : sample > 1f ? 1f : sample;

        return (short) Math.Round(clipped * rescaleFactor);
    }
}
