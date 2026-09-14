namespace VocabBridge.Models;

public sealed record Language(string Code, string Name, string Sample)
{
    public override string ToString() => Name;
}

public static class Languages
{
    public static readonly Language[] All =
    [
        new("my", "Myanmar", "မင်္ဂလာပါ။ နေကောင်းလား။"),
        new("en", "English", "Hello. Welcome to VocabBridge."),
        new("ru", "Russian", "Здравствуйте. Добро пожаловать.")
    ];
    public static Language Get(string code) => All.First(x => x.Code == code);
    public static bool Contains(string code) => All.Any(x => x.Code == code);
}

public sealed class Word
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = "vocabulary";
    public Dictionary<string, string> Texts { get; set; } = [];
    public string Text(string language) => Texts.GetValueOrDefault(language, "");
}

public sealed class Settings
{
    public bool SetupComplete { get; set; }
    public List<string> SelectedLanguages { get; set; } = [];
    public Dictionary<string, string> VoiceNames { get; set; } = [];
    public double Rate { get; set; } = 1;
    public int GapMs { get; set; } = 500;
    public int Loops { get; set; } = 1;
}

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 2;
    public List<Word> Words { get; set; } = [];
    public Settings Settings { get; set; } = new();
}

public sealed record WordColumn(string Key, string Heading, string Language);

public static class WordColumns
{
    public static IReadOnlyList<WordColumn> For(string category, IEnumerable<string> languages, bool legacy = false)
    {
        var result = new List<WordColumn>();
        foreach (var code in languages)
        {
            if (code == "ru" && category == "verbs")
            {
                result.Add(new("ru_i", "Russian · Imperfective", "ru"));
                result.Add(new("ru_p", "Russian · Perfective", "ru"));
                if (legacy) result.Add(new("ru_legacy", "Russian · Previous text", "ru"));
            }
            else result.Add(new(code, Languages.Get(code).Name, code));
        }
        return result;
    }
    public static string LanguageOf(string key) => key.StartsWith("ru_", StringComparison.Ordinal) ? "ru" : key;
}
