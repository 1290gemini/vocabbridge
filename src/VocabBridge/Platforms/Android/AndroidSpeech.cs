using Android.Content;
using Android.OS;
using Android.Speech.Tts;
using VocabBridge.Services;
using NativeTts = Android.Speech.Tts.TextToSpeech;

namespace VocabBridge.Platforms.Android;

public sealed class AndroidSpeech : IDisposable
{
    private NativeTts? engine;
    private InitListener? initListener;
    private ProgressListener? progressListener;
    private readonly SemaphoreSlim gate = new(1, 1);
    private TaskCompletionSource<bool>? pending;
    private string? pendingId;
    public bool Ready { get; private set; }
    public string EngineName => engine?.DefaultEngine ?? "Android TTS";
    public IReadOnlyList<PhoneVoice> Voices { get; private set; } = [];

    public async Task RefreshAsync()
    {
        await gate.WaitAsync();
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                CloseEngine();
                var initialized = new TaskCompletionSource<OperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                initListener = new InitListener(initialized);
                engine = new NativeTts(global::Android.App.Application.Context, initListener);
                var result = await initialized.Task.WaitAsync(TimeSpan.FromSeconds(12));
                Ready = result == OperationResult.Success;
                if (!Ready) return;
                progressListener = new ProgressListener(this);
                engine.SetOnUtteranceProgressListener(progressListener);
                var nativeVoices = (engine.Voices ?? []).OrderBy(v => v.Name, StringComparer.Ordinal).ToArray();
                Voices = nativeVoices.Select(v =>
                {
                    var siblings = nativeVoices.Where(other => other.Locale?.ToLanguageTag() == v.Locale?.ToLanguageTag() &&
                        !other.IsNetworkConnectionRequired && !(other.Features?.Contains("notInstalled") ?? false)).ToArray();
                    var name = VoiceLabels.Name(v.Locale?.Language ?? "", v.Locale?.ToLanguageTag() ?? "",
                        Math.Max(1, Array.IndexOf(siblings, v) + 1), siblings.Length);
                    return new PhoneVoice(v.Name ?? "", name, v.Locale?.Language ?? "", v.IsNetworkConnectionRequired,
                        !(v.Features?.Contains("notInstalled") ?? false));
                }).ToArray();
            });
        }
        catch { await MainThread.InvokeOnMainThreadAsync(CloseEngine); }
        finally { gate.Release(); }
    }

    public VoiceCheck Check(string language) => VoicePolicy.Check(language, Voices, Ready);

    public async Task SpeakAsync(string text, string language, string? preferredVoice, double rate, CancellationToken cancellation)
    {
        await gate.WaitAsync(cancellation);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            var check = Check(language);
            if (check.State != VoiceState.Ready) throw new InvalidOperationException(check.Message);
            var choice = check.Voices.FirstOrDefault(x => x.Id == preferredVoice) ?? check.Voices[0];
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var id = Guid.NewGuid().ToString("N");
            pending = completion;
            pendingId = id;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                // Re-read native voice metadata immediately before every utterance.
                var voice = engine?.Voices?.FirstOrDefault(v => v.Name == choice.Id &&
                    !v.IsNetworkConnectionRequired && !(v.Features?.Contains("notInstalled") ?? false));
                if (voice is null || (int)engine!.SetVoice(voice) < 0)
                    throw new InvalidOperationException("This offline voice is no longer available. Recheck it in Settings.");
                if (text.Length > NativeTts.MaxSpeechInputLength)
                    throw new InvalidOperationException("This text is too long for the phone's TTS engine.");
                engine.SetSpeechRate((float)Math.Clamp(rate, 0.5, 2));
                using var options = new Bundle();
                if (engine.Speak(text, QueueMode.Flush, options, id) == OperationResult.Error)
                    throw new InvalidOperationException("The phone could not play this voice. Check voice downloads and try again.");
            });
            // Wait for actual native completion, never estimate duration from text length.
            await completion.Task.WaitAsync(TimeSpan.FromMinutes(2), cancellation);
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException("The phone's TTS engine did not finish. Recheck its voice data.");
        }
        finally
        {
            pending = null;
            pendingId = null;
            await MainThread.InvokeOnMainThreadAsync(() => engine?.Stop());
            gate.Release();
        }
    }

    public Task<bool> OpenDownloadsAsync() => MainThread.InvokeOnMainThreadAsync(() =>
    {
        var intent = new Intent("android.speech.tts.engine.INSTALL_TTS_DATA");
        if (!string.IsNullOrWhiteSpace(engine?.DefaultEngine)) intent.SetPackage(engine.DefaultEngine);
        return Launch(intent) || Launch(new Intent("com.android.settings.TTS_SETTINGS")) ||
               Launch(new Intent(global::Android.Provider.Settings.ActionSettings));
    });

    public Task<bool> OpenSettingsAsync() => MainThread.InvokeOnMainThreadAsync(() =>
        Launch(new Intent("com.android.settings.TTS_SETTINGS")) || Launch(new Intent(global::Android.Provider.Settings.ActionSettings)));

    private static bool Launch(Intent intent)
    {
        try
        {
            intent.AddFlags(ActivityFlags.NewTask);
            global::Android.App.Application.Context.StartActivity(intent);
            return true;
        }
        catch (ActivityNotFoundException) { return false; }
        catch (Java.Lang.SecurityException) { return false; }
    }
    private void Finish(string? id, Exception? error = null)
    {
        if (id != pendingId) return;
        if (error is null) pending?.TrySetResult(true); else pending?.TrySetException(error);
    }
    private void CloseEngine()
    {
        Ready = false;
        Voices = [];
        pending?.TrySetCanceled();
        engine?.Stop();
        engine?.Shutdown();
        engine?.Dispose();
        engine = null;
        initListener?.Dispose(); initListener = null;
        progressListener?.Dispose(); progressListener = null;
    }
    public void Dispose() => CloseEngine();

    private sealed class InitListener(TaskCompletionSource<OperationResult> completion) : Java.Lang.Object, NativeTts.IOnInitListener
    {
        public void OnInit(OperationResult status) => completion.TrySetResult(status);
    }
    private sealed class ProgressListener(AndroidSpeech owner) : UtteranceProgressListener
    {
        public override void OnStart(string? utteranceId) { }
        public override void OnDone(string? utteranceId) => owner.Finish(utteranceId);
        [Obsolete("Required legacy Android TTS callback.")]
        public override void OnError(string? utteranceId) => owner.Finish(utteranceId,
            new InvalidOperationException("The phone could not speak. Download or recheck the offline voice in Settings."));
        public override void OnError(string? utteranceId, TextToSpeechError errorCode) => owner.Finish(utteranceId,
            new InvalidOperationException($"Phone TTS error ({errorCode}). Recheck or download the offline voice in Settings."));
        public override void OnStop(string? utteranceId, bool interrupted) => owner.Finish(utteranceId, new System.OperationCanceledException());
    }
}
