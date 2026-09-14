using System.Globalization;
using VocabBridge.Models;

namespace VocabBridge.Services;

public static class VoiceLabels
{
    public static string Name(string language, string locale, int number, int count)
    {
        string label;
        try { label = CultureInfo.GetCultureInfo(locale.Replace('_', '-')).EnglishName; }
        catch (CultureNotFoundException) { label = Languages.All.FirstOrDefault(l => l.Code == VoicePolicy.Normalize(language))?.Name ?? "Phone voice"; }
        return count > 1 ? $"{label} · Voice {number}" : label;
    }
}
