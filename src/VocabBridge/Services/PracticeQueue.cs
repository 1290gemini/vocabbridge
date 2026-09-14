using VocabBridge.Models;

namespace VocabBridge.Services;

public static class PracticeQueue
{
    public static IReadOnlyList<SpeechItem> Build(AppData data, string category) => data.Words
        .Where(w => w.Category == category)
        .SelectMany(w => WordColumns.For(category, data.Settings.SelectedLanguages, true)
            .Where(c => !string.IsNullOrWhiteSpace(w.Text(c.Key)))
            .Select(c => new SpeechItem(w.Text(c.Key), c.Language))).ToList();
}
