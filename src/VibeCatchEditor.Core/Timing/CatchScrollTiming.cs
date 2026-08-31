namespace VibeCatchEditor.Core;

public static class CatchScrollTiming
{
    public const double PlayfieldWidth = 512;
    public const double SpawnY = -100;
    public const double CatchY = 340;
    public const double FallDistance = CatchY - SpawnY;

    public static double PreemptMs(double approachRate)
    {
        if (!double.IsFinite(approachRate) || approachRate < 0 || approachRate > 10)
            throw new ArgumentOutOfRangeException(nameof(approachRate));

        // Legacy difficulty stores AR as float, then truncates the double preempt result to whole milliseconds.
        double legacyAr = (float)approachRate;
        double progress = (legacyAr - 5) / 5;
        return (int)(legacyAr > 5 ? 1200 - 750 * progress : 1200 - 600 * progress);
    }

    public static double PixelsPerMs(double approachRate, double playfieldWidth)
    {
        if (!double.IsFinite(playfieldWidth) || playfieldWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(playfieldWidth));

        // Scale the 440-unit fall by the same width ratio as X so window height cannot distort spacing.
        return FallDistance / PreemptMs(approachRate) * (playfieldWidth / PlayfieldWidth);
    }
}
