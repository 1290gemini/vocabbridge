using VocabBridge.Services;

namespace VocabBridge.Views;

public sealed class PlaybackSettingsView : ContentView
{
    private readonly AppServices services;
    private readonly Slider rate = new() { Minimum = 0.5, Maximum = 2, MinimumTrackColor = Ui.Accent, ThumbColor = Ui.Accent };
    private readonly Stepper gap = new() { Minimum = 0, Maximum = 5000, Increment = 250 };
    private readonly Stepper loops = new() { Minimum = 1, Maximum = 20, Increment = 1 };
    private readonly Label rateLabel = Ui.Text("");
    private readonly Label gapLabel = Ui.Text("");
    private readonly Label loopsLabel = Ui.Text("");
    private readonly Label saved = Ui.Text("", 13, Ui.Muted);
    public PlaybackSettingsView(AppServices services)
    {
        this.services = services;
        Content = Ui.Card(Ui.Stack(Ui.Text("Playback settings", 22, bold: true), rateLabel, rate, gapLabel, gap, loopsLabel, loops,
            Ui.Button("Save playback settings", async () =>
            {
                await services.Store.ChangeAsync(d =>
                {
                    d.Settings.Rate = Math.Round(rate.Value, 2); d.Settings.GapMs = (int)gap.Value; d.Settings.Loops = (int)loops.Value;
                });
                saved.Text = "Saved. Used the next time you start playback.";
            }), saved));
        rate.ValueChanged += (_, _) => Labels(); gap.ValueChanged += (_, _) => Labels(); loops.ValueChanged += (_, _) => Labels();
        Reload();
    }
    public void Reload()
    {
        var settings = services.Store.Snapshot().Settings;
        rate.Value = settings.Rate; gap.Value = settings.GapMs; loops.Value = settings.Loops; Labels();
    }
    private void Labels()
    {
        rateLabel.Text = $"Speed: {rate.Value:F2}×"; gapLabel.Text = $"Gap: {gap.Value / 1000:F2} seconds";
        loopsLabel.Text = $"Repeat list: {(int)loops.Value} time(s)"; saved.Text = "";
    }
}
