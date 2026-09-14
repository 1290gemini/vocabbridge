using Microsoft.Maui.Controls.Shapes;

namespace VocabBridge.Views;

internal static class Ui
{
    public static readonly Color Ink = Color.FromArgb("#F3F7EF");
    public static readonly Color Muted = Color.FromArgb("#BED2C9");
    public static readonly Color Paper = Color.FromArgb("#163D38");
    public static readonly Color Surface = Color.FromArgb("#214D45");
    public static readonly Color Accent = Color.FromArgb("#C1E3AB");
    public static Label Text(string text, double size = 16, Color? color = null, bool bold = false) => new()
    {
        Text = text, FontSize = size, TextColor = color ?? Ink,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        LineBreakMode = LineBreakMode.WordWrap
    };
    public static Button Button(string text, Func<Task> action, bool secondary = false)
    {
        var button = new Button { Text = text, BackgroundColor = secondary ? Color.FromArgb("#315E54") : Accent,
            TextColor = secondary ? Ink : Paper, CornerRadius = 14, Padding = new Thickness(14, 10), MinimumHeightRequest = 48 };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await action(); }
            catch (Exception ex)
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page is not null) await page.DisplayAlertAsync("VocabBridge", ex.Message, "OK");
            }
            finally { button.IsEnabled = true; }
        };
        return button;
    }
    public static Border Card(View content) => new()
    {
        Content = content, BackgroundColor = Surface, Stroke = Color.FromArgb("#3C655B"),
        StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = 16
    };
    public static VerticalStackLayout Stack(params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        foreach (var child in children) stack.Add(child);
        return stack;
    }
    public static Grid Row(params View[] children)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        for (var i = 0; i < children.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(children[i], i, 0);
        }
        return grid;
    }
}
