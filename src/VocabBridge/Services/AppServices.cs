using VocabBridge.Platforms.Android;

namespace VocabBridge.Services;

public sealed class AppServices
{
    public LocalStore Store { get; } = new(FileSystem.AppDataDirectory);
    public AndroidSpeech Speech { get; } = new();
    public PracticePlayer Player { get; }
    public event Action? DataChanged;
    public event Action? VoicesChanged;
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    public AppServices() => Player = new(Speech);
    public void NotifyDataChanged() => DataChanged?.Invoke();
    public async Task RefreshVoicesAsync()
    {
        await refreshGate.WaitAsync();
        try
        {
            await Player.StopAsync();
            await Speech.RefreshAsync();
            VoicesChanged?.Invoke();
        }
        finally { refreshGate.Release(); }
    }
}
