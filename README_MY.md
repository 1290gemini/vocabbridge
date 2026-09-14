# VocabBridge Android 1.1 — မြန်မာလိုလမ်းညွှန်

ဒီ version မှာ သင်တောင်းထားသည့် table UI နှင့် Settings ပုံစံကို ပြောင်းထားပါသည်။ Native .NET MAUI Android app ဖြစ်ပြီး server၊ login/sign up/sign out မလိုပါ။ Data ကို ဖုန်းထဲတွင်သာ သိမ်းပါသည်။

## UI ပြောင်းလဲမှု

- အပေါ်ဆုံး app title bar ဖယ်ထားပြီး **Wordbook / Listen / Settings** menu ကနေ စပါသည်။ ဖုန်း၏ battery/time ပြသသည့် system status bar ကို ဆက်ထားသည်။
- နောက်ခံကို အရင် title bar ၏ အစိမ်းရင့်ရောင် `#163D38` အတိုင်းထားသည်။
- Page တွေထဲမှာ logo မပြပါ။ **VB / VocabBridge** logo ကို install ပြီးနောက် launcher တွင် မြင်ရမည့် app icon အဖြစ် ချိတ်ထားသည်။ Android စဖွင့်ချိန် system splash တွင် icon ပေါ်နိုင်သည်။
- Wordbook တွင် **Verbs table** နှင့် **Vocabularies table** သီးခြားပြသည်။ Row တစ်ခုချင်းစီတွင် Play မပါပါ။ Edit/Delete ပါသည်။
- Table ကျယ်လျှင် ဘယ်/ညာ swipe လုပ်ပြီး column တွေကြည့်နိုင်သည်။ Table တစ်ခုလျှင် တစ်မျက်နှာ ၂၀ row ပြပြီး Previous/Next ဖြင့် ကြည့်နိုင်သည်။

## Settings — ဘာသာစကားရွေးခြင်း

**Myanmar / English / Russian** သုံးခုသာ ရွေးနိုင်သည်။ အသစ် install လုပ်ပြီး ပထမဆုံးဖွင့်ချိန်တွင် တစ်ခုမှ default မရွေးထားပါ။ Settings မှ ကိုယ်လေ့လာမည့် ဘာသာစကားကို ON လုပ်ပါ။ ရွေးပြီးမှ စကားလုံးထည့်နိုင်ပါသည်။

အဟောင်းမှ update သို့မဟုတ် backup restore လုပ်လျှင် သိမ်းထားပြီးသော language choices ကို ထိန်းထားသည်။ Default မရွေးခြင်းသည် အသစ်စတင်သည့် wordbook အတွက် ဖြစ်သည်။

English/Myanmar နှစ်ခုဖြင့် စပြီး Russian ထပ်ရွေးလျှင် လက်ရှိ row များကို ထိန်းထားပြီး Russian column အသစ်များကို အလွတ်ဖြင့် ထည့်ပေးသည်။

Voice ပြသရာတွင် `en-US en-us-x-iom-local` ကဲ့သို့ engine code မပြတော့ဘဲ **English (United States)**၊ **Russian (Russia)** ကဲ့သို့ အမည်ပြသည်။ တစ်နေရာတည်းအတွက် အသံအများကြီးရှိလျှင် **Voice 1 / Voice 2** ဟု ခွဲပြသည်။ Engine မပေးထားသော လူနာမည်/ကျားမအသံအမျိုးအစားကို မခန့်မှန်းထားပါ။

အသံရှိပါက Test voice နှိပ်နိုင်သည်။ မရှိပါက Download voice data မှ ဖုန်း၏ TTS download/settings screen သို့ သွားနိုင်သည်။ Download ရနိုင်မှုသည် engine ပေါ်မူတည်သည်။ Myanmar အသံကို ဖုန်းတိုင်းက ပံ့ပိုးမည်ဟု မယူဆပါ။ Voice data download ပြီးလျှင် app သို့ ပြန်ဝင်ပြီး Recheck phone voices နှိပ်နိုင်သည်။ Voice မရှိလည်း စာသားသိမ်းခြင်းကို ဆက်လုပ်နိုင်သည်။

## Language တစ်ခုဖျက်ခြင်း

Settings မှ language switch ကို OFF လုပ်သည့်အခါ:

1. **Back up first / Delete without backup / Cancel** ဟု မေးပါသည်။
2. Back up first ရွေးလျှင် phone file picker မှ backup JSON သိမ်းပါ။ File picker ကို Cancel လုပ်လျှင် သို့မဟုတ် သိမ်းမရလျှင် **language မဖျက်ပါ**။
3. နောက်ဆုံး confirmation တွင် ပျက်မည့် language နှင့် ထိခိုက်မည့် row အရေအတွက်ကို ပြပါသည်။
4. **Delete language** ကို အတည်ပြုမှ database column နှင့် ၎င်းဘာသာ၏ စာသားများကို တကယ်ဖျက်ပါသည်။

အဲဒီဘာသာတစ်ခုတည်းတွင်သာ စာသားရှိသော row သည် column ဖျက်ပြီးနောက် အလွတ်ဖြစ်မည်ဖြစ်၍ row ပါဖျက်သည်။ အရေအတွက်ကို confirmation မှာ ဖော်ပြထားသည်။ အခြားဘာသာတွင် စာသားရှိသော row များကို ဆက်ထိန်းထားသည်။ နောက်ဆုံး language ပါ အတည်ပြုဖျက်လျှင် ရွေးချယ်မှုမရှိ၊ data မရှိသော အစအခြေအနေသို့ ပြန်ရောက်နိုင်သည်။

ပြန် ON လုပ်ခြင်းသည် ဖျက်ပြီးသောစာသားကို ပြန်ယူခြင်းမဟုတ်ပါ။ Column အလွတ် ပြန်ထည့်ခြင်းဖြစ်သည်။ အရင်စာသားပြန်လိုလျှင် backup restore လုပ်ပါ။ Dialog ဖွင့်ထားစဉ် data ပြောင်းသွားလျှင် backup နှင့် data ကိုက်ညီရန် ဖျက်ခြင်းကို ရပ်ပြီး ပြန်စရန်ပြောသည်။

## Russian verb နှစ်မျိုး

Verbs → Add verb တွင် Russian ရွေးထားလျှင် field နှစ်ခုပါသည်:

| Field | ဥပမာ |
|---|---|
| Russian · Imperfective | читать |
| Russian · Perfective | прочитать |

သက်ဆိုင်ရာ aspect မရှိလျှင် field အလွတ်ထားနိုင်ပါသည်။ Vocabulary row အတွက် Russian field တစ်ခုသာ ပါသည်။ Russian language ကို ဖျက်ပါက Russian vocabulary နှင့် verb aspect နှစ်မျိုးလုံး ဖျက်ပါသည်။

အရင် version က Russian verb ကို ဘယ် aspect မှန်း data ထဲတွင် မမှတ်ထားသောကြောင့် **Russian · Previous text** အဖြစ် သိမ်းထားသည်။ Edit လုပ်ပြီး သက်ဆိုင်ရာ Imperfective/Perfective field သို့ ထည့်ပါ။ ပြီးမှ Previous text field ကို ရှင်းနိုင်သည်။ Previous text အားလုံးရှင်းပြီးလျှင် အဲဒီ ယာယီ column မပြတော့ပါ။ မသိဘဲ aspect တစ်ခုသို့ အလိုအလျောက်မသတ်မှတ်ပါ။

## Listen

**Verbs / Vocabularies** ရွေးသည့်အတိုင်း သက်ဆိုင်ရာ table ကို ပြပါသည်။ **Start practice** နှိပ်လျှင် အဲဒီ category ထဲရှိ row အားလုံးကို Settings တွင် ရွေးထားသော language အစဉ်အတိုင်း ဖတ်သည်။ Pagination တွင် တစ်မျက်နှာမြင်ရသော row များသာမက category အားလုံးကို ဖတ်သည်။

Russian verbs တွင် Imperfective ပြီး Perfective ကို Russian voice ဖြင့် ဖတ်သည်။ အလွတ် field များကို ကျော်သည်။ မရွေးထားသော language ကို မဖတ်ပါ။ Listen တွင် All selected languages picker မပါတော့ပါ။

**Speed / Gap / Repeat count** ကို **Settings → Playback settings** တွင် ပြင်ပြီး Save playback settings နှိပ်ပါ။ နောက် Start practice လုပ်ချိန်မှ setting အသစ်ကို သုံးသည်။ Pause/Resume/Stop ကို Listen တွင်ထားသည်။ Pause ပြီး resume လုပ်ပါက ရပ်ထားသည့်စကားလုံး၏ အစမှ ပြန်ဖတ်သည်။ App ကို background ပို့ပါက playback ရပ်သည်။

## Data နှင့် backup

ဖုန်း၏ app-private storage အောက်ရှိ **wordbook.sqlite3** တွင် **Verbs / Vocabularies** table နှစ်ခုဖြင့် သိမ်းသည်။ Language ရွေး/ဖျက်ခြင်းသည် SQLite column တကယ်ထည့်/ဖျက်ခြင်းဖြစ်သည်။ Column နှင့် data ပြောင်းခြင်းကို transaction တစ်ခုတည်းဖြင့် လုပ်သောကြောင့် တစ်ဝက်တစ်ပျက်ဖြစ်လျှင် မူလအခြေအနေကို ထိန်းသည်။

Settings → Export backup to phone ဖြင့် Downloads ကဲ့သို့ ကိုယ်ရွေးသော local folder ထဲ JSON backup သိမ်းနိုင်သည်။ Restore a backup သည် လက်ရှိ wordbook တစ်ခုလုံးကို အစားထိုးသည်။ လက်ရှိစာသားများ ဆက်လိုလျှင် restore မလုပ်ခင် export လုပ်ပါ။ App uninstall / Clear storage လုပ်လျှင် private database ပျက်နိုင်ပါသည်။

ယခင် `wordbook.json` ရှိလျှင် SQLite သို့ ကူးပြီး original JSON ကို ထိန်းထားသည်။ Version 1 JSON backup ကိုလည်း restore လုပ်နိုင်သည်။ Myanmar/English/Russian အပြင် အခြားဘာသာပါသော အဟောင်းဖိုင်ကို မဖျက်ဘဲ message ပြပြီး ကူးခြင်းရပ်သည်။

## APK အသစ်တင်ရန်

`install/VocabBridge-arm64-debug.apk` သည် ARM64 Android 6.0 နှင့်အထက် ဖုန်းအတွက် test build ဖြစ်သည်။

အဟောင်းတွင် data ရှိပါက **အရင် app မှ Export backup လုပ်ထားပါ**။ Test APK signing key က အဟောင်းနှင့် မတူလျှင် အပေါ်ကနေ update install လုပ်မရနိုင်ပါ။ `INSTALL_FAILED_UPDATE_INCOMPATIBLE` ဖြစ်ပါက backup ဖိုင်ရှိကြောင်း အရင်စစ်ပြီးမှ app အဟောင်းကို uninstall လုပ်ပါ။ APK အသစ်တင်ပြီး Settings → Restore a backup ဖြင့် ပြန်ယူပါ။

ZIP ဖြည်ထားသည့် VocabBridge-Android folder ထဲတွင်:

```bash
adb devices
adb install -r install/VocabBridge-arm64-debug.apk
```

ဖုန်းမှ app ကိုဖွင့်ပါ။ Settings မှ language ရွေး → Test voice → Wordbook တွင် data ထည့် → Listen တွင် category ရွေးပြီး စမ်းပါ။

## Linux Mint မှ build လုပ်ရန်

လိုအပ်ချက်များ: .NET 10 SDK၊ MAUI Android workload၊ Android SDK 36၊ JDK 21။ SDK/JDK directory ကို ကိုယ့်စက်တွင်ရှိသည့် path ဖြင့် ပြောင်းပါ။

```bash
dotnet workload install maui-android --version 10.0.100
dotnet build src/VocabBridge/VocabBridge.csproj -c Debug -m:1 \
  -p:EmbedAssembliesIntoApk=true \
  -p:AndroidSdkDirectory="$HOME/Android/Sdk" \
  -p:JavaSdkDirectory="$HOME/Android/jdk-21"
```

Build APK: `src/VocabBridge/bin/Debug/net10.0-android/android-arm64/com.vocabbridge.app-Signed.apk`။ ကိုယ့်စက်မှ အမြဲ build လုပ်လျှင် signing key တူအောင် ထိန်းထားနိုင်သည်။

```bash
dotnet run --project tests/VocabBridge.CoreChecks
```

စမ်းသပ်ပြီးသောရလဒ်နှင့် ဖုန်းပေါ်တွင် ဆက်စစ်ရန်အချက်များကို **VALIDATION.md** တွင် ဖတ်ပါ။ ဖုန်းတကယ်၏ TTS အသံနှင့် UI ကို ဒီနေရာမှ စမ်းထားခြင်းမရှိပါ။
