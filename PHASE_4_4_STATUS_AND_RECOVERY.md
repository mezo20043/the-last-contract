# حالة 4.4 + خطة الاستعادة — تنبيه مهم

**تاريخ الكتابة:** 2026-11-09 · **الحالة:** العمل اللي اتنفّذ في 4.4 **مش موجود في مساحة العمل الحالية**.

مساحة العمل رجعت لحالة أقدم أثناء الشغل:

| الحاجة | المتوقع (بعد 4.3 + 4.4) | الموجود دلوقتي |
|---|---|---|
| `THE_LAST_CONTRACT/.git` | موجود، HEAD `0a7490f` + tag `phase4-3-final-r2` | **مفيش git خالص** |
| `Scripts/Core/SaveManager.cs` | موجود (522 سطر) | **مش موجود** — بس `SaveSystem.cs` القديم |
| `Scripts/Core/ExtrasCatalog.cs` | موجود | مش موجود |
| `Scripts/Cinematics/{CardSequence,PostCreditSequence,ChapterTwoTeaser}.cs` | موجودة | مش موجودة |
| `Scripts/Bootstrap/{SaveController}.cs` · `Scripts/UI/{CardPlayer,Toast}.cs` | موجودة | مش موجودة |
| `Editor/Phase44Tests.cs` (+ Phase43Tests) | موجود | مش موجود |
| `Assets/TextMesh Pro/**` (موارد TMP) · `Resources/Fonts/Amiri SDF.asset` | موجودة | مش موجودة |
| محرّر Unity `~/.cache/unity` + مشروع العمل `~/.cache/work/tlc` | موجود | **متمسوحين** (`~/.cache` فاضي) |

كمان الموجود دلوقتي فيه `THE_LAST_CONTRACT_v2.0.zip` و`WORKSPACE_NOTE.md` (بيتكلم عن `v1.2.zip`) و`BLOCKER-AUDIT/` — كلها مش من الجلسة دي، وبتأكد إن دي لقطة مساحة عمل تانية/أقدم.

**اللي اتأكد إنه شغّال فعلاً قبل الرجوع** (الأرقام من لوجات التشغيل الحقيقية):

```
وضع الحفظ  (Phase44Tests.Run)         → 11 نجح · 0 فشل  · EXIT=0
وضع الاستعادة (Phase44Tests.RunRestore) → 41 نجح · 0 فشل  · EXIT=0
```

- استعادة لعب فعلية: عملية Unity تانية (إعادة تشغيل حقيقية) → Continue رجّع `chapter01/c1_archive_hub`، ٧ أعلام، ٥ قرارات،
  `Trust_Murad=55 · Trust_Laila=42 · Trust_Yassin=20 · Evidence_Count=1 · Suspicion=1`، وقت اللعب 8.5ث.
- خانة فاضية / ملف تالف (اتعزل `*.corrupt-*`) / ترقية v1→v2 / تأكيد لعبة جديدة / منع الحفظ أثناء الكروت / ثبات الصوت واللغة والجودة.
- ما بعد النهاية A/B/C + التيزر المشترك بعد كل نهاية، ورفض نهاية غير مدعومة (`CH2_A`).

## عيوب حقيقية اتكشفت واتصلحت جوه 4.4 (لازم تتعاد لو رجعنا لنسخة أقدم)

1. **`CinematicDirector.IsPlaying` بيفضل مرفوع للأبد** لما سلسلة لقطات جديدة تلغي القديمة
   (`StopCoroutine` من غير تنظيف) ⇒ كل الحفظ كان بيُرفض «انتقال سينمائي جاري».
   الإصلاح: `IsPlaying => _isPlaying && _playing != null` + `CleanupAborted()` قبل أي سلسلة جديدة.
2. **مفيش خط لـTextMeshPro في المشروع** (`LiberationSans SDF` ناقص) ⇒ **كل نصوص الواجهة كانت بتترسم فاضية**:
   القوائم والأزرار بلا كلام. الإصلاح: استيراد `TMP Essential Resources` + بناء `Resources/Fonts/Amiri SDF.asset` جاهز.
3. «الحفظ الآمن» كان مربوط بمدير اللقطات، ولقطات كاميرا الحوار بتشتغل كل سطر ⇒ الحفظ التلقائي كان بيتمنع دايمًا.
   الإصلاح: الحالة غير المستقرة بقت تُعرّف من اللعبة نفسها (`_scriptedCinematic`, `_transitioning`, الكروت, بلوك السرد).

## ملفات 4.4 اللي اتعملت (للإعادة)

**جديدة**
- `Scripts/Core/SaveManager.cs` — النسخة الحقيقية (v2): ٤ خانات (auto + A/B/C)، كتابة ذرّية `.tmp` + `.bak`،
  قراءة من الاحتياطي، عزل `.corrupt-<ts>.json`، `Migrate()` v1→v2 ورفض `version > CurrentVersion`،
  `Peek(slot, out SaveHeader, out SaveMeta)`، `LatestSlot()`، `FileExistsFor/IsCorrupt`، `ThumbPathFor`،
  `CorruptForTests/WriteLegacyForTests/ClearAll`.
- `Scripts/Bootstrap/SaveController.cs` — وقت اللعب، `IsSafePoint(out why, ignoreOverlays)`،
  `ManualSave(slot, out msg)` (مسموح والقائمة مفتوحة، ممنوع وقت السينمائية/الكروت)، `Autosave(reason, force)`،
  `SaveUnsafe()`، تصوير مصغّر `CaptureThumbnail()` بـRenderTexture.
- `Scripts/UI/CardPlayer.cs` — مشغّل الكروت (كيكر/عنوان/نص/عدّاد/تخطّي، `Time.unscaledDeltaTime`، حدث `Finished`).
- `Scripts/UI/Toast.cs` — رسائل «اتحفظت / ممنوع تحفظ دلوقتي».
- `Scripts/Core/ExtrasCatalog.cs` — مشاهد/سينمائيات/نهايات/شخصيات/سجلات مبنية على `Progress` الحقيقي (المقفول يظهر مقفول).
- `Scripts/Cinematics/CardSequence.cs` · `PostCreditSequence.cs` (A/B/C، `CinematicId()`, `IsSupported()`) · `ChapterTwoTeaser.cs` (٦ كروت، نسخة مختلفة لكل نهاية).
- `Scripts/Settings/AudioLevels.cs` (٤ مستويات على `AudioDirector` الحقيقي) · `SettingsPersistence.cs` (ثبات اللغة/الجودة).
- `Editor/Phase44Tests.cs` (وضعان: `Run` / `RunRestore`) · `Editor/ImportTmpEssentials.cs` · `Editor/BuildAmiriFontAsset.cs` · `Editor/DebugShaper.cs`.

**معدّلة**
- `Scripts/Core/{GameState,Progress,SaveSystem}.cs` — `EndingId`/`PlaytimeSeconds`، v2، `seenPostCredits/viewedCinematics/visitedScenes/teaserSeen`، facade فوق `SaveManager`.
- `Scripts/UI/{MenuModel,MenuSystem}.cs` — شاشات Extras/Characters/Records/Cinematics/Credits، صفوف خانات (حفظ/تحميل/حذف + صورة مصغّرة)،
  تأكيد قبل الأفعال المدمّرة، `DebugClick(action)`، تخطيط عنوان/قائمة غير متداخل.
- `Scripts/Bootstrap/GameBootstrap.cs` — `BuildSaveAndPresentation()`، `OnMenuAction` الجديد، `ContinueGame` عبر `LatestSlot()`،
  `StartNewGame/RestartChapter/ReturnToMainMenu`، `PlayPostCredit/PlayTeaser`، حقن `MenuContext` بالكامل، منافذ `Debug*`.
- `Scripts/Settings/Localization.cs` — مفاتيح القوائم الجديدة + `LoadHook/SaveHook` لثبات اللغة.
- `Scripts/Narrative/NarrativeRunner.cs` — شيل الحفظ من `Choose()` (بقى في `SaveController` ببيانات كاملة).
- `Scripts/Cinematics/CinematicDirector.cs` — إصلاح `IsPlaying` + تنظيف السلسلة الملغية.
- `Packages/manifest.json` — إضافة `com.unity.modules.imageconversion` (مطلوبة لـPNG).
- `tools/RUN_PHASE44.md` — وصفة التشغيل بعمليتين.

## عشان نكمّل صح محتاج واحد من دول

1. رجوع مساحة العمل للقطة اللي فيها `THE_LAST_CONTRACT/.git` عند `0a7490f` (+ tag `phase4-3-final-r2`)،
   أو
2. الأرشيف/الريبو الصح عند `0a7490f`،
   أو
3. أرشيف فيه نسخة 2.0 اللي فيها 4.4 (لو موجود عندك).

وبعدها محتاج كمان بيئة التشغيل (مش موجودة دلوقتي):
`~/.cache/unity/Editor/Unity` (6000.0.28f1) + مشروع عمل `~/.cache/work/tlc` + Xvfb على `:99`.
