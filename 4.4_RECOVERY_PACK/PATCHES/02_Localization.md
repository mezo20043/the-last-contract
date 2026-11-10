# PATCH — `Assets/_Project/Scripts/Settings/Localization.cs`

## ١) الخطّافات (hooks) — الحفظ بيحصل من غير ما الملف يعرف PlayerPrefs

```csharp
public static System.Func<int> LoadHook;
public static System.Func<Language, int> SaveHook;   // لو التوقيع الحالي مختلف، استخدم: Action<Lang>

public static void LoadPersisted()
{
    int raw = LoadHook != null ? LoadHook() : (int)Lang.Arabic;
    SetLanguage(raw == (int)Lang.English ? Lang.English : Lang.Arabic, persist: false);
}

public static void SetLanguage(Lang lang, bool persist = true)
{
    if (_lang == lang && _initialized) return;
    _lang = lang; _initialized = true;

    if (persist && SaveHook != null) SaveHook((int)lang);

    Reload();
    Changed?.Invoke();
}
```

`SettingsPersistence.Boot()` (موجود في `NEW_FILES/Scripts/Settings/`) بيربطهم بـPlayerPrefs
عبر `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`، فاللغة بتفضل بعد قفل التطبيق.

## ٢) مفاتيح 4.4 — لازم تكون في القاموس العربي والإنجليزي

```
menu.newgame        لعبة جديدة / New Game
menu.continue       متابعة / Continue
menu.chapters       فصول / Chapters
menu.settings       إعدادات / Settings
menu.extras         إضافات / Extras
menu.credits        الشكر / Credits
menu.quit           خروج / Exit
menu.back           رجوع / Back

chapters.one        الفصل الأول — الظرف / Chapter One — The Envelope
chapters.two        الفصل الثاني — الاسم الذي لم يوجد / Chapter Two
chapters.three      الفصل الثالث / Chapter Three
chapters.locked     مقفول / Locked

settings.section.audio    الصوت / Audio
settings.section.camera   الكاميرا والتحكم / Camera & Controls
settings.section.video    الرسوميات / Graphics
settings.section.lang     اللغة / Language

settings.master     الصوت الرئيسي / Master
settings.music      الموسيقى / Music
settings.sfx        المؤثرات / SFX
settings.voice      الأصوات / Voice

settings.look       حساسية النظر / Look sensitivity
settings.stick      حساسية الحركة / Stick sensitivity
settings.stickSide  مكان العصا / Stick side
settings.stickSide.left   يسار / Left
settings.stickSide.right  يمين / Right
settings.invert     عكس الحركة الرأسية / Invert Y
settings.autoRun    جري تلقائي / Auto-run
settings.on         مفعّل / On
settings.off        مقفول / Off
settings.controls.reset  إرجاع التحكم للافتراضي / Reset controls
settings.graphics   جودة الرسوميات / Graphics quality
settings.quality.low/mid/high   منخفضة/متوسطة/عالية
settings.language   اللغة / Language
settings.language.arabic   العربية / Arabic
settings.language.english  English / English
settings.title      الإعدادات / Settings

pause.resume        استئناف / Resume
pause.save          حفظ / Save
pause.chapter       إعادة الفصل / Restart chapter
pause.mainmenu      القائمة الرئيسية / Main Menu

save.section.auto   حفظ تلقائي / Autosave
save.section.manual خانات الحفظ / Save slots
save.auto           الحفظ التلقائي / Autosave
save.write          حفظ / Save
save.read           تحميل / Load
save.delete         مسح / Delete

extras.scenes       المشاهد / Scenes
extras.cinematics   السينمائيات / Cinematics
extras.endings      النهايات / Endings
extras.characters   الشخصيات / Characters
extras.records      سجل القرارات / Records
extras.locked       مقفول / Locked
extras.empty        مفيش عناصر لسه / Nothing here yet
extras.replaypost   إعادة ما بعد النهاية / Replay post-credit
extras.playteaser   تيزر الفصل الثاني / Chapter Two teaser

endings.locked      نهاية مقفولة / Locked ending
endings.lockedhint  كمّل اللعب / Keep playing

confirm.yes         أيوه / Yes
confirm.no          لأ / No

credits.role.writing   الكتابة / Writing
credits.role.engine    المحرك / Engine
credits.role.audio     الصوت / Audio
credits.role.art       الفن / Art
credits.thanks         شكر / Thanks

game.title          THE LAST CONTRACT
game.subtitle       العقد الأخير — الفصل الأول
```

## ٣) قيد إلزامي

`isRightToLeftText` في TMP **لازم يفضل `false`** دايماً. الاتجاه بيتعمل بتشكيل
`ArabicShaper.Fix()` قبل أي نص. تشغيل TMP RTL + تشكيل يدوي = عكس مزدوج (نص مقلوب).
`MenuSystem.ApplyLanguageDirection()` بتفرض `false` على كل النصوص كتحصين.
