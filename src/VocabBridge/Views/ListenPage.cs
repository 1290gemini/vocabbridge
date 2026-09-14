using VocabBridge.Services;

namespace VocabBridge.Views;

public sealed class ListenPage : ContentPage
{
    private readonly AppServices services;
    private readonly Picker category = new() { Title = "Category", ItemsSource = new[] { "Verbs", "Vocabularies" }, SelectedIndex = 0 };
    private readonly WordTable table = new();
    private readonly Label status = Ui.Text("", 15, Ui.Muted);
    private readonly Button pause;
    private readonly Button start;
    private string Kind => category.SelectedIndex == 0 ? "verbs" : "vocabulary";
    public ListenPage(AppServices services)
    {
        this.services = services; Title = "Listen";
        pause = Ui.Button("Pause", () => { services.Player.TogglePause(); return Task.CompletedTask; }, true);
        start = Ui.Button("Start practice", async () =>
        {
            var data = services.Store.Snapshot(); await services.Player.StartAsync(PracticeQueue.Build(data, Kind), data.Settings);
        });
        var top = Ui.Stack(category, start, Ui.Row(pause, Ui.Button("Stop", services.Player.StopAsync, true)), status);
        var grid = new Grid { Padding = 16, RowSpacing = 16, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        grid.Add(top, 0, 0); grid.Add(new ScrollView { Content = table }, 0, 1); Content = grid;
        category.SelectedIndexChanged += async (_, _) => { await services.Player.StopAsync(); Refresh(); };
        services.DataChanged += Refresh; services.Player.Changed += UpdatePlayer;
    }
    protected override void OnAppearing() { base.OnAppearing(); Refresh(); UpdatePlayer(); }
    private void Refresh()
    {
        var data = services.Store.Snapshot(); var rows = data.Words.Where(w => w.Category == Kind).ToList();
        table.SetData(rows, Kind, data.Settings.SelectedLanguages);
        start.IsEnabled = rows.Count > 0 && data.Settings.SelectedLanguages.Count > 0;
    }
    private void UpdatePlayer()
    {
        status.Text = services.Player.Status; pause.Text = services.Player.IsPaused ? "Resume" : "Pause";
        pause.IsEnabled = services.Player.IsPlaying;
    }
}
