// Only the Android transport and UI dispatcher are faked. Production player logic is linked unchanged.
public static class MainThread
{
    public static void BeginInvokeOnMainThread(Action action) => action();
}
namespace VocabBridge.Platforms.Android
{
    public sealed class AndroidSpeech
    {
        public List<string> Texts { get; } = [];
        private TaskCompletionSource<bool>? completion;
        public VocabBridge.Services.VoiceCheck Check(string language) => new(VocabBridge.Services.VoiceState.Ready, [], "ready");
        public Task SpeakAsync(string text, string language, string? voice, double rate, CancellationToken token)
        {
            Texts.Add(text); completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return completion.Task.WaitAsync(token);
        }
        public void Complete() => completion!.TrySetResult(true);
        public async Task WaitForCallsAsync(int count)
        {
            var timeout = DateTime.UtcNow.AddSeconds(3);
            while (Texts.Count < count)
            {
                if (DateTime.UtcNow > timeout) throw new TimeoutException("Player did not progress.");
                await Task.Delay(5);
            }
        }
    }
}
