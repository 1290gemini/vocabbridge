using System.Text.Json;
using Microsoft.Data.Sqlite;
using VocabBridge.Models;
using VocabBridge.Services;
using VocabBridge.Platforms.Android;

var root = Path.Combine(Path.GetTempPath(), "vocabbridge-checks-" + Guid.NewGuid());
Directory.CreateDirectory(root);
var count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    count++; Console.WriteLine("PASS: " + name);
}
async Task Reject(Func<Task> action, string name)
{
    try { await action(); }
    catch (Exception ex) when (ex is InvalidDataException or JsonException or InvalidOperationException or SqliteException or IOException)
    { Check(true, name); return; }
    throw new Exception("Expected rejection: " + name);
}
SqliteConnection Connect(string path)
{
    var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()); c.Open(); return c;
}
string[] Columns(LocalStore store, string table)
{
    using var c = Connect(store.DatabasePath); using var cmd = c.CreateCommand(); cmd.CommandText = $"PRAGMA table_info({table})";
    using var rows = cmd.ExecuteReader(); var names = new List<string>();
    while (rows.Read()) names.Add(rows.GetString(1)); return names.ToArray();
}
void Execute(LocalStore store, string sql)
{
    using var c = Connect(store.DatabasePath); using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
}
try
{
    var store = new LocalStore(root); await store.LoadAsync();
    Check(store.Snapshot().Settings.SelectedLanguages.Count == 0, "fresh install selects no languages");
    Check(Languages.All.Select(x => x.Code).SequenceEqual(["my", "en", "ru"]), "only Myanmar English Russian offered");
    Check(Columns(store, "Verbs").SequenceEqual(["Id", "SortOrder"]) && Columns(store, "Vocabularies").SequenceEqual(["Id", "SortOrder"]), "two real SQLite tables start without language columns");
    await Reject(() => store.SaveWordAsync(new Word { Texts = new() { ["en"] = "hello" } }), "adding words requires a selected language");
    await store.AddLanguageAsync("my"); await store.AddLanguageAsync("en");
    var vocabulary = new Word { Texts = new() { ["my"] = "စာအုပ်", ["en"] = "book" } };
    var verb = new Word { Category = "verbs", Texts = new() { ["en"] = "read", ["my"] = "ဖတ်သည်" } };
    await store.SaveWordAsync(vocabulary); await store.SaveWordAsync(verb);
    Check(Columns(store, "Verbs").Contains("my") && Columns(store, "Vocabularies").Contains("en"), "language selection adds physical columns to both tables");
    await store.AddLanguageAsync("ru");
    Check(Columns(store, "Verbs").Contains("ru_i") && Columns(store, "Verbs").Contains("ru_p") && !Columns(store, "Verbs").Contains("ru") && Columns(store, "Vocabularies").Contains("ru"), "Russian uses two aspect columns for verbs and one for vocabulary");
    Check(store.Snapshot().Words.First(w => w.Id == vocabulary.Id).Text("en") == "book" && store.Snapshot().Words.First(w => w.Id == verb.Id).Text("ru_i") == "", "third-language addition preserves rows and starts empty");
    verb.Texts["ru_i"] = "читать"; verb.Texts["ru_p"] = "прочитать"; await store.SaveWordAsync(verb);
    vocabulary.Texts["ru"] = "книга"; await store.SaveWordAsync(vocabulary);
    verb.Texts["en"] = "external mutation";
    Check(store.Snapshot().Words.First(w => w.Id == verb.Id).Text("en") == "read", "editor cannot mutate saved data");
    var reopened = new LocalStore(root); await reopened.LoadAsync();
    Check(reopened.Snapshot().Words.First(w => w.Id == verb.Id).Text("ru_p") == "прочитать" && reopened.Snapshot().Words.First(w => w.Id == vocabulary.Id).Text("my") == "စာအုပ်", "both tables and Unicode persist across restart");
    var queue = PracticeQueue.Build(store.Snapshot(), "verbs");
    Check(queue.Select(x => x.Text).SequenceEqual(["ဖတ်သည်", "read", "читать", "прочитать"]), "verb playback follows selected language order and includes both Russian aspects");
    Check(queue.Where(x => x.Text is "читать" or "прочитать").All(x => x.Language == "ru"), "both verb forms use the Russian voice");
    Check(PracticeQueue.Build(store.Snapshot(), "vocabulary").Select(x => x.Text).SequenceEqual(["စာအုပ်", "book", "книга"]), "vocabulary playback excludes verbs");
    var before = store.Export();
    await Reject(() => store.ImportAsync("{}"), "unrelated JSON rejected");
    await Reject(() => store.ImportAsync("{"), "truncated JSON rejected");
    Check(store.Export() == before, "invalid restore leaves data unchanged");
    await Reject(() => store.SaveWordAsync(new Word()), "empty entries rejected");
    await Reject(() => store.SaveWordAsync(new Word { Texts = new() { ["en"] = new string('a', 501) } }), "oversized text rejected");
    await Reject(() => store.AddLanguageAsync("th"), "unoffered language rejected");
    await Reject(() => store.ChangeAsync(d => d.Settings.SelectedLanguages.Remove("my")), "generic settings writes cannot bypass language removal flow");
    var confirmed = false;
    async Task<bool> Remove(RemovalChoice choice, Func<string, Task<bool>> export, bool confirm) => await LanguageRemoval.RunAsync(store, "ru", _ => Task.FromResult(choice), export,
        _ => { confirmed = true; return Task.FromResult(confirm); });
    Check(!await Remove(RemovalChoice.Cancel, _ => throw new Exception("Export must not run"), true), "cancel action sheet aborts removal");
    Check(!await Remove(RemovalChoice.BackUpFirst, _ => Task.FromResult(false), true) && !confirmed, "cancelled backup never reaches delete confirmation");
    await Reject(() => Remove(RemovalChoice.BackUpFirst, _ => throw new IOException("disk full"), true), "failed backup aborts removal");
    Check(store.Export() == before, "all backup cancellation/failure paths preserve Russian data");
    string? exported = null;
    Check(!await Remove(RemovalChoice.BackUpFirst, json => { exported = json; return Task.FromResult(true); }, false), "cancel final confirmation retains language");
    Check(exported is not null && LocalStore.Parse(exported).Words.First(w => w.Id == verb.Id).Text("ru_p") == "прочитать", "pre-deletion backup includes both Russian forms");
    Check(await Remove(RemovalChoice.BackUpFirst, json => { exported = json; return Task.FromResult(true); }, true), "successful backup and confirmation permit removal");
    Check(!Columns(store, "Verbs").Any(x => x.StartsWith("ru")) && !Columns(store, "Vocabularies").Contains("ru"), "Russian columns are physically dropped from both tables");
    Check(store.Snapshot().Words.All(w => !w.Texts.Keys.Any(k => k.StartsWith("ru"))) && store.Snapshot().Words.Count == 2, "removal deletes Russian values but keeps other translations");
    await store.AddLanguageAsync("ru");
    Check(store.Snapshot().Words.All(w => w.Text("ru_i") == "" && w.Text("ru_p") == "" && w.Text("ru") == ""), "re-added language starts empty instead of resurrecting deleted data");
    await store.ImportAsync(exported!);
    Check(store.Snapshot().Words.First(w => w.Id == verb.Id).Text("ru_p") == "прочитать", "backup restore recreates removed schema and values");
    var stale = await store.PrepareRemovalAsync("ru");
    await store.ChangeAsync(d => d.Settings.Rate = 1.2);
    await Reject(() => store.RemoveLanguageAsync(stale), "stale removal plan cannot delete data newer than its backup");
    await Task.WhenAll(Enumerable.Range(0, 8).Select(i => store.SaveWordAsync(new Word { Texts = new() { ["en"] = "word " + i } })));
    Check(store.Snapshot().Words.Count == 10, "concurrent saves retain every entry");
    var onlyRussian = new Word { Texts = new() { ["ru"] = "только" } }; await store.SaveWordAsync(onlyRussian);
    var plan = await store.PrepareRemovalAsync("ru"); Check(plan.EmptyRows == 1, "removal preview counts rows that would become empty");
    await store.RemoveLanguageAsync(plan);
    Check(store.Snapshot().Words.All(w => w.Id != onlyRussian.Id) && store.Snapshot().Words.Count == 10, "only newly empty rows are removed");
    Execute(store, "CREATE TRIGGER fail_insert BEFORE INSERT ON Vocabularies BEGIN SELECT RAISE(ABORT, 'test write failure'); END");
    var preFailure = store.Export();
    await Reject(() => store.AddLanguageAsync("ru"), "SQLite transaction reports interrupted schema/data write");
    Check(!Columns(store, "Verbs").Contains("ru_i") && !Columns(store, "Vocabularies").Contains("ru") && store.Export() == preFailure, "failed write rolls back both tables and selected-language settings");
    Execute(store, "DROP TRIGGER fail_insert");
    await store.ImportAsync(exported!);
    var legacyRoot = Path.Combine(root, "legacy"); Directory.CreateDirectory(legacyRoot);
    var legacyJson = JsonSerializer.Serialize(new { SchemaVersion = 1, Words = new[] { new { Id = Guid.NewGuid(), Category = "verbs", Texts = new Dictionary<string, string> { ["en"] = "read", ["ru"] = "читать" } } }, Settings = new Settings { SelectedLanguages = ["en"] } });
    await File.WriteAllTextAsync(Path.Combine(legacyRoot, "wordbook.json"), legacyJson);
    var legacyStore = new LocalStore(legacyRoot); await legacyStore.LoadAsync();
    Check(legacyStore.Snapshot().Words.Single().Text("ru_legacy") == "читать" && legacyStore.Snapshot().Words.Single().Text("ru_i") == "", "legacy Russian verbs are preserved without guessing aspect");
    Check(legacyStore.Snapshot().Settings.SelectedLanguages.Contains("ru") && await File.ReadAllTextAsync(Path.Combine(legacyRoot, "wordbook.json")) == legacyJson, "migration retains hidden legacy translations and untouched source backup");
    Check(Columns(legacyStore, "Verbs").Contains("ru_legacy"), "migration stores previous Russian text in a real column");
    var previous = legacyStore.Snapshot().Words.Single(); previous.Texts["ru_i"] = previous.Text("ru_legacy"); previous.Texts["ru_legacy"] = "";
    await legacyStore.SaveWordAsync(previous);
    Check(!Columns(legacyStore, "Verbs").Contains("ru_legacy"), "clearing last legacy value removes temporary migration column");
    var malformedRoot = Path.Combine(root, "malformed"); Directory.CreateDirectory(malformedRoot);
    await File.WriteAllTextAsync(Path.Combine(malformedRoot, "wordbook.json"), "broken");
    await Reject(() => new LocalStore(malformedRoot).LoadAsync(), "bad legacy file is not silently reset");
    Check(!File.Exists(Path.Combine(malformedRoot, "wordbook.sqlite3")), "failed migration leaves no empty replacement database");
    var currentExport = store.Export();
    Check(!currentExport.Contains("CreatedAt") && !currentExport.Contains("UpdatedAt") && !currentExport.Contains("Notes"), "no descriptions or timestamps added");
    foreach (var code in store.Snapshot().Settings.SelectedLanguages.ToArray()) await store.RemoveLanguageAsync(await store.PrepareRemovalAsync(code));
    Check(store.Snapshot().Settings.SelectedLanguages.Count == 0 && store.Snapshot().Words.Count == 0, "explicitly removing last language returns to empty setup");

    PhoneVoice[] voices = [new("en-off", "English", "en-US", false, true), new("ru-dl", "Russian", "ru", false, false), new("my-net", "Myanmar", "my", true, true)];
    Check(VoicePolicy.Check("en", voices, true).Voices.Single().Id == "en-off", "installed offline voice accepted");
    Check(VoicePolicy.Check("ru", voices, true).State == VoiceState.DownloadRequired, "missing voice data still prompts download");
    Check(VoicePolicy.Check("my", voices, true).State == VoiceState.Unsupported, "network-only voice never accepted");
    Check(VoicePolicy.Check("en", voices, false).State == VoiceState.EngineUnavailable, "engine failure is distinct");
    Check(VoiceLabels.Name("en", "en-US", 1, 1) == "English (United States)" && VoiceLabels.Name("ru", "ru-RU", 1, 1) == "Russian (Russia)", "voice names use language and country rather than engine codes");
    Check(VoiceLabels.Name("en", "en-US", 2, 3) == "English (United States) · Voice 2", "multiple unnamed voices get honest numbered labels");
    var speech = new AndroidSpeech(); var player = new PracticePlayer(speech);
    await player.StartAsync([new("one", "en"), new("two", "en")], new Settings { GapMs = 0 });
    Check(player.IsPlaying && speech.Texts.SequenceEqual(["one"]), "queue waits for native completion");
    player.TogglePause(); player.TogglePause(); await speech.WaitForCallsAsync(2);
    Check(speech.Texts.SequenceEqual(["one", "one"]), "rapid pause/resume replays current word");
    speech.Complete(); await speech.WaitForCallsAsync(3); Check(speech.Texts.Last() == "two", "queue advances after completion");
    await player.StopAsync(); Check(!player.IsPlaying && !player.IsPaused, "stop resets queue");
    await player.StartAsync([new("loop", "en")], new Settings { GapMs = 0, Loops = 2 });
    speech.Complete(); await speech.WaitForCallsAsync(5); speech.Complete(); await Task.Delay(30);
    Check(!player.IsPlaying && player.Status == "Practice complete.", "repeat count completes exactly");
    Console.WriteLine($"All {count} checks passed.");
}
finally { Directory.Delete(root, recursive: true); }
