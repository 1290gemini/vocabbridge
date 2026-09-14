using VocabBridge.Models;
using VocabBridge.Services;
using VocabBridge.Platforms.Android;

namespace VocabBridge.Views;

public sealed class SettingsPage : ContentPage
{
    private readonly AppServices services;
    private readonly VerticalStackLayout choices = new() { Spacing = 12 };
    private readonly PlaybackSettingsView playback;
    private bool rendering;
    private bool changing;
    public SettingsPage(AppServices services)
    {
        this.services = services; Title = "Settings";
        playback = new(services);
        var content = Ui.Stack(Ui.Text("Learning languages", 23, bold: true),
            Ui.Text("Select the languages you want to save and listen to.", 14, Ui.Muted), choices,
            Ui.Button("Recheck phone voices", services.RefreshVoicesAsync, true),
            Ui.Button("Phone voice settings", async () =>
            {
                await services.Player.StopAsync();
                if (!await services.Speech.OpenSettingsAsync()) throw new InvalidOperationException("Open Android Settings and search for Text-to-speech output.");
            }, true), playback,
            Ui.Text("Backup", 23, bold: true),
            Ui.Text("Export your wordbook before uninstalling or clearing app data.", 14, Ui.Muted),
            Ui.Button("Export backup to phone", async () =>
            {
                await services.Player.StopAsync();
                if (await DocumentFiles.ExportAsync(services.Store.Export())) await DisplayAlertAsync("Backup saved", "Your wordbook was saved to the file you chose.", "OK");
            }, true),
            Ui.Button("Restore a backup", RestoreAsync, true));
        Content = new ScrollView { Content = content, Padding = 16 };
        services.VoicesChanged += Render; services.DataChanged += Render;
    }
    protected override void OnAppearing() { base.OnAppearing(); Render(); }
    private async Task RestoreAsync()
    {
        await services.Player.StopAsync();
        var json = await DocumentFiles.ImportAsync(); if (json is null) return;
        var incoming = LocalStore.Parse(json);
        if (!await DisplayAlertAsync("Replace this wordbook?", $"Restore {incoming.Words.Count} entries and their language columns? This replaces your current wordbook. Export it first if you want to keep it.", "Restore", "Cancel")) return;
        await services.Store.ImportAsync(json); services.NotifyDataChanged(); playback.Reload(); await services.RefreshVoicesAsync();
        await DisplayAlertAsync("Restored", "Your wordbook is ready. Check the phone voices before listening.", "OK");
    }
    private void Render()
    {
        rendering = true;
        try
        {
            choices.Clear(); var settings = services.Store.Snapshot().Settings;
            foreach (var language in Languages.All)
            {
                var enabled = settings.SelectedLanguages.Contains(language.Code);
                var toggle = new Switch { IsToggled = enabled, OnColor = Ui.Accent };
                SemanticProperties.SetDescription(toggle, $"Learn {language.Name}");
                var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
                row.Add(Ui.Text(language.Name, 20, bold: true), 0, 0); row.Add(toggle, 1, 0);
                var block = Ui.Stack(row);
                if (enabled)
                {
                    var check = services.Speech.Check(language.Code);
                    block.Add(Ui.Text(check.State switch
                    {
                        VoiceState.Ready => "Available on this phone",
                        VoiceState.DownloadRequired => "Voice download needed",
                        VoiceState.Unsupported => "No offline voice available in this engine",
                        _ => "Set up a phone TTS engine"
                    }, 14, check.State == VoiceState.Ready ? Ui.Accent : Color.FromArgb("#F4C795")));
                    if (check.State == VoiceState.Ready)
                    {
                        var voices = check.Voices.ToList();
                        var picker = new Picker { Title = "Voice", ItemsSource = voices, ItemDisplayBinding = new Binding(nameof(PhoneVoice.Name)) };
                        picker.SelectedItem = voices.FirstOrDefault(v => v.Id == settings.VoiceNames.GetValueOrDefault(language.Code)) ?? voices[0];
                        picker.SelectedIndexChanged += async (_, _) =>
                        {
                            if (rendering || picker.SelectedItem is not PhoneVoice voice) return;
                            try { await services.Store.ChangeAsync(d => d.Settings.VoiceNames[language.Code] = voice.Id); }
                            catch (Exception ex) { await DisplayAlertAsync("Voice setting", ex.Message, "OK"); Render(); }
                        };
                        block.Add(picker);
                        block.Add(Ui.Button("Test voice", async () =>
                        {
                            var settings = services.Store.Snapshot().Settings; settings.Loops = 1; settings.GapMs = 0;
                            await services.Player.StartAsync([new(language.Sample, language.Code)], settings);
                        }, true));
                    }
                    else block.Add(Ui.Button("Download voice data", () => DownloadAsync(language), true));
                }
                toggle.Toggled += async (_, args) =>
                {
                    if (rendering || changing) return;
                    changing = true; Content.IsEnabled = false;
                    try
                    {
                        await services.Player.StopAsync();
                        if (args.Value)
                        {
                            await services.Store.AddLanguageAsync(language.Code); services.NotifyDataChanged();
                            await services.RefreshVoicesAsync();
                            if (services.Speech.Check(language.Code).State != VoiceState.Ready &&
                                await DisplayAlertAsync($"{language.Name} voice", "An offline voice is not ready. Open voice downloads? You can still save words without a voice.", "Open downloads", "Later"))
                                await DownloadAsync(language);
                        }
                        else
                        {
                            var removed = await LanguageRemoval.RunAsync(services.Store, language.Code, async plan =>
                            {
                                var choice = await DisplayActionSheetAsync($"Remove {language.Name}? Its columns and saved translations will be deleted. Back up first?", "Cancel", null,
                                    "Back up first", "Delete without backup");
                                return choice switch { "Back up first" => RemovalChoice.BackUpFirst, "Delete without backup" => RemovalChoice.DeleteWithoutBackup, _ => RemovalChoice.Cancel };
                            }, DocumentFiles.ExportAsync, async plan =>
                            {
                                var columns = language.Code == "ru" ? "Russian vocabulary and both Russian verb forms" : language.Name + " columns";
                                return await DisplayAlertAsync("Confirm language deletion", $"Delete {columns} from both tables?\n\n{plan.AffectedRows} rows contain this language. {plan.EmptyRows} rows would become empty and will also be removed.", "Delete language", "Cancel");
                            });
                            if (removed) services.NotifyDataChanged();
                        }
                    }
                    catch (Exception ex) { await DisplayAlertAsync("Language setting", ex.Message, "OK"); }
                    finally { changing = false; Content.IsEnabled = true; Render(); }
                };
                choices.Add(Ui.Card(block));
            }
        }
        finally { rendering = false; }
    }
    private async Task DownloadAsync(Language language)
    {
        await services.Player.StopAsync();
        await DisplayAlertAsync("Voice download", $"Choose {language.Name} in the phone's TTS voice-data screen and download it if offered. Return here to recheck. If this engine does not support the language, choose another TTS engine. Downloads may need internet.", "Open");
        if (!await services.Speech.OpenDownloadsAsync()) throw new InvalidOperationException("Open Android Settings and search for Text-to-speech output.");
    }
}
