using System.Diagnostics;
using System.Text.Json;
using VibeCatchEditor.App.Editor;
using VibeCatchEditor.App.Rendering;
using VibeCatchEditor.Core;

namespace VibeCatchEditor.App.Diagnostics;

internal static class RenderCheck
{
    internal static void Run(D2DCanvas canvas, EditorView view)
    {
        var cases = new List<object>();
        foreach (int dpi in new[] { 96, 144, 192 })
        foreach (var size in new[] { (1440, 900), (980, 620) })
        {
            canvas.Resize(size.Item1 * dpi / 96, size.Item2 * dpi / 96, dpi);
            canvas.Begin(); view.Render(canvas, size.Item1, size.Item2); canvas.End();
            cases.Add(new { dpi, widthDip = size.Item1, heightDip = size.Item2, rendered = true });
        }
        canvas.Resize(0, 0, 96);
        canvas.Resize(1440, 900, 96);
        view.Document.Fruits.Clear();
        for (int i = 0; i < 1000; i++)
            view.Document.Fruits.Add(new Fruit { TimeMs = 100 + i * 5, X = 16 + i * 73 % 480 });
        var timings = new List<double>();
        for (int i = 0; i < 65; i++)
        {
            var timer = Stopwatch.StartNew();
            canvas.Begin(); view.Render(canvas, 1440, 900); canvas.End();
            timer.Stop();
            if (i >= 5) timings.Add(timer.Elapsed.TotalMilliseconds);
        }
        timings.Sort();
        var report = new
        {
            adapter = canvas.AdapterName,
            skin = view.SkinName, decodedSkinImages = canvas.LoadedImageCount,
            note = "DPI values exercise render targets and DIP layout; not OS display-setting changes. Hidden-window timing includes EndDraw/Present and is not a visible-refresh guarantee.",
            cases, zeroSizeThenRestore = true, visibleFruitCount = 1000, measuredFrames = timings.Count,
            medianFrameMs = timings[timings.Count / 2], p95FrameMs = timings[(int)(timings.Count * .95)],
            modelErrors = CurveMath.Validate(view.Document)
        };
        var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AppLog.Path)!, "render-check.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        AppLog.Write($"Render check passed: {path}");
    }
}
