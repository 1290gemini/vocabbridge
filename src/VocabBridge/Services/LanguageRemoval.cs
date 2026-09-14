namespace VocabBridge.Services;

public enum RemovalChoice { Cancel, BackUpFirst, DeleteWithoutBackup }

public static class LanguageRemoval
{
    public static async Task<bool> RunAsync(LocalStore store, string language,
        Func<LanguageRemovalPlan, Task<RemovalChoice>> choose,
        Func<string, Task<bool>> export, Func<LanguageRemovalPlan, Task<bool>> confirm)
    {
        var plan = await store.PrepareRemovalAsync(language);
        var choice = await choose(plan);
        if (choice == RemovalChoice.Cancel) return false;
        if (choice == RemovalChoice.BackUpFirst && !await export(plan.BackupJson)) return false;
        if (!await confirm(plan)) return false;
        await store.RemoveLanguageAsync(plan);
        return true;
    }
}
