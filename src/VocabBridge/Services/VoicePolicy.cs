namespace VocabBridge.Services;

public sealed record PhoneVoice(string Id, string Name, string Language, bool NetworkRequired, bool Installed);
public enum VoiceState { Ready, DownloadRequired, Unsupported, EngineUnavailable }
public sealed record VoiceCheck(VoiceState State, IReadOnlyList<PhoneVoice> Voices, string Message);

public static class VoicePolicy
{
    // Android locales usually use ISO-639-1; normalize common ISO-639-2 aliases too.
    public static string Normalize(string value)
    {
        var code = value.Replace('_', '-').Split('-')[0].ToLowerInvariant();
        return code switch
        {
            "eng" => "en", "mya" or "bur" => "my", "rus" => "ru", "tha" => "th",
            "jpn" => "ja", "kor" => "ko", "zho" or "chi" => "zh", "spa" => "es",
            "fra" or "fre" => "fr", "deu" or "ger" => "de", "ita" => "it", "por" => "pt",
            "hin" => "hi", "ara" => "ar", "vie" => "vi", _ => code
        };
    }
    public static VoiceCheck Check(string language, IEnumerable<PhoneVoice> all, bool engineReady)
    {
        if (!engineReady) return new(VoiceState.EngineUnavailable, [], "No working phone TTS engine. Open Android TTS settings.");
        var matching = all.Where(v => Normalize(v.Language) == Normalize(language)).ToList();
        var ready = matching.Where(v => !v.NetworkRequired && v.Installed).ToList();
        if (ready.Count > 0) return new(VoiceState.Ready, ready, $"{ready.Count} offline voice(s) available on this phone.");
        if (matching.Any(v => !v.NetworkRequired && !v.Installed))
            return new(VoiceState.DownloadRequired, [], "Download the offline voice data, then return and recheck.");
        return new(VoiceState.Unsupported, [], matching.Count > 0
            ? "This engine offers only online voices for this language. Check for an offline voice or choose another engine."
            : "No offline voice is listed for this language. Check voice downloads or choose another TTS engine.");
    }
}
