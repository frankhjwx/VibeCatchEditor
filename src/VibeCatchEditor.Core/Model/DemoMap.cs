using L = VibeCatchEditor.Localization.Strings;

namespace VibeCatchEditor.Core;

public static class DemoMap
{
    public static MapDocument Create()
    {
        var map = new MapDocument { Name = L.Get("core.names.demo"), DurationMs = 30_000, BeatLengthMs = 500 };
        double[] xPositions = [96, 160, 240, 336, 416, 344, 256, 168];
        for (int phrase = 0; phrase < 4; phrase++)
        {
            for (int i = 0; i < xPositions.Length; i++)
                map.Fruits.Add(new Fruit { TimeMs = phrase * 8_000 + 250 + i * 250, X = xPositions[i] });
        }
        map.Fruits.Add(new Fruit { TimeMs = 4_750, X = 128 });
        map.Fruits.Add(new Fruit { TimeMs = 5_000, X = 256 });
        map.Fruits.Add(new Fruit { TimeMs = 5_250, X = 384 });

        var wave = new CurveTrack { Name = L.Get("core.names.demoWave"), Kind = CurveKind.Bezier, CompensateTinyDroplets = true };
        wave.Nodes.Add(new Anchor { TimeMs = 1_000, X = 120, HandleOut = new(350, 10) });
        wave.Nodes.Add(new Anchor { TimeMs = 2_500, X = 392, HandleIn = new(-500, -10), HandleOut = new(350, 0) });
        wave.Nodes.Add(new Anchor { TimeMs = 4_000, X = 168, HandleIn = new(-400, 0) });
        map.Tracks.Add(wave);

        var zigzag = new CurveTrack { Name = L.Get("core.names.demoZigzag"), Kind = CurveKind.Linear, CompensateTinyDroplets = true };
        zigzag.Nodes.Add(new Anchor { TimeMs = 3_000, X = 72 });
        zigzag.Nodes.Add(new Anchor { TimeMs = 3_750, X = 296 });
        zigzag.Nodes.Add(new Anchor { TimeMs = 4_500, X = 448 });
        zigzag.Nodes.Add(new Anchor { TimeMs = 5_250, X = 256 });
        map.Tracks.Add(zigzag);

        var sweep = new CurveTrack { Name = L.Get("core.names.demoSweep"), Kind = CurveKind.Bezier, CompensateTinyDroplets = true };
        sweep.Nodes.Add(new Anchor { TimeMs = 6_000, X = 96, HandleOut = new(400, 64) });
        sweep.Nodes.Add(new Anchor { TimeMs = 7_500, X = 416, HandleIn = new(-600, -16), HandleOut = new(400, 0) });
        sweep.Nodes.Add(new Anchor { TimeMs = 9_000, X = 208, HandleIn = new(-500, 120) });
        map.Tracks.Add(sweep);
        map.Fruits.Sort((a, b) => a.TimeMs.CompareTo(b.TimeMs));
        return map;
    }
}
