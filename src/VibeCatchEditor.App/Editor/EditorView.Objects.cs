using L = VibeCatchEditor.Localization.Strings;
using VibeCatchEditor.App.Rendering;
using VibeCatchEditor.App.Skinning;
using VibeCatchEditor.Core;

namespace VibeCatchEditor.App.Editor;

public sealed partial class EditorView
{
    private void DrawCatchObject(ICanvas c, ConvertedCatchObject item, float x, float y, float fieldWidth)
    {
        float scale = fieldWidth / 512;
        float diameter = CatchSize.FruitDiameter(Document.CircleSize) * scale;
        bool hyper = hyperdashObjects.Contains((item.SourceId, item.EventIndex));
        uint hyperColour = skin?.HyperDashFruitColour ?? 0xFF3030;
        var kind = SkinObjectKind(item.Kind);
        if (skin is not null)
        {
            int skinIndex = skinIndices.GetValueOrDefault(item.SourceId);
            if (hyper) skin.Draw(c, kind, skinIndex, x, y, diameter * 1.2f, hyperColour);
            if (skin.Draw(c, kind, skinIndex, x, y, diameter, 0xFFFFFF)) return;
        }
        float radius = ObjectRadius(item.Kind) * scale;
        if (hyper) c.Circle(x, y, radius * 1.2f, hyperColour);
        c.Circle(x, y, radius, item.Kind == CatchObjectKind.Banana ? Gold : 0xFFFFFF);
    }

    private static CatchSkinObject SkinObjectKind(CatchObjectKind kind) => kind switch
    {
        CatchObjectKind.Droplet => CatchSkinObject.Droplet,
        CatchObjectKind.TinyDroplet => CatchSkinObject.TinyDroplet,
        CatchObjectKind.Banana => CatchSkinObject.Banana,
        _ => CatchSkinObject.Fruit
    };

    private float ObjectRadius(CatchObjectKind kind) => kind switch
    {
        CatchObjectKind.Droplet => CatchSize.DefaultDropletRadius(Document.CircleSize),
        CatchObjectKind.TinyDroplet => CatchSize.DefaultTinyDropletRadius(Document.CircleSize),
        CatchObjectKind.Banana => CatchSize.BananaRadius(Document.CircleSize),
        _ => CatchSize.FruitRadius(Document.CircleSize)
    };

    private ConvertedCatchObject? HitCatchObject(float x, float y)
    {
        EnsureConversion();
        return conversion!.Objects
            .OrderBy(o => PointerDistance(new(o.TimeMs, o.X), x, y))
            .FirstOrDefault(item =>
            {
                var p = Screen(new(item.TimeMs, item.X));
                float scale = Playfield.Width / 512;
                var bounds = skin?.Bounds(SkinObjectKind(item.Kind), skinIndices.GetValueOrDefault(item.SourceId),
                    p.X, p.Y, CatchSize.FruitDiameter(Document.CircleSize) * scale);
                if (bounds is { } b)
                    return Math.Abs(x - p.X) <= Math.Max(7, b.Width / 2)
                        && Math.Abs(y - p.Y) <= Math.Max(7, b.Height / 2);
                return Near(new(item.TimeMs, item.X), x, y, Math.Max(7, ObjectRadius(item.Kind) * scale));
            });
    }

    private Guid HitTrackPath(float x, float y)
    {
        if (!showTargets) return Guid.Empty;
        foreach (var track in Document.Tracks.AsEnumerable().Reverse())
        {
            foreach (var node in track.Nodes)
                if (Near(Point(node), x, y, 9)) return track.Id;
            for (int segment = 0; segment < track.Nodes.Count - 1; segment++)
            {
                var previous = Screen(CurveMath.Evaluate(track, segment, 0));
                for (int sample = 1; sample <= 64; sample++)
                {
                    var point = Screen(CurveMath.Evaluate(track, segment, sample / 64.0));
                    if (SegmentDistance(x, y, previous.X, previous.Y, point.X, point.Y) < 6) return track.Id;
                    previous = point;
                }
            }
        }
        return Guid.Empty;
    }

    private void BeginObjectDrag(float x, float y)
    {
        if (objectSelection.Count == 0) return;
        bool movesOneFruit = objectSelection.Count == 1 && Document.Fruits.Any(item => objectSelection.Contains(item.Id));
        history.Begin(L.Get(movesOneFruit ? "editor.command.moveFruit" : "editor.command.moveObjects"));
        objectDragStart = Document.DeepClone();
        objectDragPrepared = false;
        drag = DragKind.Objects;
        BeginPointerDrag(x, y);
    }

    private void MoveSelectedObjects(float x, float y)
    {
        if (objectDragStart is null) return;
        if (!objectDragPrepared)
        {
            try
            {
                foreach (Guid id in objectSelection.Where(id => Document.ImportedSliders.Any(slider => slider.Id == id)).ToArray())
                    ImportedSliderEditing.ConvertToTrack(Document, id);
                objectDragStart = Document.DeepClone();
                objectDragPrepared = true;
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException)
            {
                history.Cancel();
                objectDragStart = null;
                drag = DragKind.None;
                StatusMessage = error.Message;
                return;
            }
        }
        var startPointer = Transform.ToMap(dragStartX, dragStartY);
        var pointer = Transform.ToMap(x, y);
        double deltaTime = pointer.TimeMs - startPointer.TimeMs;
        double deltaX = pointer.X - startPointer.X;
        double minTime = double.PositiveInfinity, maxTime = double.NegativeInfinity;
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;

        foreach (var fruit in objectDragStart.Fruits.Where(item => objectSelection.Contains(item.Id)))
        {
            IncludeTime(fruit.TimeMs);
            IncludeX(fruit.X);
        }
        foreach (var track in objectDragStart.Tracks.Where(item => objectSelection.Contains(item.Id)))
        {
            foreach (var node in track.Nodes)
            {
                IncludeTime(node.TimeMs);
                IncludeX(node.X);
                IncludeX(node.X + node.HandleIn.X);
                IncludeX(node.X + node.HandleOut.X);
            }
            if (track.Nodes.Count >= 2)
                IncludeTime(track.Nodes[0].TimeMs + (track.Nodes[^1].TimeMs - track.Nodes[0].TimeMs) * track.SpanCount);
        }
        foreach (var shower in objectDragStart.BananaShowers.Where(item => objectSelection.Contains(item.Id)))
        {
            IncludeTime(shower.TimeMs);
            IncludeTime(shower.EndTimeMs);
        }

        if (snap && double.IsFinite(minTime))
            deltaTime = TimingMap.Snap(Document, minTime + deltaTime, divisor) - minTime;
        if (double.IsFinite(minTime)) deltaTime = Math.Clamp(deltaTime, -minTime, Document.DurationMs - maxTime);
        else deltaTime = 0;
        if (double.IsFinite(minX)) deltaX = Math.Clamp(deltaX, -minX, 512 - maxX);
        else deltaX = 0;

        foreach (var source in objectDragStart.Fruits.Where(item => objectSelection.Contains(item.Id)))
        {
            var target = Document.Fruits.Single(item => item.Id == source.Id);
            target.TimeMs = source.TimeMs + deltaTime;
            target.X = source.X + deltaX;
        }
        foreach (var source in objectDragStart.Tracks.Where(item => objectSelection.Contains(item.Id)))
        {
            var target = Document.Tracks.Single(item => item.Id == source.Id);
            foreach (var sourceNode in source.Nodes)
            {
                var targetNode = target.Nodes.Single(node => node.Id == sourceNode.Id);
                targetNode.TimeMs = sourceNode.TimeMs + deltaTime;
                targetNode.X = sourceNode.X + deltaX;
            }
        }
        foreach (var source in objectDragStart.BananaShowers.Where(item => objectSelection.Contains(item.Id)))
        {
            var target = Document.BananaShowers.Single(item => item.Id == source.Id);
            target.TimeMs = source.TimeMs + deltaTime;
            target.EndTimeMs = source.EndTimeMs + deltaTime;
        }
        StatusMessage = L.Get("editor.status.objectsMoved", objectSelection.Count, Number(deltaTime), Number(deltaX));

        void IncludeTime(double value) { minTime = Math.Min(minTime, value); maxTime = Math.Max(maxTime, value); }
        void IncludeX(double value) { minX = Math.Min(minX, value); maxX = Math.Max(maxX, value); }
    }
}
