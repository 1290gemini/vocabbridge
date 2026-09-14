using VocabBridge.Models;
using VocabBridge.Services;

namespace VocabBridge.Views;

public sealed class WordbookPage : ContentPage
{
    private readonly AppServices services;
    private readonly SearchBar search = new() { Placeholder = "Search words…", TextColor = Ui.Ink, PlaceholderColor = Ui.Muted, BackgroundColor = Ui.Surface };
    private readonly WordTable verbs;
    private readonly WordTable vocabulary;
    private readonly Button addVerb;
    private readonly Button addVocabulary;
    public WordbookPage(AppServices services)
    {
        this.services = services; Title = "Wordbook";
        verbs = new(EditAsync, DeleteAsync); vocabulary = new(EditAsync, DeleteAsync);
        addVerb = Ui.Button("+ Add verb", () => EditAsync(new Word { Category = "verbs" }));
        addVocabulary = Ui.Button("+ Add vocabulary", () => EditAsync(new Word { Category = "vocabulary" }));
        var content = Ui.Stack(Ui.Text("Verbs", 23, bold: true), addVerb, verbs,
            new BoxView { HeightRequest = 8, Color = Colors.Transparent }, Ui.Text("Vocabularies", 23, bold: true), addVocabulary, vocabulary);
        var layout = new Grid { Padding = 16, RowSpacing = 12, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        layout.Add(search, 0, 0); layout.Add(new ScrollView { Content = content }, 0, 1); Content = layout;
        search.TextChanged += (_, _) => Refresh(); services.DataChanged += Refresh;
    }
    protected override void OnAppearing() { base.OnAppearing(); Refresh(); }
    private void Refresh()
    {
        var data = services.Store.Snapshot(); var query = search.Text?.Trim() ?? "";
        var rows = data.Words.Where(w => w.Texts.Values.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        verbs.SetData(rows.Where(w => w.Category == "verbs"), "verbs", data.Settings.SelectedLanguages);
        vocabulary.SetData(rows.Where(w => w.Category == "vocabulary"), "vocabulary", data.Settings.SelectedLanguages);
        addVerb.IsEnabled = addVocabulary.IsEnabled = data.Settings.SelectedLanguages.Count > 0;
    }
    private Task EditAsync(Word word) => Navigation.PushModalAsync(new WordEditorPage(services, word));
    private async Task DeleteAsync(Word word)
    {
        if (!await DisplayAlertAsync("Delete entry?", "Remove this row from your phone?", "Delete", "Cancel")) return;
        await services.Player.StopAsync(); await services.Store.DeleteWordAsync(word.Id); services.NotifyDataChanged();
    }
}
