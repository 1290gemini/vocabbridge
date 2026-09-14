# Validation — VocabBridge Android 1.1

## Executed

- **55 executable checks passed** against production SQLite storage, schema migration, language-removal flow, speech queue, voice policy and display-name code. See `validation/core-checks.txt`. Android TTS transport and UI dispatcher are fakes in playback checks; SQLite tests use real native SQLite files and SQL queries.
- **ARM64 Android Debug APK build succeeded: 0 warnings, 0 errors.** .NET SDK 10.0.100, MAUI workload set 10.0.100, MAUI 10.0.0, .NET Android 36.1.2, Android SDK 36, JDK 21.0.9. See `validation/android-build.txt`.
- Initial restore identified an old vulnerable native SQLite dependency. The project and tests now explicitly use SQLitePCLRaw bundle **2.1.13**; final restore/build reports no dependency warnings. The resolved Android native package version is 2.1.13, and `lib/arm64-v8a/libe_sqlite3.so` is present in the APK.
- APK identity inspected: `com.vocabbridge.app`, version code **2**, version **1.1.0**, minimum API 23, target API 36, ARM64.
- APK manifest explicitly references `@mipmap/appicon` and `@mipmap/appicon_round`. The adaptive and raster launcher assets are present.
- APK signing verifies with v1, v2 and v3 schemes. The verifier emits standard v1 warnings about AndroidX META-INF metadata; full v2/v3 signatures verify. This is a debug test package, not a store release.
- Packaged manifest has no internet, microphone or broad storage permissions, and disables automatic backup/data transfer. AndroidX contributes an app-specific signature permission for internal receivers.
- Source inspection confirms no in-page logo references, old LanguagesPage, Listen language picker, or per-word Play controls.

## Data checks

Fresh installation has no selected languages and no language columns. Verbs and Vocabularies are separate SQLite tables. Language add is checked with `PRAGMA table_info`; Russian adds two aspect columns to Verbs and one to Vocabularies. Existing Unicode text persists through addition and restart.

Tests cover cancelled action sheet, cancelled export, failed export, cancelled final confirmation, successful backup-before-delete, physical column removal, retention of other translations, empty-row cleanup, empty re-addition, restore of dropped data/schema, and rejection of removal plans prepared before newer changes.

A deliberately failing SQLite trigger tests rollback after schema/data writes begin: both tables and settings remain unchanged. Other tests check invalid JSON, empty/oversized entries, concurrent saves, no timestamps/descriptions and removal of the last language.

Version-1 JSON migration retains the original file and preserves Russian verbs as unclassified previous text. It does not guess aspect. Clearing the final previous-text value drops that temporary column. A corrupt legacy file leaves no empty replacement database.

Speech checks cover category filtering, selected-language order, both Russian forms using the Russian voice, installed/offline filtering, readable locale names, numbered variants, native-completion order, rapid pause/resume, stop and repeat completion.

## Remaining real-phone checks

**No Android phone or emulator UI/audio session was available.** Actual layout, launcher display, document-picker behavior and speech quality must be checked on the target phone.

1. Back up the old app before installation. If signing identities differ, uninstall only after confirming the export exists, then install and restore. Verify version 1.1.0.
2. Confirm VB/VocabBridge is the installed launcher icon. Confirm app pages have no logo or navigation title bar, top tabs are Wordbook/Listen/Settings, and the background is the previous dark green.
3. On a fresh wordbook, select two languages, add rows to both tables, then select the third. Verify blank new columns and unchanged old text.
4. Add a Russian verb with Imperfective and Perfective. Verify both in the table and in speech. Check migrated Previous text rows can be classified manually.
5. Deselect a language. Exercise all Cancel/Backup/Delete branches, including cancelling the document picker. Confirm columns disappear only after final approval and other translations remain. Re-add the language and verify blank fields.
6. Inspect voice names on a real engine. Test each available offline voice and the download/settings fallback, including unsupported Myanmar. Return from Android settings and verify voice recheck.
7. In Listen, switch Verbs/Vocabularies and verify the corresponding table and speech list. Confirm only selected languages are read and that playback settings live in Settings.
8. Check table scrolling, 20-row pagination, long text and the keyboard on a narrow phone. Practice plays the complete selected category, including rows on other table pages.
9. Export/restore both a version-1 and a version-2 backup, restart the app and confirm persistence. Test sound in airplane mode after voice data is installed.

## Scope

No server, account or external data migration is involved. Data is private to the phone. Legacy backups containing languages beyond Myanmar/English/Russian are rejected without silently deleting those translations. The external TTS engine determines available voices and download screens; its metadata and audio are not certified by these core checks.
