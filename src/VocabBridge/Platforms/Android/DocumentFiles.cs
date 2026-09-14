using Android.App;
using Android.Content;
using System.Text;

namespace VocabBridge.Platforms.Android;

// Storage Access Framework: user chooses a local destination; no storage permission.
public static class DocumentFiles
{
    public const int RequestCode = 7142;
    private static TaskCompletionSource<global::Android.Net.Uri?>? pending;
    private static async Task<global::Android.Net.Uri?> ChooseAsync(bool write)
    {
        if (pending is not null) throw new InvalidOperationException("A file picker is already open.");
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No Android activity is available.");
        var completion = new TaskCompletionSource<global::Android.Net.Uri?>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending = completion;
        try
        {
            var intent = new Intent(write ? Intent.ActionCreateDocument : Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(write ? "application/json" : "*/*");
            intent.PutExtra(Intent.ExtraLocalOnly, true);
            if (write) intent.PutExtra(Intent.ExtraTitle, "VocabBridge-backup.json");
            activity.StartActivityForResult(intent, RequestCode);
            return await completion.Task;
        }
        catch (ActivityNotFoundException) { throw new InvalidOperationException("This phone has no file picker available."); }
        finally { pending = null; }
    }
    public static void OnResult(Result result, Intent? data) => pending?.TrySetResult(result == Result.Ok ? data?.Data : null);
    public static async Task<bool> ExportAsync(string json)
    {
        var uri = await ChooseAsync(true);
        if (uri is null) return false;
        using var stream = global::Android.App.Application.Context.ContentResolver!.OpenOutputStream(uri, "wt")
            ?? throw new IOException("Could not write this location.");
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
        return true;
    }
    public static async Task<string?> ImportAsync()
    {
        var uri = await ChooseAsync(false);
        if (uri is null) return null;
        using var stream = global::Android.App.Application.Context.ContentResolver!.OpenInputStream(uri)
            ?? throw new IOException("Could not read this file.");
        using var result = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            if (result.Length + read > 32 * 1024 * 1024) throw new InvalidDataException("Choose a backup smaller than 32 MB.");
            await result.WriteAsync(buffer.AsMemory(0, read));
        }
        return Encoding.UTF8.GetString(result.ToArray());
    }
}
