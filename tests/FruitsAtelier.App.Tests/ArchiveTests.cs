using System.IO.Compression;
using FruitsAtelier.App.Platform;

internal static class ArchiveTests
{
    public static void Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "global.json"))) root = root.Parent;
        string folder = Path.Combine(root!.FullName, "artifacts", "tests", "beatmap-archives", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string cache = Path.Combine(folder, "cache");
        string valid = Package("valid.osz", ["set/map.osu", "set/audio.ogg", "set/scene.osb", "set/video.mp4"]);
        var maps = BeatmapArchive.Import(valid, cache);
        Check(maps.Count == 1 && File.Exists(maps[0]), "Map missing from import.");
        string set = Path.GetDirectoryName(maps[0])!;
        Check(File.Exists(Path.Combine(set, "audio.ogg")), "Audio resource layout changed.");
        Check(!File.Exists(Path.Combine(set, "scene.osb")) && !File.Exists(Path.Combine(set, "video.mp4")), "Video or storyboard was extracted.");
        Check(BeatmapArchive.Import(valid, cache).SequenceEqual(maps), "Completed import was not reused.");
        foreach (string bad in new[] { "../escape.wav", "C:/escape.wav", "folder/CON.png", "folder/name:ads.mp3" })
            Reject(Package(Guid.NewGuid() + ".osz", ["map.osu", bad]));
        Reject(Package("duplicates.osz", ["map.osu", "MAP.osu"]));
        string marker = Path.Combine(Directory.GetParent(set)!.FullName, ".complete");
        File.WriteAllText(marker, "incomplete");
        Reject(valid);

        string Package(string name, string[] entries)
        {
            string path = Path.Combine(folder, name);
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (string entry in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
                writer.Write("fixture");
            }
            return path;
        }
        void Reject(string path)
        {
            try { BeatmapArchive.Import(path, cache); }
            catch (InvalidDataException) { return; }
            throw new Exception("Unsafe archive was accepted: " + path);
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
