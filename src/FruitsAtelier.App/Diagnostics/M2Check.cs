using System.Diagnostics;
using System.Text.Json;
using FruitsAtelier.App.Platform;
using FruitsAtelier.Core;

namespace FruitsAtelier.App.Diagnostics;

internal static class M2Check
{
    internal static int Run(IEnumerable<string> archives)
    {
        string artifacts = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(AppLog.Path)!, ".."));
        string output = Path.Combine(artifacts, "m2-validation");
        Directory.CreateDirectory(output);
        var cases = new List<object>();
        foreach (string archive in archives)
        {
            var watch = Stopwatch.StartNew();
            var maps = BeatmapArchive.Import(archive, Path.Combine(artifacts, "beatmaps"));
            foreach (string path in maps)
            {
                var map = OsuBeatmapReader.ReadFile(path);
                var generated = CatchStreamConverter.Convert(map);
                Require(generated.Success, "Imported conversion failed: " + string.Join("; ", generated.Diagnostics));
                var roundTrip = OsuBeatmapWriter.Serialize(map);
                Require(roundTrip.ObjectSequenceMatches, "Unedited map changed object sequence.");
                Require(roundTrip.MaxConvertedTimeErrorMs < 0.001 && roundTrip.MaxConvertedXError < 0.001, "Unedited map changed conversion.");
                string folder = Path.Combine(output, Path.GetFileNameWithoutExtension(archive));
                Directory.CreateDirectory(folder);
                string exportedPath = Path.Combine(folder, "roundtrip.osu");
                EditorWindow.CopyResources(map, folder, roundTrip.ReadBack);
                OsuBeatmapWriter.WriteFile(map, exportedPath);
                var fromDisk = OsuBeatmapReader.ReadFile(exportedPath);
                Require(fromDisk.Fruits.Count == map.Fruits.Count && fromDisk.ImportedSliders.Count == map.ImportedSliders.Count, "File roundtrip lost parents.");
                Require(map.TimingPoints.Select(p => p.OriginalLine).SequenceEqual(fromDisk.TimingPoints.Select(p => p.OriginalLine)), "Timing source order or text changed.");
                var edited = map.DeepClone();
                var fruit = edited.Fruits.First();
                fruit.X = fruit.X > 256 ? fruit.X - 3 : fruit.X + 3;
                fruit.TimeMs = TimingMap.Snap(edited, fruit.TimeMs + 35, 6);
                var curve = new CurveTrack { Name = "M2 authored Bezier" };
                double start = edited.DurationMs - 1500;
                curve.Nodes.Add(new() { TimeMs = start, X = 160, HandleOut = new(125, 80) });
                curve.Nodes.Add(new() { TimeMs = start + 1000, X = 320, HandleIn = new(-125, -50) });
                edited.Tracks.Add(curve);
                string projectPath = Path.Combine(folder, "edited.catchproj");
                ProjectSerializer.WriteFile(edited, projectPath);
                Require(edited.ContentEquals(ProjectSerializer.ReadFile(projectPath)), "Project roundtrip changed authoring state.");
                var changedOutput = OsuBeatmapWriter.WriteFile(edited, Path.Combine(folder, "edited.osu"));
                Require(changedOutput.ReadBack.ImportedSliders.Count == map.ImportedSliders.Count + 1, "Authored curve was not exported as a slider.");
                cases.Add(new
                {
                    archive = Path.GetFileName(archive), map = Path.GetFileName(path),
                    timingPoints = map.TimingPoints.Count, redPoints = map.TimingPoints.Count(p => p.Uninherited),
                    distinctBpms = map.TimingPoints.Where(p => p.Uninherited).Select(p => p.BeatLengthMs).Distinct().Count(),
                    standaloneFruit = map.Fruits.Count, sliders = map.ImportedSliders.Count, bananaShowers = map.BananaShowers.Count,
                    generated = generated.Objects.GroupBy(o => o.Kind).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    uneditedTimeErrorMs = roundTrip.MaxConvertedTimeErrorMs, uneditedXError = roundTrip.MaxConvertedXError,
                    projectRoundTrip = true, editedOutputSequenceMatches = changedOutput.ObjectSequenceMatches,
                    editedOutputXError = changedOutput.ObjectSequenceMatches ? (double?)changedOutput.MaxConvertedXError : null,
                    editedOutputTimeErrorMs = changedOutput.ObjectSequenceMatches ? (double?)changedOutput.MaxConvertedTimeErrorMs : null,
                    editedOutputDiagnostics = changedOutput.Diagnostics, elapsedMs = watch.Elapsed.TotalMilliseconds
                });
            }
        }
        Require(cases.Count > 0, "No test maps supplied.");
        string report = Path.Combine(output, "report.json");
        File.WriteAllText(report, JsonSerializer.Serialize(cases, new JsonSerializerOptions { WriteIndented = true }));
        AppLog.Write("M2 file integration passed: " + report);
        return 0;
    }

    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }
}
