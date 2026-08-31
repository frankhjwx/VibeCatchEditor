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
                float scale = plot.Width / 512;
                var bounds = skin?.Bounds(SkinObjectKind(item.Kind), skinIndices.GetValueOrDefault(item.SourceId),
                    p.X, p.Y, CatchSize.FruitDiameter(Document.CircleSize) * scale);
                if (bounds is { } b)
                    return Math.Abs(x - p.X) <= Math.Max(7, b.Width / 2)
                        && Math.Abs(y - p.Y) <= Math.Max(7, b.Height / 2);
                return Near(new(item.TimeMs, item.X), x, y, Math.Max(7, ObjectRadius(item.Kind) * scale));
            });
    }
}
