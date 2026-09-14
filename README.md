# VocabBridge Android 1.1

Native .NET MAUI Android app. Read **README_MY.md** for Myanmar instructions.

## This update

- Top menu: **Wordbook / Listen / Settings**. No app navigation title bar or in-page logo. The page background is the previous title-bar green, `#163D38`.
- Wordbook displays separate **Verbs** and **Vocabularies** tables, with selected-language columns, search and add/edit/delete. There are no per-row Play buttons.
- Listen displays the selected category's table and speaks every selected language in sequence. Playback speed, gap and repetition controls are in Settings. There is no language picker on Listen.
- Settings offers only **Myanmar, English and Russian**. A new installation selects none. Existing language choices survive an update or backup restore.
- Russian verbs have separate **Imperfective** and **Perfective** fields. Either can be left blank. Vocabulary has one Russian field.
- Voice labels use names such as **English (United States)** and **Russian (Russia)**. Where the engine has several unnamed voices in one locale, numbered labels distinguish them. Internal IDs are retained only for matching; no invented personal names or genders.
- The VB/VocabBridge artwork is explicitly assigned as the Android launcher icon and round icon. It is not rendered within app pages. Android's system splash can still display the app icon.

## Local SQLite storage

`FileSystem.AppDataDirectory/wordbook.sqlite3` contains two word tables and an AppSettings table. Data remains on the phone. No server, account, login, cloud sync or internet permission is required. No descriptions or created/updated timestamps are stored.

| Language selected | Vocabularies columns | Verbs columns |
|---|---|---|
| Myanmar | `my` | `my` |
| English | `en` | `en` |
| Russian | `ru` | `ru_i`, `ru_p` |

`Id` and `SortOrder` maintain identity and row order. Adding a language runs `ALTER TABLE ADD COLUMN`; existing rows start blank in that language. Removing it drops its actual columns and all their values from both tables. Russian removal drops both aspect fields and any migrated previous Russian text. Newly empty rows are also removed, with their count disclosed before confirmation.

Removal flow: **Back up first / Delete without backup / Cancel**, followed by a final confirmation. A cancelled or failed export prevents deletion. If the wordbook changes while a dialog is open, the old removal plan is rejected so deletion cannot use a stale backup. Schema and row changes are committed in a single SQLite transaction. A failure rolls back both tables and language settings.

Before changes, the previous database state is copied to `wordbook.sqlite3.bak` in private storage. Explicit portable backups are JSON files exported through Android's local document picker. Uninstalling or clearing app data removes private data: export to a local folder first. JSON restore replaces the current wordbook after validation and user confirmation. Removing and then re-adding a language does not restore its deleted words; restore an exported backup to recover them.

## Updating from 1.0

The previous `wordbook.json` is migrated automatically when installing an update with a compatible signing key. Its original bytes are retained. Version-1 JSON exports are also accepted by Settings → Restore a backup. Previously hidden nonempty translations in the three supported languages are retained.

Old Russian verbs have no aspect classification. They migrate into `ru_legacy`, displayed as **Russian · Previous text**. Edit these rows, place the text in the correct aspect field, then clear Previous text. The extra column disappears after the last such value is cleared. The app never guesses an aspect or discards the old string.

An old file containing languages outside Myanmar/English/Russian is rejected with a message, leaving it unchanged; it is not silently truncated. Back up that older wordbook before changing versions.

The supplied APK is a new **debug-signed test build**. A different signing identity from an installed copy causes `INSTALL_FAILED_UPDATE_INCOMPATIBLE`. Export from the old app before uninstalling it, install the new APK, then restore the JSON backup. For ongoing development, build all updates on your own machine with the same signing key. Use a private stable release key for distribution.

## Build and install

Requires .NET SDK 10, MAUI Android workload, Android SDK 36 and JDK 21. Defaults target ARM64 Android 6.0+ (API 23).

```bash
dotnet workload install maui-android --version 10.0.100
dotnet build src/VocabBridge/VocabBridge.csproj -c Debug -m:1 \
  -p:EmbedAssembliesIntoApk=true \
  -p:AndroidSdkDirectory="$HOME/Android/Sdk" \
  -p:JavaSdkDirectory="$HOME/Android/jdk-21"
dotnet run --project tests/VocabBridge.CoreChecks
```

Change SDK/JDK paths to your installations. See Microsoft's dependency-installation reference below if these are missing.

```bash
adb devices
adb install -r install/VocabBridge-arm64-debug.apk
```

You can also copy the APK to a compatible phone and open it in its file manager. Other architectures require a build with `-r android-arm` or `-r android-x64` and have not been tested here. See **VALIDATION.md** for actual checks and remaining phone tests.

## Source map

- `Models/AppData.cs`: three languages, dynamic column definitions and aspect fields.
- `Services/LocalStore.cs`: real SQLite schema, transactions, migration and backups.
- `Services/LanguageRemoval.cs`: backup/confirmation flow independent of UI.
- `Services/PracticeQueue.cs`: selected-category, selected-language speech order.
- `Services/VoiceLabels.cs`: readable labels without engine IDs.
- `Platforms/Android/AndroidSpeech.cs`: phone TTS and download/settings intents.
- `Views/WordTable.cs`: aligned horizontally scrollable tables, paginated to 20 rows.
- `Views/SettingsPage.cs`, `PlaybackSettingsView.cs`: language/voice and playback controls.

## References

- [MAUI tab placement](https://learn.microsoft.com/en-us/dotnet/maui/android/platform-specifics/tabbedpage-toolbar-placement?view=net-maui-10.0)
- [Android voice metadata](https://developer.android.com/reference/android/speech/tts/Voice)
- [SQLite ALTER TABLE](https://www.sqlite.org/lang_altertable.html)
- [Microsoft.Data.Sqlite native libraries](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions)
- [Android and JDK dependencies](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies)

Native dependencies: Microsoft.Data.Sqlite 10.0.0 and SQLitePCLRaw bundle 2.1.13. The explicit bundle version replaces the older vulnerable transitive native SQLite package. Phone TTS availability still depends on the installed engine; downloads may need internet in that separate engine app.
