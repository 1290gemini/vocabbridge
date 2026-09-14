using VocabBridge.Models;

namespace VocabBridge.Views;

// One horizontally scrollable grid keeps headers and cells aligned. Pagination bounds native view count.
public sealed class WordTable : ContentView
{
    private const int PageSize = 20;
    private List<Word> words = [];
    private IReadOnlyList<WordColumn> columns = [];
    private int page;
    private readonly Func<Word, Task>? edit;
    private readonly Func<Word, Task>? delete;
    public WordTable(Func<Word, Task>? edit = null, Func<Word, Task>? delete = null)
    { this.edit = edit; this.delete = delete; }
    public void SetData(IEnumerable<Word> source, string category, IReadOnlyList<string> languages)
    {
        words = source.ToList();
        columns = WordColumns.For(category, languages, words.Any(w => !string.IsNullOrWhiteSpace(w.Text("ru_legacy"))));
        page = Math.Min(page, Math.Max(0, (words.Count - 1) / PageSize));
        Render();
    }
    private void Render()
    {
        if (columns.Count == 0) { Content = Ui.Card(Ui.Text("Choose your languages in Settings.", 15, Ui.Muted)); return; }
        var grid = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = Color.FromArgb("#40685D") };
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(44)));
        foreach (var column in columns) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(column.Language == "ru" && columns.Any(c => c.Key == "ru_i") ? 180 : 150)));
        if (edit is not null) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));
        if (delete is not null) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(80)));
        var headers = new[] { "#" }.Concat(columns.Select(c => c.Heading)).ToList();
        if (edit is not null) headers.Add("Edit"); if (delete is not null) headers.Add("Delete");
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < headers.Count; c++) grid.Add(Cell(headers[c], true), c, 0);
        var slice = words.Skip(page * PageSize).Take(PageSize).ToList();
        for (var r = 0; r < slice.Count; r++)
        {
            var word = slice[r]; grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(Cell((page * PageSize + r + 1).ToString()), 0, r + 1);
            for (var c = 0; c < columns.Count; c++) grid.Add(Cell(string.IsNullOrWhiteSpace(word.Text(columns[c].Key)) ? "—" : word.Text(columns[c].Key)), c + 1, r + 1);
            var actionColumn = columns.Count + 1;
            if (edit is not null) grid.Add(ActionCell("Edit", () => edit(word)), actionColumn++, r + 1);
            if (delete is not null) grid.Add(ActionCell("Delete", () => delete(word)), actionColumn, r + 1);
        }
        var content = Ui.Stack(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = grid, HorizontalScrollBarVisibility = ScrollBarVisibility.Always });
        if (words.Count == 0) content.Add(Ui.Text("No entries yet.", 14, Ui.Muted));
        else content.Add(Ui.Text($"{page * PageSize + 1}–{page * PageSize + slice.Count} of {words.Count}", 12, Ui.Muted));
        if (words.Count > PageSize)
        {
            var previous = Ui.Button("Previous", () => { page--; Render(); return Task.CompletedTask; }, true);
            var next = Ui.Button("Next", () => { page++; Render(); return Task.CompletedTask; }, true);
            previous.IsEnabled = page > 0; next.IsEnabled = (page + 1) * PageSize < words.Count;
            content.Add(Ui.Row(previous, next));
        }
        Content = content;
    }
    private static View Cell(string value, bool header = false) => new Border
    {
        BackgroundColor = header ? Color.FromArgb("#315F53") : Ui.Surface,
        StrokeThickness = 0, Padding = new Thickness(10, 12), MinimumHeightRequest = 52,
        Content = Ui.Text(value, header ? 13 : 16, header ? Ui.Accent : Ui.Ink, header)
    };
    private static View ActionCell(string title, Func<Task> action)
    {
        var button = Ui.Button(title, action, true); button.FontSize = 12; button.Padding = new Thickness(5, 8);
        return new Border { BackgroundColor = Ui.Surface, StrokeThickness = 0, Padding = 4, Content = button };
    }
}
