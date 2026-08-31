using VibeCatchEditor.Core;

internal static class CurvePointEditingTests
{
    public static void InsertLinearCorner()
    {
        var track = FourPoints();
        track.Nodes[1].OutgoingKind = CurveKind.Linear;
        track.Nodes[1].HandleOut = new(900, 800);
        track.Nodes[2].HandleIn = new(-900, -800);
        var before = Document(track).DeepClone();
        var left = track.Nodes[1]; var right = track.Nodes[2];
        var inserted = CurvePointEditing.InsertCorner(track, 1, 0.4);
        Near(1400, inserted.TimeMs); Near(240, inserted.X);
        Check(inserted.HandleIn == default && inserted.HandleOut == default, "Inserted corner has direction handles.");
        Check(left.HandleOut == default && right.HandleIn == default, "Linear segment activated dormant handles.");
        Check(CurveMath.SegmentKind(track, 1) == CurveKind.Linear && CurveMath.SegmentKind(track, 2) == CurveKind.Linear,
            "Corner insertion did not retain linear children.");
        Check(ReferenceEquals(left, track.Nodes[1]) && ReferenceEquals(right, track.Nodes[3]) && ReferenceEquals(inserted, track.Nodes[2]),
            "Insertion replaced existing anchor references or returned an unattached node.");
        Check(left.HandleIn == before.Tracks[0].Nodes[1].HandleIn && right.HandleOut == before.Tracks[0].Nodes[2].HandleOut,
            "Insertion erased active handles on other segments.");
        CheckMetadata(before.Tracks[0], track);
        Valid(track);
    }

    public static void InsertBezierCorner()
    {
        var track = FourPoints();
        var before = Document(track).DeepClone().Tracks[0];
        const double parameter = 0.3;
        var expected = CurveMath.Evaluate(track, 1, parameter);
        var left = track.Nodes[1]; var right = track.Nodes[2];
        var leftOutgoing = left.HandleOut; var rightIncoming = right.HandleIn;
        var inserted = CurvePointEditing.InsertCorner(track, 1, parameter);
        Near(expected.TimeMs, inserted.TimeMs); Near(expected.X, inserted.X);
        Near(leftOutgoing.TimeMs * parameter, left.HandleOut.TimeMs); Near(leftOutgoing.X * parameter, left.HandleOut.X);
        Near(rightIncoming.TimeMs * (1 - parameter), right.HandleIn.TimeMs); Near(rightIncoming.X * (1 - parameter), right.HandleIn.X);
        Check(inserted.HandleIn == default && inserted.HandleOut == default && !CurvePointEditing.IsCurved(track, inserted.Id),
            "The inserted point is not a corner.");
        Check(CurveMath.SegmentKind(track, 1) == CurveKind.Bezier && CurveMath.SegmentKind(track, 2) == CurveKind.Bezier,
            "Neighbour direction handles were lost when inserting a corner.");
        Check(left.HandleIn == before.Nodes[1].HandleIn && right.HandleOut == before.Nodes[2].HandleOut,
            "Unsplit neighbour segments changed.");
        for (int i = 0; i <= 30; i++)
        {
            Near(CurveMath.Evaluate(before, 0, i / 30.0).X, CurveMath.Evaluate(track, 0, i / 30.0).X);
            Near(CurveMath.Evaluate(before, 2, i / 30.0).X, CurveMath.Evaluate(track, 3, i / 30.0).X);
        }
        Valid(track);
    }

    public static void CornerKeepsNeighbourHandles()
    {
        var track = FourPoints();
        var before = Document(track).DeepClone().Tracks[0];
        var node = track.Nodes[1];
        CurvePointEditing.SetCurved(track, node.Id, false);
        Check(!CurvePointEditing.IsCurved(track, node.Id) && node.HandleIn == default && node.HandleOut == default,
            "Converting to a corner retained its own handles.");
        Check(track.Nodes[0].HandleOut == before.Nodes[0].HandleOut && track.Nodes[2].HandleIn == before.Nodes[2].HandleIn,
            "Converting a point erased neighbouring active handles.");
        Check(CurveMath.SegmentKind(track, 0) == CurveKind.Bezier && CurveMath.SegmentKind(track, 1) == CurveKind.Bezier,
            "Segments with neighbour handles were incorrectly flattened.");
        CurvePointEditing.SetCurved(track, track.Nodes[0].Id, false);
        Check(CurveMath.SegmentKind(track, 0) == CurveKind.Linear, "A segment with no active handles did not become linear.");
        Valid(track);
    }

    public static void CurvedPointClearsDormantHandles()
    {
        var track = FourPoints();
        track.Nodes[0].OutgoingKind = CurveKind.Linear;
        track.Nodes[1].OutgoingKind = CurveKind.Linear;
        track.Nodes[0].HandleOut = new(999, 2000);
        track.Nodes[1].HandleIn = new(-999, -2000);
        track.Nodes[1].HandleOut = new(999, 2000);
        track.Nodes[2].HandleIn = new(-999, -2000);
        var node = track.Nodes[1];
        var nextOutgoing = track.Nodes[2].HandleOut;
        Check(!CurvePointEditing.IsCurved(track, node.Id), "Inactive line handles classified the point as curved.");
        CurvePointEditing.SetCurved(track, node.Id, true);
        Check(CurvePointEditing.IsCurved(track, node.Id), "Curve conversion has no active handle.");
        Check(node.HandleIn.TimeMs < 0 && node.HandleOut.TimeMs > 0, "The central tangent has incorrect directions.");
        Near(node.HandleIn.X / node.HandleIn.TimeMs, node.HandleOut.X / node.HandleOut.TimeMs);
        Check(track.Nodes[0].HandleOut == default && track.Nodes[2].HandleIn == default,
            "Curve conversion reactivated dormant neighbour handles.");
        Check(track.Nodes[2].HandleOut == nextOutgoing, "Curve conversion modified an unrelated active handle.");
        Check(CurveMath.SegmentKind(track, 0) == CurveKind.Bezier && CurveMath.SegmentKind(track, 1) == CurveKind.Bezier,
            "The new tangent is not active on both adjacent segments.");
        Valid(track);
        var snapshot = Document(track).DeepClone();
        CurvePointEditing.SetCurved(track, node.Id, true);
        Check(Document(track).ContentEquals(snapshot), "Converting an already curved point changed its handles.");
    }

    public static void BoundaryTangents()
    {
        foreach (int index in new[] { 0, 1, 2 })
        foreach (double x in new[] { 0.0, 512.0 })
        {
            var track = new CurveTrack { Kind = CurveKind.Linear };
            track.Nodes.Add(new() { TimeMs = 0, X = 100 });
            track.Nodes.Add(new() { TimeMs = 0.003, X = 256 });
            track.Nodes.Add(new() { TimeMs = 0.006, X = 400 });
            var node = track.Nodes[index]; node.X = x;
            CurvePointEditing.SetCurved(track, node.Id, true);
            Check(CurvePointEditing.IsCurved(track, node.Id), "Boundary point has no active direction handle.");
            Valid(track);
        }
        var crowded = new CurveTrack { Kind = CurveKind.Bezier };
        crowded.Nodes.Add(new() { TimeMs = 0, X = 200, HandleOut = new(1000, 10) });
        crowded.Nodes.Add(new() { TimeMs = 1000, X = 256 });
        crowded.Nodes.Add(new() { TimeMs = 2000, X = 300, HandleIn = new(-1000, -10) });
        var beforeOut = crowded.Nodes[0].HandleOut; var afterIn = crowded.Nodes[2].HandleIn;
        CurvePointEditing.SetCurved(crowded, crowded.Nodes[1].Id, true);
        Check(CurvePointEditing.IsCurved(crowded, crowded.Nodes[1].Id), "No curve handle was created with saturated time controls.");
        Check(crowded.Nodes[0].HandleOut == beforeOut && crowded.Nodes[2].HandleIn == afterIn, "Saturated neighbour handles were moved.");
        Valid(crowded);
    }

    public static void InvalidOperationsAreAtomic()
    {
        var track = FourPoints();
        var snapshot = Document(track).DeepClone();
        foreach (double parameter in new[] { 0.0, 1.0, double.NaN, double.PositiveInfinity, 1e-9 })
        {
            Reject(() => CurvePointEditing.InsertCorner(track, 1, parameter));
            Check(Document(track).ContentEquals(snapshot), "A rejected split changed the document.");
        }
        Reject(() => CurvePointEditing.InsertCorner(track, -1, 0.5));
        Reject(() => CurvePointEditing.SetCurved(track, Guid.NewGuid(), true));
        Check(Document(track).ContentEquals(snapshot), "An invalid point or segment changed the document.");
        track.Nodes[2].TimeMs = track.Nodes[1].TimeMs;
        snapshot = Document(track).DeepClone();
        Reject(() => CurvePointEditing.InsertCorner(track, 1, 0.5));
        Reject(() => CurvePointEditing.SetCurved(track, track.Nodes[1].Id, false));
        Check(Document(track).ContentEquals(snapshot), "Duplicate-time rejection partially changed handles.");
    }

    public static void HistoryPreservesRepeatAndMetadata()
    {
        var source = Document(FourPoints());
        var history = new EditorHistory(source);
        history.Begin("Insert corner");
        var track = history.Document.Tracks[0];
        var inserted = CurvePointEditing.InsertCorner(track, 1, 0.5);
        history.Commit();
        var added = history.Document.DeepClone();
        CheckMetadata(source.Tracks[0], track);
        history.Undo();
        Check(history.Document.ContentEquals(source), "Undo did not restore the original points and metadata.");
        history.Redo();
        Check(history.Document.ContentEquals(added), "Redo did not restore the inserted point identity.");
        history.Begin("Curve point");
        CurvePointEditing.SetCurved(history.Document.Tracks[0], inserted.Id, true);
        history.Commit();
        history.Undo();
        Check(history.Document.ContentEquals(added), "Undo curve conversion did not restore the corner and neighbouring controls.");
    }

    public static void RemoveMixedPoint()
    {
        foreach (bool leftLinear in new[] { true, false })
        {
            var track = FourPoints();
            int line = leftLinear ? 0 : 1;
            track.Nodes[line].OutgoingKind = CurveKind.Linear;
            track.Nodes[line].HandleOut = new(900, 2000);
            track.Nodes[line + 1].HandleIn = new(-900, -2000);
            var before = Document(track).DeepClone().Tracks[0];
            var previous = track.Nodes[0]; var next = track.Nodes[2];
            CurvePointEditing.Remove(track, track.Nodes[1].Id);
            Check(track.Nodes.Count == 3 && ReferenceEquals(track.Nodes[0], previous) && ReferenceEquals(track.Nodes[1], next),
                "Removing an internal point replaced its neighbours.");
            Check(previous.HandleOut == (leftLinear ? default : before.Nodes[0].HandleOut)
                && next.HandleIn == (leftLinear ? before.Nodes[2].HandleIn : default),
                "Merged segment revived a dormant handle or discarded an active neighbour handle.");
            Check(next.HandleOut == before.Nodes[2].HandleOut, "Removing a point changed an unrelated segment.");
            Check(CurveMath.SegmentKind(track, 0) == CurveKind.Bezier, "Remaining neighbour handle is not active on the merged segment.");
            CheckMetadata(before, track);
            Valid(track);
        }
        var straight = FourPoints();
        straight.Nodes[0].OutgoingKind = straight.Nodes[1].OutgoingKind = CurveKind.Linear;
        CurvePointEditing.Remove(straight, straight.Nodes[1].Id);
        Check(CurveMath.SegmentKind(straight, 0) == CurveKind.Linear && straight.Nodes[0].HandleOut == default
            && straight.Nodes[1].HandleIn == default, "Two lines became a curve through dormant handles after deletion.");
        Valid(straight);
    }

    public static void RemoveRejectionAndHistory()
    {
        var source = Document(FourPoints());
        var history = new EditorHistory(source);
        var track = history.Document.Tracks[0];
        Reject(() => CurvePointEditing.Remove(track, track.Nodes[0].Id));
        Reject(() => CurvePointEditing.Remove(track, track.Nodes[^1].Id));
        Reject(() => CurvePointEditing.Remove(track, Guid.NewGuid()));
        Check(history.Document.ContentEquals(source), "Rejected deletion changed the source document.");
        history.Begin("Remove point");
        CurvePointEditing.Remove(track, track.Nodes[1].Id);
        history.Commit();
        var removed = history.Document.DeepClone();
        history.Undo();
        Check(history.Document.ContentEquals(source), "Undo removal did not restore point handles, repeat and original metadata.");
        history.Redo();
        Check(history.Document.ContentEquals(removed), "Redo removal changed point identity or merged controls.");
        track = history.Document.Tracks[0];
        CurvePointEditing.Remove(track, track.Nodes[1].Id);
        Check(track.Nodes.Count == 2, "Deleting the remaining internal point failed.");
        var twoPoints = history.Document.DeepClone();
        Reject(() => CurvePointEditing.Remove(track, track.Nodes[0].Id));
        Reject(() => CurvePointEditing.Remove(track, track.Nodes[1].Id));
        Check(history.Document.ContentEquals(twoPoints), "Two-point rejection modified the track.");
    }

    private static CurveTrack FourPoints()
    {
        var track = new CurveTrack { Kind = CurveKind.Bezier, SpanCount = 3, SourceOrder = 17,
            OriginalLine = "source samples retained", CompensateTinyDroplets = false, Name = "Imported track" };
        track.Nodes.Add(new() { TimeMs = 0, X = 100, HandleOut = new(200, 20) });
        track.Nodes.Add(new() { TimeMs = 1000, X = 200, HandleIn = new(-200, -20), HandleOut = new(250, 50) });
        track.Nodes.Add(new() { TimeMs = 2000, X = 300, HandleIn = new(-250, -50), HandleOut = new(200, 20) });
        track.Nodes.Add(new() { TimeMs = 3000, X = 400, HandleIn = new(-200, -20) });
        return track;
    }

    private static MapDocument Document(CurveTrack track)
    {
        var document = new MapDocument { DurationMs = 30000 };
        document.Tracks.Add(track);
        return document;
    }

    private static void CheckMetadata(CurveTrack before, CurveTrack after)
        => Check(before.Id == after.Id && before.Kind == after.Kind && before.Name == after.Name && before.SpanCount == after.SpanCount
            && before.SourceOrder == after.SourceOrder && before.OriginalLine == after.OriginalLine
            && before.CompensateTinyDroplets == after.CompensateTinyDroplets, "Point editing changed parent metadata or repeat.");

    private static void Valid(CurveTrack track)
    {
        var errors = CurveMath.Validate(Document(track));
        Check(errors.Count == 0, string.Join("; ", errors));
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid point operation was accepted.");
    }

    private static void Near(double expected, double actual)
        => Check(double.IsFinite(actual) && Math.Abs(expected - actual) < 1e-8, $"Expected {expected}, got {actual}.");

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
