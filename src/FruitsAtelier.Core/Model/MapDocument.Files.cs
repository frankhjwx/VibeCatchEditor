namespace FruitsAtelier.Core;

public sealed class OsuSection
{
    public string Name { get; set; } = "";
    public List<string> Lines { get; } = [];
}

public sealed partial class MapDocument
{
    public string? SourcePath { get; set; }
    public string? AudioPath { get; set; }
    public bool IsDemo { get; set; } = true;
    public List<OsuSection> OriginalSections { get; } = [];

    private void CopyFileStateTo(MapDocument copy)
    {
        copy.SourcePath = SourcePath;
        copy.AudioPath = AudioPath;
        copy.IsDemo = IsDemo;
        foreach (var section in OriginalSections)
        {
            var cloned = new OsuSection { Name = section.Name };
            cloned.Lines.AddRange(section.Lines);
            copy.OriginalSections.Add(cloned);
        }
    }

    private bool FileStateEquals(MapDocument other) => SourcePath == other.SourcePath
        && AudioPath == other.AudioPath && IsDemo == other.IsDemo
        && OriginalSections.Count == other.OriginalSections.Count
        && OriginalSections.Zip(other.OriginalSections).All(pair => pair.First.Name == pair.Second.Name
            && pair.First.Lines.SequenceEqual(pair.Second.Lines));
}
