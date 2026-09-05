using NAudio.Wave;
using FruitsAtelier.App.Audio;

internal static class SeekAlignmentTests
{
    public static Task Run(string root)
    {
        var failures = new List<string>();
        foreach (string file in Directory.GetFiles(Path.Combine(root, "artifacts", "beatmaps"), "*.mp3", SearchOption.AllDirectories).Take(2))
        {
            using var sequential = MediaFoundationAudioReader.Open(file);
            using var memory = new MemoryStream();
            sequential.CopyTo(memory);
            byte[] baseline = memory.ToArray();
            int alignment = sequential.WaveFormat.BlockAlign;
            int bytesPerSecond = sequential.WaveFormat.AverageBytesPerSecond;
            using var seeking = MediaFoundationAudioReader.Open(file);
            foreach (double targetMs in new[] { 12345.0, 200.0, 17123.0, sequential.TotalTime.TotalMilliseconds / 2,
                1100.0, sequential.TotalTime.TotalMilliseconds - 100, sequential.TotalTime.TotalMilliseconds, 0.0 })
            {
                long desired = (long)(targetMs * bytesPerSecond / 1000);
                desired -= desired % alignment;
                seeking.Position = desired;
                if (seeking.Position != desired) throw new Exception("Paused seek did not preserve the exact PCM frame index.");
                byte[] actual = new byte[bytesPerSecond / 4 / alignment * alignment];
                int count = seeking.Read(actual, 0, actual.Length);
                bool same = count == Math.Min(actual.Length, baseline.Length - desired)
                    && baseline.AsSpan((int)desired, count).SequenceEqual(actual.AsSpan(0, count));
                if (!same)
                {
                    var lag = FindLag(baseline, actual, (int)desired, sequential.WaveFormat, 0);
                    string diagnosis = $"{Path.GetFileName(file)} seek {targetMs:0.###}ms: sample mismatch, best correlation offset {lag.OffsetMs:0.###}ms (r={lag.Score:0.000})";
                    Console.WriteLine("  " + diagnosis);
                    failures.Add(diagnosis);
                }
            }
        }
        if (failures.Count > 0) throw new Exception(string.Join(Environment.NewLine, failures));
        return Task.CompletedTask;
    }

    private static (double OffsetMs, double Score) FindLag(byte[] reference, byte[] actual, int position, WaveFormat format, int skip)
    {
        int align = format.BlockAlign;
        int range = format.SampleRate;
        int step = Math.Max(1, format.SampleRate / 1000);
        int best = 0;
        double bestScore = double.NegativeInfinity;
        for (int lag = -range; lag <= range; lag += step) Consider(lag);
        int coarse = best;
        for (int lag = coarse - step; lag <= coarse + step; lag++) Consider(lag);
        return (best * 1000.0 / format.SampleRate, bestScore);

        void Consider(int lag)
        {
            if (position + lag * align + skip < 0 || position + lag * align + actual.Length > reference.Length) return;
            double cross = 0, aEnergy = 0, bEnergy = 0;
            for (int offset = skip; offset < actual.Length; offset += align * 13)
            {
                short a = BitConverter.ToInt16(actual, offset);
                short b = BitConverter.ToInt16(reference, position + lag * align + offset);
                cross += (double)a * b; aEnergy += (double)a * a; bEnergy += (double)b * b;
            }
            double score = cross / Math.Sqrt(Math.Max(1, aEnergy * bEnergy));
            if (score > bestScore) { bestScore = score; best = lag; }
        }
    }
}
