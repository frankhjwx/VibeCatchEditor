using L = FruitsAtelier.Localization.Strings;
namespace FruitsAtelier.Core;

public sealed class TimelineTransform(double left, double bottom, double width, double viewStartMs, double pixelsPerMs)
{
    public double Left { get; set; } = left;
    public double Bottom { get; set; } = bottom;
    public double Width { get; set; } = width;
    public double ViewStartMs { get; set; } = viewStartMs;
    public double PixelsPerMs { get; set; } = pixelsPerMs;

    public (double X, double Y) ToScreen(MapPoint point)
    {
        CheckDimensions();
        return (Left + point.X / 512 * Width, Bottom - (point.TimeMs - ViewStartMs) * PixelsPerMs);
    }

    public MapPoint ToMap(double screenX, double screenY)
    {
        CheckDimensions();
        return new(ViewStartMs + (Bottom - screenY) / PixelsPerMs, (screenX - Left) / Width * 512);
    }

    public void ZoomAt(double screenY, double factor)
    {
        if (!double.IsFinite(screenY)) throw new ArgumentOutOfRangeException(nameof(screenY));
        if (!double.IsFinite(factor) || factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        double anchorTime = ToMap(Left, screenY).TimeMs;
        PixelsPerMs = Math.Clamp(PixelsPerMs * factor, 0.015, 16);
        ViewStartMs = anchorTime - (Bottom - screenY) / PixelsPerMs;
    }

    private void CheckDimensions()
    {
        if (!double.IsFinite(Left) || !double.IsFinite(Bottom) || !double.IsFinite(ViewStartMs)
            || !double.IsFinite(Width) || Width <= 0 || !double.IsFinite(PixelsPerMs) || PixelsPerMs <= 0)
            throw new InvalidOperationException(L.Get("core.timeline.dimensions"));
    }
}
