namespace WhisperTray.Core.Tests.TestInfrastructure;

public static class AudioSamples
{
    public static short[] SynthesizeSine(int sampleRate, double frequencyHz, double durationSeconds, double amplitude = 0.3)
    {
        var n = (int)(sampleRate * durationSeconds);
        var result = new short[n];
        for (var i = 0; i < n; i++)
        {
            var t = (double)i / sampleRate;
            var s = amplitude * Math.Sin(2 * Math.PI * frequencyHz * t);
            result[i] = (short)(s * short.MaxValue);
        }
        return result;
    }

    public static byte[] SamplesToPcmBytes(short[] samples)
    {
        var bytes = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
