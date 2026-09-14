using VocabBridge.Models;
using VocabBridge.Platforms.Android;

namespace VocabBridge.Services;

public sealed record SpeechItem(string Text, string Language);

public sealed class PracticePlayer(AndroidSpeech speech)
{
    private CancellationTokenSource? session;
    private CancellationTokenSource? utterance;
    private bool paused;
    private TaskCompletionSource<bool>? resume;
    private Task running = Task.CompletedTask;
    private readonly SemaphoreSlim commands = new(1, 1);
    public bool IsPlaying => session is not null;
    public bool IsPaused => paused;
    public string Status { get; private set; } = "Choose your words and start practice.";
    public event Action? Changed;
    public event Action<string>? Failed;
    private void Notify(string status) { Status = status; MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke()); }

    public async Task StartAsync(IReadOnlyList<SpeechItem> items, Settings settings)
    {
        await commands.WaitAsync();
        try
        {
            await StopCoreAsync();
            if (items.Count == 0) { Notify("No words in this selection. Add words first."); return; }
            foreach (var language in items.Select(x => x.Language).Distinct())
            {
                var check = speech.Check(language);
                if (check.State != VoiceState.Ready)
                    throw new InvalidOperationException($"{Languages.Get(language).Name}: {check.Message}");
            }
            session = new CancellationTokenSource();
            running = RunAsync(items.ToArray(), settings, session);
        }
        finally { commands.Release(); }
    }
    public async Task StopAsync()
    {
        await commands.WaitAsync();
        try { await StopCoreAsync(); }
        finally { commands.Release(); }
    }
    private async Task StopCoreAsync()
    {
        session?.Cancel();
        resume?.TrySetResult(true);
        await running;
    }
    public void TogglePause()
    {
        if (session is null) return;
        paused = !paused;
        if (paused)
        {
            resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
            utterance?.Cancel();
            Notify("Paused. Resume replays the current word.");
        }
        else { resume?.TrySetResult(true); Notify("Resuming…"); }
    }
    private async Task RunAsync(IReadOnlyList<SpeechItem> items, Settings settings, CancellationTokenSource owner)
    {
        var token = owner.Token;
        try
        {
            for (var loop = 0; loop < settings.Loops; loop++)
            {
                var index = 0;
                while (index < items.Count)
                {
                    token.ThrowIfCancellationRequested();
                    if (paused && resume is not null) await resume.Task.WaitAsync(token);
                    token.ThrowIfCancellationRequested();
                    var item = items[index];
                    using var current = CancellationTokenSource.CreateLinkedTokenSource(token);
                    utterance = current;
                    Notify($"{loop + 1}/{settings.Loops} · {index + 1}/{items.Count} · {Languages.Get(item.Language).Name}\n{item.Text}");
                    try
                    {
                        await speech.SpeakAsync(item.Text, item.Language, settings.VoiceNames.GetValueOrDefault(item.Language), settings.Rate, current.Token);
                        await Task.Delay(settings.GapMs, current.Token);
                        index++;
                    }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested && current.IsCancellationRequested)
                    { /* Pause interrupted this word: replay it after resume, including rapid pause/resume. */ }
                    finally { utterance = null; }
                }
            }
            Notify("Practice complete.");
        }
        catch (OperationCanceledException) { Notify("Playback stopped."); }
        catch (Exception ex)
        {
            Notify(ex.Message);
            MainThread.BeginInvokeOnMainThread(() => Failed?.Invoke(ex.Message));
        }
        finally
        {
            session = null; paused = false; resume = null; owner.Dispose();
            MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke());
        }
    }
}
