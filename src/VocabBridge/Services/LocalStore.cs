using System.Text.Json;
using Microsoft.Data.Sqlite;
using VocabBridge.Models;

namespace VocabBridge.Services;

public sealed record LanguageRemovalPlan(string Code, long Revision, string BackupJson, int AffectedRows, int EmptyRows);

// SQLite commits schema and data together. SQL identifiers come only from WordColumns.
public sealed class LocalStore(string directory)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AppData data = new();
    private long revision;
    private bool loaded;
    public string DatabasePath => Path.Combine(directory, "wordbook.sqlite3");
    public string? RecoveryMessage { get; private set; }
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly string[] Tables = ["Verbs", "Vocabularies"];
    public AppData Snapshot() => Clone(data);
    private static AppData Clone(AppData value) => JsonSerializer.Deserialize<AppData>(JsonSerializer.Serialize(value, Json), Json)!;
    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }
    public async Task LoadAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (loaded) return;
            Directory.CreateDirectory(directory);
            if (File.Exists(DatabasePath))
            {
                using var connection = Open(DatabasePath);
                data = ReadDatabase(connection);
            }
            else
            {
                var oldPath = Path.Combine(directory, "wordbook.json");
                data = File.Exists(oldPath) ? Parse(await File.ReadAllTextAsync(oldPath)) : new AppData();
                // A crash before migration commits leaves the legacy file authoritative.
                var migrationPath = DatabasePath + ".migrating";
                if (File.Exists(migrationPath)) File.Delete(migrationPath);
                using (var connection = Open(migrationPath)) WriteDatabase(connection, data);
                File.Move(migrationPath, DatabasePath);
                if (File.Exists(oldPath))
                    RecoveryMessage = "Your wordbook was moved to the local database. Your original JSON file is retained. Previous Russian verb text is kept separately until you assign its aspect.";
            }
            loaded = true;
        }
        finally { gate.Release(); }
    }
    public Task ChangeAsync(Action<AppData> change) => ChangeCoreAsync(change, false);
    private async Task ChangeCoreAsync(Action<AppData> change, bool allowLanguageChanges, long? expectedRevision = null)
    {
        await LoadAsync();
        await gate.WaitAsync();
        try
        {
            if (expectedRevision is not null && expectedRevision != revision)
                throw new InvalidOperationException("Your wordbook changed while the dialog was open. Try again to back up the latest data.");
            var next = Clone(data);
            change(next);
            if (!allowLanguageChanges && !next.Settings.SelectedLanguages.SequenceEqual(data.Settings.SelectedLanguages))
                throw new InvalidOperationException("Use the language add/remove controls to change database columns.");
            Validate(next);
            using var connection = Open(DatabasePath);
            using (var backup = Open(DatabasePath + ".bak")) connection.BackupDatabase(backup);
            WriteDatabase(connection, next);
            data = next;
            revision++;
        }
        finally { gate.Release(); }
    }
    public Task AddLanguageAsync(string code) => ChangeCoreAsync(next =>
    {
        if (!Languages.Contains(code)) throw new InvalidDataException("Choose Myanmar, English or Russian.");
        if (!next.Settings.SelectedLanguages.Contains(code)) next.Settings.SelectedLanguages.Add(code);
        next.Settings.SetupComplete = true;
    }, true);
    public async Task<LanguageRemovalPlan> PrepareRemovalAsync(string code)
    {
        await LoadAsync();
        await gate.WaitAsync();
        try
        {
            if (!data.Settings.SelectedLanguages.Contains(code)) throw new InvalidOperationException("This language is not selected.");
            return new(code, revision, Export(),
                data.Words.Count(w => w.Texts.Any(x => WordColumns.LanguageOf(x.Key) == code && !string.IsNullOrWhiteSpace(x.Value))),
                data.Words.Count(w => !w.Texts.Any(x => WordColumns.LanguageOf(x.Key) != code && !string.IsNullOrWhiteSpace(x.Value))));
        }
        finally { gate.Release(); }
    }
    public Task RemoveLanguageAsync(LanguageRemovalPlan plan) => ChangeCoreAsync(next =>
    {
        if (!next.Settings.SelectedLanguages.Remove(plan.Code)) throw new InvalidOperationException("This language is not selected.");
        next.Settings.VoiceNames.Remove(plan.Code);
        foreach (var word in next.Words)
            foreach (var key in word.Texts.Keys.Where(k => WordColumns.LanguageOf(k) == plan.Code).ToArray()) word.Texts.Remove(key);
        next.Words.RemoveAll(w => !w.Texts.Values.Any(x => !string.IsNullOrWhiteSpace(x)));
        next.Settings.SetupComplete = next.Settings.SelectedLanguages.Count > 0;
    }, true, plan.Revision);
    public Task SaveWordAsync(Word word) => ChangeAsync(next =>
    {
        if (next.Settings.SelectedLanguages.Count == 0) throw new InvalidDataException("Choose a language in Settings before adding words.");
        var copy = new Word { Id = word.Id, Category = word.Category,
            Texts = word.Texts.ToDictionary(x => x.Key, x => x.Value.Trim()) };
        var index = next.Words.FindIndex(x => x.Id == copy.Id);
        if (index < 0) next.Words.Add(copy); else next.Words[index] = copy;
    });
    public Task DeleteWordAsync(Guid id) => ChangeAsync(next => next.Words.RemoveAll(x => x.Id == id));
    public string Export() => JsonSerializer.Serialize(data, Json);
    public Task ImportAsync(string json)
    {
        var incoming = Parse(json);
        return ChangeCoreAsync(next => { next.Words = incoming.Words; next.Settings = incoming.Settings; }, true);
    }
    public static AppData Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("SchemaVersion", out var version) ||
            !root.TryGetProperty("Words", out _) || !root.TryGetProperty("Settings", out _))
            throw new InvalidDataException("Choose a VocabBridge Android backup file.");
        var value = JsonSerializer.Deserialize<AppData>(json, Json) ?? throw new InvalidDataException("Backup is empty.");
        if (version.GetInt32() == 1)
        {
            if (value.Words is null || value.Settings?.SelectedLanguages is null) throw new InvalidDataException("Invalid legacy backup.");
            foreach (var word in value.Words)
            {
                if (word?.Texts is null) throw new InvalidDataException("Invalid legacy word.");
                foreach (var key in word.Texts.Keys)
                {
                    if (!Languages.Contains(key)) throw new InvalidDataException("This older file contains a language outside Myanmar, English and Russian. The original file has not been changed.");
                    if (!value.Settings.SelectedLanguages.Contains(key) && !string.IsNullOrWhiteSpace(word.Texts[key]))
                        value.Settings.SelectedLanguages.Add(key);
                }
                if (word.Category == "verbs" && word.Texts.Remove("ru", out var previous)) word.Texts["ru_legacy"] = previous;
                foreach (var key in word.Texts.Keys.Where(k => !value.Settings.SelectedLanguages.Contains(WordColumns.LanguageOf(k))).ToArray()) word.Texts.Remove(key);
            }
            value.SchemaVersion = 2;
        }
        Validate(value);
        return value;
    }
    private static void Validate(AppData value)
    {
        if (value.SchemaVersion != 2 || value.Words is null || value.Settings is null) throw new InvalidDataException("Unsupported wordbook format.");
        var s = value.Settings;
        if (s.SelectedLanguages is null || s.SelectedLanguages.Count > 3 ||
            s.SelectedLanguages.Distinct().Count() != s.SelectedLanguages.Count || s.SelectedLanguages.Any(x => !Languages.Contains(x)) ||
            s.VoiceNames is null || !double.IsFinite(s.Rate) || s.Rate is < 0.5 or > 2 || s.GapMs is < 0 or > 5000 || s.Loops is < 1 or > 20)
            throw new InvalidDataException("Invalid language or playback settings.");
        if (value.Words.Count > 100000 || value.Words.Any(w => w is null || w.Id == Guid.Empty ||
            w.Category is not ("vocabulary" or "verbs") || w.Texts is null ||
            w.Texts.Any(x => !WordColumns.For(w.Category, s.SelectedLanguages, true).Any(c => c.Key == x.Key) || x.Value is null || x.Value.Length > 500) ||
            !w.Texts.Values.Any(x => !string.IsNullOrWhiteSpace(x))) || value.Words.Select(w => w.Id).Distinct().Count() != value.Words.Count)
            throw new InvalidDataException("Invalid word entries. Select their languages and enter 1–500 characters in at least one field.");
    }
    private static void Execute(SqliteConnection connection, SqliteTransaction tx, string sql)
    {
        using var command = connection.CreateCommand(); command.Transaction = tx; command.CommandText = sql; command.ExecuteNonQuery();
    }
    private static string[] ColumnNames(SqliteConnection connection, string table, SqliteTransaction? tx = null)
    {
        using var command = connection.CreateCommand(); command.Transaction = tx; command.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = command.ExecuteReader(); var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(1));
        return result.ToArray();
    }
    private static void WriteDatabase(SqliteConnection connection, AppData next)
    {
        using var tx = connection.BeginTransaction();
        Execute(connection, tx, "CREATE TABLE IF NOT EXISTS AppSettings (Id INTEGER PRIMARY KEY CHECK(Id=1), Json TEXT NOT NULL)");
        foreach (var table in Tables)
        {
            var category = table == "Verbs" ? "verbs" : "vocabulary";
            Execute(connection, tx, $"CREATE TABLE IF NOT EXISTS \"{table}\" (Id TEXT PRIMARY KEY, SortOrder INTEGER NOT NULL)");
            var columns = WordColumns.For(category, next.Settings.SelectedLanguages,
                next.Words.Any(w => w.Category == category && !string.IsNullOrEmpty(w.Text("ru_legacy")))).Select(c => c.Key).ToArray();
            var existing = ColumnNames(connection, table, tx).Except(["Id", "SortOrder"]).ToArray();
            foreach (var column in columns.Except(existing)) Execute(connection, tx, $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" TEXT NOT NULL DEFAULT ''");
            foreach (var column in existing.Except(columns))
            {
                if (column is not ("en" or "my" or "ru" or "ru_i" or "ru_p" or "ru_legacy")) throw new InvalidDataException("Unexpected database column.");
                Execute(connection, tx, $"ALTER TABLE \"{table}\" DROP COLUMN \"{column}\"");
            }
            Execute(connection, tx, $"DELETE FROM \"{table}\"");
            for (var i = 0; i < next.Words.Count; i++)
            {
                var word = next.Words[i]; if (word.Category != category) continue;
                using var command = connection.CreateCommand(); command.Transaction = tx;
                var names = new[] { "Id", "SortOrder" }.Concat(columns).ToArray();
                command.CommandText = $"INSERT INTO \"{table}\" ({string.Join(",", names.Select(n => $"\"{n}\""))}) VALUES ({string.Join(",", names.Select((_, n) => "$p" + n))})";
                command.Parameters.AddWithValue("$p0", word.Id.ToString()); command.Parameters.AddWithValue("$p1", i);
                for (var c = 0; c < columns.Length; c++) command.Parameters.AddWithValue("$p" + (c + 2), word.Text(columns[c]));
                command.ExecuteNonQuery();
            }
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = tx; command.CommandText = "INSERT OR REPLACE INTO AppSettings (Id, Json) VALUES (1, $json)";
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(next.Settings, Json)); command.ExecuteNonQuery();
        }
        Execute(connection, tx, "PRAGMA user_version=2"); tx.Commit();
    }
    private static AppData ReadDatabase(SqliteConnection connection)
    {
        using var version = connection.CreateCommand(); version.CommandText = "PRAGMA user_version";
        if (Convert.ToInt32(version.ExecuteScalar()) != 2) throw new InvalidDataException("Unsupported database version. Your data has not been changed.");
        using var settings = connection.CreateCommand(); settings.CommandText = "SELECT Json FROM AppSettings WHERE Id=1";
        var result = new AppData { Settings = JsonSerializer.Deserialize<Settings>((string?)settings.ExecuteScalar() ?? throw new InvalidDataException("Missing settings."), Json)! };
        var rows = new List<(int Order, Word Word)>();
        foreach (var table in Tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = $"SELECT * FROM \"{table}\"";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var word = new Word { Id = Guid.Parse(reader.GetString(0)), Category = table == "Verbs" ? "verbs" : "vocabulary" };
                for (var i = 2; i < reader.FieldCount; i++) word.Texts[reader.GetName(i)] = reader.GetString(i);
                rows.Add((reader.GetInt32(1), word));
            }
        }
        result.Words = rows.OrderBy(x => x.Order).Select(x => x.Word).ToList();
        Validate(result); return result;
    }
}
