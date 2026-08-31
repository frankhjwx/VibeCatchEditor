using VibeCatchEditor.Core;

internal static class BatchPointTests
{
    public static void MixedBatchPreservesUntouchedSegments()
    {
        var track = Track();
        track.Nodes[0].OutgoingKind = CurveKind.Linear;
        track.Nodes[0].HandleOut = new(900, 800);
        track.Nodes[1].HandleIn = new(-900, -800);
        track.Nodes[4].OutgoingKind = CurveKind.Linear;
        track.Nodes[4].HandleOut = new(900, 900);
        track.Nodes[5].HandleIn = new(-900, -900);
        var old = With(track).DeepClone().Tracks[0];
        var preserved = new[] { track.Nodes[0], track.Nodes[3], track.Nodes[4], track.Nodes[5], track.Nodes[7] };
        CurvePointEditing.RemoveMany(track, new[] { track.Nodes[1].Id, track.Nodes[2].Id, track.Nodes[6].Id });
        Check(track.Nodes.SequenceEqual(preserved), "Batch deletion replaced references or removed an unselected point.");
        Check(track.Nodes[0].HandleOut == default && track.Nodes[1].HandleIn == old.Nodes[3].HandleIn,
            "Merged segment revived a dormant outgoing handle or lost an active incoming handle.");
        Check(CurveMath.SegmentKind(track, 0) == CurveKind.Bezier && CurveMath.SegmentKind(track, 3) == CurveKind.Bezier,
            "New adjacency did not derive its kind from surviving active handles.");
        Check(track.Nodes[1].OutgoingKind == old.Nodes[3].OutgoingKind && track.Nodes[2].OutgoingKind == CurveKind.Linear,
            "Untouched segment overrides were normalised or changed.");
        Check(track.Nodes[2].HandleOut == old.Nodes[4].HandleOut && track.Nodes[3].HandleIn == old.Nodes[5].HandleIn,
            "An unchanged linear segment lost its hidden control handles.");
        for (int i = 0; i <= 20; i++)
        {
            Near(CurveMath.Evaluate(old, 3, i / 20.0).X, CurveMath.Evaluate(track, 1, i / 20.0).X);
            Near(CurveMath.Evaluate(old, 4, i / 20.0).X, CurveMath.Evaluate(track, 2, i / 20.0).X);
        }
        Valid(track);
    }

    public static void EndpointBatchAndNewLine()
    {
        var track = Track();
        track.Nodes[2].OutgoingKind = CurveKind.Linear;
        track.Nodes[2].HandleOut = new(900, 800);
        track.Nodes[3].HandleIn = new(-900, -800);
        track.Nodes[4].OutgoingKind = CurveKind.Linear;
        track.Nodes[4].HandleOut = new(900, 900);
        track.Nodes[5].HandleIn = new(-900, -900);
        var first = track.Nodes[2]; var last = track.Nodes[5];
        var ids = track.Nodes.Where(n => n != first && n != last).Select(n => n.Id);
        CurvePointEditing.RemoveMany(track, ids);
        Check(track.Nodes.Count == 2 && ReferenceEquals(first, track.Nodes[0]) && ReferenceEquals(last, track.Nodes[1]),
            "One batch could not delete both endpoints and intervening points.");
        Check(first.HandleIn == default && last.HandleOut == default, "New endpoint kept a now-unused direction handle.");
        Check(first.HandleOut == default && last.HandleIn == default && CurveMath.SegmentKind(track, 0) == CurveKind.Linear,
            "Merged inactive boundary handles created a curve.");
        Near(2000, first.TimeMs); Near(5000, last.TimeMs);
        Valid(track);
    }

    public static void EndpointOnlyPreservesAdjacentState()
    {
        var track = Track();
        track.Nodes[1].OutgoingKind = CurveKind.Linear;
        track.Nodes[1].HandleOut = new(900, 1400);
        track.Nodes[2].HandleIn = new(-900, -1400);
        var before = With(track).DeepClone().Tracks[0];
        CurvePointEditing.RemoveMany(track, new[] { track.Nodes[0].Id, track.Nodes[^1].Id });
        Check(track.Nodes[0].HandleIn == default && track.Nodes[^1].HandleOut == default,
            "Trimming the range did not clear the new endpoints' unused handles.");
        for (int i = 0; i < track.Nodes.Count; i++)
        {
            var node = track.Nodes[i]; var original = before.Nodes[i + 1];
            Check(node.OutgoingKind == original.OutgoingKind, "An unchanged segment lost its explicit/default interpolation type.");
            if (i != 0) Check(node.HandleIn == original.HandleIn, "Unchanged incoming control was modified.");
            if (i != track.Nodes.Count - 1) Check(node.HandleOut == original.HandleOut, "Unchanged outgoing control was modified.");
        }
        Valid(track);
    }

    public static void EmptyDuplicatesAndFailures()
    {
        var track = Track();
        var before = With(track).DeepClone();
        CurvePointEditing.RemoveMany(track, Array.Empty<Guid>());
        Check(With(track).ContentEquals(before), "Empty selection changed the track.");
        var duplicate = track.Nodes[3].Id;
        CurvePointEditing.RemoveMany(track, new[] { duplicate, duplicate });
        Check(track.Nodes.Count == 7 && track.Nodes.All(n => n.Id != duplicate), "Duplicate IDs were counted as multiple removals.");
        before = With(track).DeepClone();
        Reject(() => CurvePointEditing.RemoveMany(track, new[] { track.Nodes[1].Id, Guid.NewGuid() }));
        Check(With(track).ContentEquals(before), "Unknown ID caused a partial batch deletion.");
        Reject(() => CurvePointEditing.RemoveMany(track, track.Nodes.Select(n => n.Id)));
        Reject(() => CurvePointEditing.RemoveMany(track, track.Nodes.Skip(1).Select(n => n.Id)));
        Check(With(track).ContentEquals(before), "Too few remaining anchors caused a partial deletion.");
    }

    public static void BatchHistoryPreservesIdentityAndRepeat()
    {
        var original = With(Track());
        var history = new EditorHistory(original);
        var track = history.Document.Tracks[0];
        var selected = new[] { track.Nodes[0].Id, track.Nodes[2].Id, track.Nodes[^1].Id };
        history.Begin("Batch delete points");
        CurvePointEditing.RemoveMany(track, selected);
        history.Commit();
        var deleted = history.Document.DeepClone();
        var old = original.Tracks[0];
        Check(track.Id == old.Id && track.Name == old.Name && track.Kind == old.Kind && track.SpanCount == old.SpanCount
            && track.OriginalLine == old.OriginalLine && track.SourceOrder == old.SourceOrder
            && track.CompensateTinyDroplets == old.CompensateTinyDroplets, "Batch deletion changed repeat or source metadata.");
        history.Undo();
        Check(history.Document.ContentEquals(original), "One undo did not restore every deleted point and original controls.");
        history.Redo();
        Check(history.Document.ContentEquals(deleted), "One redo did not restore the batch result and retained IDs.");
        Valid(history.Document.Tracks[0]);
    }

    private static CurveTrack Track()
    {
        var track = new CurveTrack { Kind = CurveKind.Bezier, SpanCount = 3, SourceOrder = 19,
            OriginalLine = "preserved samples", CompensateTinyDroplets = false, Name = "Batch fixture" };
        for (int i = 0; i < 8; i++) track.Nodes.Add(new Anchor { TimeMs = i * 1000, X = 80 + i * 45,
            HandleIn = new(-200, -8), HandleOut = new(200, 8), OutgoingKind = i == 3 ? CurveKind.Bezier : null });
        return track;
    }

    private static MapDocument With(CurveTrack track)
    {
        var document = new MapDocument { DurationMs = 30000 };
        document.Tracks.Add(track);
        return document;
    }
    private static void Valid(CurveTrack track)
    {
        var errors = CurveMath.Validate(With(track));
        Check(errors.Count == 0, string.Join("; ", errors));
    }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid batch was accepted.");
    }
    private static void Near(double expected, double actual)
        => Check(double.IsFinite(actual) && Math.Abs(expected - actual) < 1e-8, $"Expected {expected}, got {actual}.");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
