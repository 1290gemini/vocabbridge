using VocabBridge.Models;
using VocabBridge.Services;
using TextEntry = Microsoft.Maui.Controls.Entry;

namespace VocabBridge.Views;

public sealed class WordEditorPage : ContentPage
{
    private readonly Dictionary<string, TextEntry> fields = [];
    public WordEditorPage(AppServices services, Word word)
    {
        var selected = services.Store.Snapshot().Settings.SelectedLanguages;
        var editing = services.Store.Snapshot().Words.Any(w => w.Id == word.Id);
        var heading = (editing ? "Edit " : "Add ") + (word.Category == "verbs" ? "verb" : "vocabulary");
        Title = heading; NavigationPage.SetHasNavigationBar(this, false);
        var stack = Ui.Stack(Ui.Text(heading, 24, bold: true));
        var legacy = !string.IsNullOrWhiteSpace(word.Text("ru_legacy"));
        foreach (var column in WordColumns.For(word.Category, selected, legacy))
        {
            var field = new TextEntry { Text = word.Text(column.Key), Placeholder = column.Heading,
                MaxLength = 500, FontSize = 20, IsTextPredictionEnabled = false };
            SemanticProperties.SetDescription(field, column.Heading);
            fields[column.Key] = field;
            stack.Add(Ui.Text(column.Heading, 14, Ui.Muted)); stack.Add(field);
            if (column.Key == "ru_legacy") stack.Add(Ui.Text("Previous text is preserved. Move it to Imperfective or Perfective when you know its aspect, then clear this field.", 13, Ui.Muted));
        }
        stack.Add(Ui.Button("Save", async () =>
        {
            await services.Player.StopAsync();
            await services.Store.SaveWordAsync(new Word { Id = word.Id, Category = word.Category,
                Texts = fields.ToDictionary(x => x.Key, x => x.Value.Text?.Trim() ?? "") });
            services.NotifyDataChanged(); await Navigation.PopModalAsync();
        }));
        stack.Add(Ui.Button("Cancel", async () => { await Navigation.PopModalAsync(); }, true));
        Content = new ScrollView { Content = Ui.Card(stack), Padding = 20 };
    }
}
