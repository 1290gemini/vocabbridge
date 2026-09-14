using VocabBridge.Services;
using VocabBridge.Views;

namespace VocabBridge;

public sealed class App : Application
{
    private readonly AppServices services = new();
    public App()
    {
        UserAppTheme = AppTheme.Dark;
        services.Player.Failed += async message =>
        {
            if (Windows.FirstOrDefault()?.Page is { } page)
                await page.DisplayAlertAsync("Phone voice", message, "OK");
        };
        Resources = new ResourceDictionary();
        Resources.Add(new Style(typeof(ContentPage)) { Setters = {
            new Setter { Property = ContentPage.BackgroundColorProperty, Value = Ui.Paper } } });
        Resources.Add(new Style(typeof(Label)) { Setters = {
            new Setter { Property = Label.TextColorProperty, Value = Ui.Ink } } });
        Resources.Add(new Style(typeof(Entry)) { Setters = {
            new Setter { Property = Entry.TextColorProperty, Value = Ui.Ink },
            new Setter { Property = Entry.PlaceholderColorProperty, Value = Ui.Muted } } });
        Resources.Add(new Style(typeof(Picker)) { Setters = {
            new Setter { Property = Picker.TextColorProperty, Value = Ui.Ink },
            new Setter { Property = Picker.TitleColorProperty, Value = Ui.Muted } } });
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loading = new ContentPage { Content = new VerticalStackLayout {
            Padding = 28, VerticalOptions = LayoutOptions.Center,
            Children = { Ui.Text("Opening your wordbook…", 20) } } };
        var window = new Window(loading);
        bool started = false, ready = false;
        loading.Appearing += async (_, _) =>
        {
            if (started) return;
            started = true;
            try
            {
                await services.Store.LoadAsync();
                await services.RefreshVoicesAsync();
                var tabs = new TabbedPage { BarBackgroundColor = Ui.Paper, SelectedTabColor = Ui.Accent,
                    UnselectedTabColor = Ui.Muted };
                Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(tabs,
                    Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Top);
                var words = new WordbookPage(services);
                var listen = new ListenPage(services);
                var settings = new SettingsPage(services);
                tabs.Children.Add(words); tabs.Children.Add(listen); tabs.Children.Add(settings);
                tabs.CurrentPage = services.Store.Snapshot().Settings.SelectedLanguages.Count > 0 ? words : settings;
                window.Page = tabs;
                ready = true;
                if (services.Store.RecoveryMessage is { } message)
                    await tabs.DisplayAlertAsync("Wordbook updated", message, "OK");
            }
            catch (Exception ex)
            {
                started = false;
                loading.Content = Ui.Stack(Ui.Text("Could not open your wordbook", 24), Ui.Text(ex.Message),
                    Ui.Button("Close app", () => { Quit(); return Task.CompletedTask; }));
                loading.Padding = 24;
            }
        };
        window.Stopped += async (_, _) => { await services.Player.StopAsync(); };
        window.Resumed += async (_, _) =>
        {
            if (ready) await services.RefreshVoicesAsync();
        };
        window.Destroying += async (_, _) => { await services.Player.StopAsync(); services.Speech.Dispose(); };
        return window;
    }
}
