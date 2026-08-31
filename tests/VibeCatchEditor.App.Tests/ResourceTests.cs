using VibeCatchEditor.App.Platform;
using VibeCatchEditor.Core;

internal static class ResourceTests
{
    public static void Run()
    {
        NestedDestination(false);
        NestedDestination(true);
        ReplacementAudio();
        UnchangedNestedAudio();
        ExistingConflict();
        SameDirectory();
    }

    private static void NestedDestination(bool alreadyExists)
    {
        var fixture = new Fixture(alreadyExists ? "existing-nested-export" : "new-nested-export");
        fixture.Write("source/audio.mp3", "original music");
        fixture.Write("source/background.png", "background");
        fixture.Write("source/export-backup/keep.png", "sibling image");
        string destination = fixture.PathOf("source/export");
        if (alreadyExists) fixture.Write("source/export/previous.png", "prior output");
        fixture.Copy(destination);
        Check(File.ReadAllText(Path.Combine(destination, "audio.mp3")) == "original music", "Nested export lost its audio.");
        Check(File.Exists(Path.Combine(destination, "background.png")), "Nested export lost its background.");
        Check(File.Exists(Path.Combine(destination, "export-backup/keep.png")), "Destination exclusion also removed a sibling with the same prefix.");
        Check(!Directory.Exists(Path.Combine(destination, "export")), "The export directory was copied into itself.");
        fixture.Copy(destination);
        Check(!Directory.Exists(Path.Combine(destination, "export")), "Repeated export recursively copied the prior output.");
        if (alreadyExists) Check(File.ReadAllText(Path.Combine(destination, "previous.png")) == "prior output", "Existing unrelated output changed.");
    }

    private static void ReplacementAudio()
    {
        var fixture = new Fixture("replacement-same-name");
        fixture.Write("source/audio.mp3", "old music");
        fixture.Write("replacement/audio.mp3", "new music");
        fixture.Document.AudioPath = fixture.PathOf("replacement/audio.mp3");
        string destination = fixture.PathOf("output");
        var output = fixture.Copy(destination);
        string writtenPath = OsuBeatmapReader.Read(output.Text, Path.Combine(destination, "map.osu")).AudioPath!;
        Check(File.ReadAllText(writtenPath) == "new music", "Written AudioFilename does not resolve to the replacement audio.");
        Check(File.ReadAllText(fixture.PathOf("source/audio.mp3")) == "old music", "Replacing output audio modified the source resource.");
        fixture.Copy(destination);
    }

    private static void UnchangedNestedAudio()
    {
        var fixture = new Fixture("nested-audio", "music/audio.mp3");
        fixture.Write("source/music/audio.mp3", "nested music");
        string destination = fixture.PathOf("output");
        var output = fixture.Copy(destination);
        string writtenPath = OsuBeatmapReader.Read(output.Text, Path.Combine(destination, "map.osu")).AudioPath!;
        Check(writtenPath == Path.Combine(destination, "music/audio.mp3").Replace('/', Path.DirectorySeparatorChar), "Writer did not preserve nested AudioFilename.");
        Check(File.ReadAllText(writtenPath) == "nested music", "Nested AudioFilename does not resolve after copying.");
        Check(!File.Exists(Path.Combine(destination, "audio.mp3")), "Unchanged nested audio was also copied to an unused flat path.");
    }

    private static void ExistingConflict()
    {
        var fixture = new Fixture("existing-conflict");
        fixture.Write("source/audio.mp3", "music");
        fixture.Write("source/background.png", "new background");
        fixture.Write("output/background.png", "user background");
        Reject(() => fixture.Copy(fixture.PathOf("output")));
        Check(File.ReadAllText(fixture.PathOf("output/background.png")) == "user background", "Export overwrote an existing different resource.");
        Check(!File.Exists(fixture.PathOf("output/audio.mp3")), "Conflict was detected only after writing part of the resource plan.");
    }

    private static void SameDirectory()
    {
        var fixture = new Fixture("same-directory");
        fixture.Write("source/audio.mp3", "original");
        fixture.Write("source/background.png", "background");
        fixture.Copy(fixture.PathOf("source"));
        Check(Directory.EnumerateFiles(fixture.PathOf("source"), "*", SearchOption.AllDirectories).Count() == 2,
            "Same-directory export duplicated resources.");
        fixture.Write("replacement/audio.mp3", "replacement");
        fixture.Document.AudioPath = fixture.PathOf("replacement/audio.mp3");
        Reject(() => fixture.Copy(fixture.PathOf("source")));
        Check(File.ReadAllText(fixture.PathOf("source/audio.mp3")) == "original", "Same-directory replacement overwrote the original audio.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject(Action operation)
    {
        try { operation(); }
        catch (IOException) { return; }
        throw new Exception("Conflicting resource export was accepted.");
    }

    private sealed class Fixture
    {
        private readonly string folder;
        public MapDocument Document { get; }
        public Fixture(string name, string audioName = "audio.mp3")
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "global.json"))) root = root.Parent;
            folder = Path.Combine(root!.FullName, "artifacts", "tests", "beatmap-resources", name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(PathOf("source"));
            Document = OsuBeatmapReader.Read($"osu file format v14\n[General]\nAudioFilename: {audioName}\nMode: 2\n[TimingPoints]\n0,500,4,1,0,100,1,0\n[HitObjects]\n256,192,1000,1,0,0:0:0:0:\n", PathOf("source/map.osu"));
        }
        public string PathOf(string relative) => Path.GetFullPath(Path.Combine(folder, relative));
        public void Write(string relative, string value)
        {
            string path = PathOf(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, value);
        }
        public OsuWriteResult Copy(string destination)
        {
            var result = OsuBeatmapWriter.Serialize(Document);
            BeatmapResources.Copy(Document, destination, result.ReadBack);
            return result;
        }
    }
}
