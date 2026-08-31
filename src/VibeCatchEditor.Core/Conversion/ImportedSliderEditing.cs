using L = VibeCatchEditor.Localization.Strings;
namespace VibeCatchEditor.Core;

public sealed record ImportedSliderEditResult(CurveTrack Track, IReadOnlyList<string> Diagnostics);

public static class ImportedSliderEditing
{
    private const double approximationTolerance = 0.001;
    private const int maximumAnchors = 30000;

    public static ImportedSliderEditResult ConvertToTrack(MapDocument document, Guid sliderId)
    {
        ArgumentNullException.ThrowIfNull(document);
        var slider = document.ImportedSliders.SingleOrDefault(s => s.Id == sliderId)
            ?? throw new ArgumentException(L.Get("core.importEditing.notFound"), nameof(sliderId));
        try
        {
            var path = new ImportedSliderGeometry(slider);
            var timing = TimingMap.At(document, slider.TimeMs);
            double velocity = LegacyCatchRules.Velocity(timing.BeatLengthMs, document.SliderMultiplier, timing.SliderVelocityMultiplier);
            if (!double.IsFinite(velocity) || velocity <= 0 || path.Distance <= 0)
                throw new InvalidOperationException(L.Get("core.importEditing.invalidPath"));
            var samples = Simplify(path.TimeXPoints(slider, velocity));
            var track = new CurveTrack
            {
                Id = slider.Id, SourceOrder = slider.SourceOrder, OriginalLine = slider.OriginalLine,
                Name = L.Get("core.names.importedSlider", slider.PathType, slider.TimeMs), Kind = CurveKind.Linear,
                SpanCount = slider.SpanCount, CompensateTinyDroplets = false
            };
            track.Nodes.AddRange(samples.Select(p => new Anchor { TimeMs = p.TimeMs, X = p.X, OutgoingKind = CurveKind.Linear }));

            var candidate = document.DeepClone();
            candidate.ImportedSliders.RemoveAll(s => s.Id == sliderId);
            candidate.Tracks.Add(track);
            var validation = CurveMath.Validate(candidate);
            if (validation.Count > 0) throw new InvalidOperationException(L.Get("core.importEditing.failedPrefix") + validation[0]);
            var before = CatchStreamConverter.Convert(document).Objects.Where(o => o.SourceId == sliderId).ToArray();
            var generated = CatchStreamConverter.Convert(candidate);
            var after = generated.Objects.Where(o => o.SourceId == sliderId).ToArray();
            if (before.Length == 0 || before.Length != after.Length)
                throw new InvalidOperationException(L.Get("core.importEditing.countChangedPrefix") + string.Join(L.Get("core.diagnostics.separator"), generated.Diagnostics.Take(2)));
            for (int i = 0; i < before.Length; i++)
                if (before[i].Kind != after[i].Kind || before[i].EventIndex != after[i].EventIndex || Math.Abs(before[i].TimeMs - after[i].TimeMs) > 0.000001)
                    throw new InvalidOperationException(L.Get("core.importEditing.sequenceChanged"));
            double error = before.Zip(after, (a, b) => Math.Abs(a.X - b.X)).Max();
            string[] diagnostics =
            [
                L.Get("core.importEditing.converted", slider.PathType, track.Nodes.Count, track.SpanCount),
                L.Get("core.importEditing.error", error)
            ];

            // Publish only after validation and event comparison; the caller's history transaction owns undo.
            document.ImportedSliders.Remove(slider);
            document.Tracks.Add(track);
            return new(track, diagnostics);
        }
        catch (CatchConversionException error)
        {
            throw new InvalidOperationException(L.Get("core.importEditing.failedPrefix") + error.Message, error);
        }
    }

    private static IReadOnlyList<MapPoint> Simplify(IReadOnlyList<MapPoint> points)
    {
        if (points.Count < 2) throw new InvalidOperationException(L.Get("core.importEditing.zeroDuration"));
        var keep = new SortedSet<int> { 0, points.Count - 1 };
        var pending = new Stack<(int Start, int End)>();
        pending.Push((0, points.Count - 1));
        long work = 0;
        while (pending.TryPop(out var range))
        {
            var a = points[range.Start]; var b = points[range.End];
            double maximum = approximationTolerance;
            int selected = -1;
            for (int i = range.Start + 1; i < range.End; i++)
            {
                if (++work > 20_000_000) throw new InvalidOperationException(L.Get("core.importEditing.simplificationBudget"));
                double expectedX = a.X + (b.X - a.X) * (points[i].TimeMs - a.TimeMs) / (b.TimeMs - a.TimeMs);
                double error = Math.Abs(points[i].X - expectedX);
                if (error > maximum) { maximum = error; selected = i; }
            }
            if (selected < 0) continue;
            keep.Add(selected);
            if (keep.Count > maximumAnchors) throw new InvalidOperationException(L.Get("core.importEditing.anchorLimit"));
            pending.Push((range.Start, selected));
            pending.Push((selected, range.End));
        }
        return keep.Select(i => points[i]).ToArray();
    }
}
