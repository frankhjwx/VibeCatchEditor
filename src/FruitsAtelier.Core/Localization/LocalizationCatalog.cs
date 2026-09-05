using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FruitsAtelier.Localization;

public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, string> english;
    private readonly Dictionary<string, Dictionary<string, string>> translations;
    public IReadOnlyList<string> AvailableLanguages { get; }

    private LocalizationCatalog(Dictionary<string, Dictionary<string, string>> tables)
    {
        if (!tables.TryGetValue("en", out var source)) throw new FormatException("English source table is required.");
        english = source;
        translations = tables;
        AvailableLanguages = Array.AsReadOnly(tables.Keys.OrderBy(key => key == "en" ? 0 : 1).ThenBy(key => key, StringComparer.Ordinal).ToArray());
    }

    public static LocalizationCatalog Parse(string englishJson, string chineseJson) => Parse(new Dictionary<string, string> { ["en"] = englishJson, ["zh-CN"] = chineseJson });
    public static LocalizationCatalog Parse(IReadOnlyDictionary<string, string> tables) => new(tables.ToDictionary(pair => pair.Key, pair => Read(pair.Value), StringComparer.Ordinal));

    public string Get(string language, string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!english.TryGetValue(key, out string? template)) return $"[{key}]";
        if (translations.TryGetValue(language, out var table) && table.TryGetValue(key, out string? translated)) template = translated;
        CultureInfo culture;
        try { culture = CultureInfo.GetCultureInfo(language); }
        catch (CultureNotFoundException) { culture = CultureInfo.InvariantCulture; }
        return string.Format(culture, template, args);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var (language, table) in translations.Where(pair => pair.Key != "en"))
        {
            foreach (string key in english.Keys.Except(table.Keys)) errors.Add($"{language} missing key: {key}");
            foreach (string key in table.Keys.Except(english.Keys)) errors.Add($"{language} unknown key: {key}");
        }
        foreach (var (key, template) in english)
        {
            var expected = Arguments(template, "en", key, errors);
            foreach (var (language, table) in translations.Where(pair => pair.Key != "en"))
            {
                if (!table.TryGetValue(key, out string? translated)) continue;
                var actual = Arguments(translated, language, key, errors);
                if (expected is not null && actual is not null && !expected.SetEquals(actual))
                    errors.Add($"Placeholder mismatch in {language}: {key}");
            }
        }
        return errors;
    }

    private static HashSet<int>? Arguments(string template, string language, string key, List<string> errors)
    {
        try { _ = CompositeFormat.Parse(template); }
        catch (FormatException) { errors.Add($"Invalid format in {language}: {key}"); return null; }
        var result = new HashSet<int>();
        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] != '{') continue;
            if (i + 1 < template.Length && template[i + 1] == '{') { i++; continue; }
            int start = ++i;
            while (i < template.Length && char.IsAsciiDigit(template[i])) i++;
            result.Add(int.Parse(template.AsSpan(start, i - start), CultureInfo.InvariantCulture));
        }
        return result;
    }

    private static Dictionary<string, string> Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new FormatException("Language table must be an object.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String) throw new FormatException($"Language value must be a string: {property.Name}");
            if (!result.TryAdd(property.Name, property.Value.GetString()!)) throw new FormatException($"Duplicate language key: {property.Name}");
        }
        return result;
    }
}
