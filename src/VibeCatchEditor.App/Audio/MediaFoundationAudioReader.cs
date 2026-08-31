using L = VibeCatchEditor.Localization.Strings;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace VibeCatchEditor.App.Audio;

internal sealed class MediaFoundationAudioReader : WaveStream
{
    private static readonly object lifetimeLock = new();
    private static int activeReaders;
    private const long maximumDecodedBytes = 512L * 1024 * 1024;
    private readonly List<byte[]> chunks;
    private readonly WaveFormat format;
    private readonly int chunkSize;
    private readonly long length;
    private long position;
    private bool disposed;

    private MediaFoundationAudioReader(MediaFoundationReader decoder, Func<bool>? cancelled)
    {
        format = decoder.WaveFormat;
        chunkSize = 65536 / format.BlockAlign * format.BlockAlign;
        chunks = new();
        // Media Foundation seeks may return different compressed frames while reporting the requested position.
        // A continuous decode gives every later seek one stable PCM frame index, including the decoder's start padding.
        while (true)
        {
            if (cancelled?.Invoke() == true) throw new OperationCanceledException(L.Get("audio.cancelled"));
            byte[] chunk = new byte[chunkSize];
            int used = 0;
            while (used < chunk.Length)
            {
                if (cancelled?.Invoke() == true) throw new OperationCanceledException(L.Get("audio.cancelled"));
                int count = decoder.Read(chunk, used, chunk.Length - used);
                if (count == 0) break;
                used += count;
            }
            if (used == 0) break;
            if ((length += used) > maximumDecodedBytes)
                throw new InvalidDataException(L.Get("audio.cacheLimit"));
            if (used % format.BlockAlign != 0) throw new InvalidDataException(L.Get("audio.incompleteFrame"));
            chunks.Add(chunk);
            if (used < chunk.Length) break;
        }
    }

    public static MediaFoundationAudioReader Open(string path, Func<bool>? cancelled = null)
    {
        lock (lifetimeLock)
        {
            if (activeReaders == 0) MediaFoundationApi.Startup();
            activeReaders++;
        }
        try
        {
            using var decoder = new MediaFoundationReader(path, new() { SingleReaderObject = true });
            return new(decoder, cancelled);
        }
        finally { ReleaseRuntime(); }
    }

    public override WaveFormat WaveFormat => format;
    public override long Length => length;
    public override long Position
    {
        get => position;
        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            long bounded = Math.Clamp(value, 0, length);
            position = bounded - bounded % format.BlockAlign;
        }
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Span<byte> destination = buffer.AsSpan(offset, count);
        int remaining = (int)Math.Min(count, length - position);
        int total = remaining;
        while (remaining > 0)
        {
            int within = (int)(position % chunkSize);
            int copy = Math.Min(remaining, chunkSize - within);
            chunks[(int)(position / chunkSize)].AsSpan(within, copy).CopyTo(destination);
            destination = destination[copy..];
            position += copy;
            remaining -= copy;
        }
        return total;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            chunks.Clear();
        }
        base.Dispose(disposing);
    }

    private static void ReleaseRuntime()
    {
        // Every Media Foundation reader in this application shares this lease; do not shut down a live sibling reader.
        lock (lifetimeLock)
            if (--activeReaders == 0) MediaFoundationApi.Shutdown();
    }
}
