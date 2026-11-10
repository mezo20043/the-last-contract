# 4.4 RECOVERY PACK — اقرأ ده الأول

**تاريخ التجهيز:** 2026-11-09 · **الحالة: لقطة `0a7490f` لسه مش موجودة · الاستعادة مش متحققة · 4.4 مش معلن اكتماله.**

## ١) الخلاصة في تلات سطور

شغل **4.4** كان نُفِّذ ونجح فعليًا (Play Mode: `save` 11/0/0 · `restore` 41/0/0 · `EXIT=0`، ١٢ لقطة)
على أساس `0a7490f` / tag `phase4-3-final-r2`. بعدها **بيئة التشغيل اتصفّرت**: `.git`، كود 4.3 r2
و4.4، محرّر Unity، نسخة العمل، ولقطات `phase4_4`. البحث الكامل على القرص (شوف
`02_SEARCH_AND_BASELINE.md`) مالاقاش أي مصدر للقطة.

الباكدج ده **مش استعادة**، ومش بديل عن اللقطة: هو إعادة تطبيق مكتوبة من التوثيق — كل ملف ومستوى
ثقته موضّحين تحت. **مفيش اختبار قبول واحد اتشغّل على الكود اللي هنا**، لأنه مفيش Unity في البيئة.

## ٢) قواعد إلزامية

- **ممنوع** تحط الملفات دي على شجرة `/home/user/THE_LAST_CONTRACT/` الحالية — دي **4.2 + تقارير 4.3**
  (`GameBootstrap.cs` ١٥٠٠ سطر، مفيش `SaveManager`، مفيش `Phase43Tests`). وضع 4.4 فوقها = خلط نسختين.
- الملفات دي **مرجع إعادة تطبيق** على نسخة `0a7490f` لما ترجع.
- مفيش أي ملف من الشجرة القديمة اتعدّل أو اتمسح أثناء تجهيز الباكدج — كل حاجة اتكتبت جوه `4.4_RECOVERY_PACK/`.
- **ممنوع إعلان نجاح الاستعادة أو اكتمال 4.4** قبل: (أ) رجوع اللقطة، (ب) تطبيق الملفات،
  (ج) تشغيل `Run` ثم `RunRestore` فعليًا وخروج `EXIT=0`.

## ٣) إيه اللي جوه

| المسار | المحتوى |
|---|---|
| `01_STATUS_AND_RECOVERY.md` | تقرير الحالة + الأرقام المؤكدة + العيوب اللي اتصلحت + واجهات الملفات |
| `02_SEARCH_AND_BASELINE.md` | نتيجة البحث عن `0a7490f` (خلاصة: **مالقتهاش**) + متطلبات البيئة وأوامر التحقق |
| `NEW_FILES/Scripts/**` | الملفات **الجديدة** اللي اتعملت في 4.4 |
| `NEW_FILES/Editor/**` | أدوات المحرر الجديدة + سكربت قبول 4.4 |
| `PATCHES/00_ORDER.md` | ترتيب التطبيق الإلزامي + الفخّان المعروفان |
| `PATCHES/01..06` | التعديلات على الملفات **الموجودة** (مقاطع مرساة، مش ملفات كاملة) |
| `EVIDENCE/` | ٥ لقطات نجت + مقتطفات اللوجات (`03_run_logs_excerpt.md`) |
| `tools/RUN_PHASE44.md` | وصفة التشغيل الكاملة (عمليتان Unity: حفظ ثم استعادة) |
| `MANIFEST_CHECKSUMS.md` | سطور + بصمة SHA-256 لكل ملف (لاكتشاف أي تلف أثناء النقل) |

## ٤) جرد الملفات ومستوى الثقة

**١٢ ملف مُعاد حرفيًا** (نصّه اتكتب في نفس الجلسة ومحفوظ) · **٢ مُعاد من العقد** · **٢ مؤلَّف للإصلاح**.

| # | ملف | سطور | الوجهة في المشروع | الثقة |
|---|---|---|---|---|
| 1 | `Scripts/Bootstrap/SaveController.cs` | 199 | `Scripts/Bootstrap/` | ✅ حرفي |
| 2 | `Scripts/UI/Toast.cs` | 78 | `Scripts/UI/` | ✅ حرفي |
| 3 | `Scripts/UI/CardPlayer.cs` | 191 | `Scripts/UI/` | ✅ حرفي |
| 4 | `Scripts/UI/MenuModel.cs` | 437 | `Scripts/UI/` | ✅ حرفي |
| 5 | `Scripts/UI/MenuSystem.cs` | 433 | `Scripts/UI/` | ✅ حرفي (النسخة النهائية) |
| 6 | `Scripts/Core/ExtrasCatalog.cs` | 158 | `Scripts/Core/` | ✅ حرفي |
| 7 | `Scripts/Settings/AudioLevels.cs` | 60 | `Scripts/Settings/` | ✅ حرفي |
| 8 | `Scripts/Settings/SettingsPersistence.cs` | 43 | `Scripts/Settings/` | ✅ حرفي |
| 9 | `Editor/ImportTmpEssentials.cs` | 71 | `Assets/_Project/Editor/` | ✅ حرفي |
| 10 | `Editor/BuildAmiriFontAsset.cs` | 84 | `Assets/_Project/Editor/` | ✅ حرفي |
| 11 | `Editor/DebugShaper.cs` | 39 | `Assets/_Project/Editor/` | ✅ حرفي |
| 12 | `Editor/Phase44Tests.cs` | 710 | `Assets/_Project/Editor/` | ✅ حرفي |
| 13 | `Scripts/Core/SaveManager.cs` | 580 | `Scripts/Core/` | ⚠️ **عقد برمجي** — الأصل ٥٢٢ سطر؛ الـAPI/السلوك/الملفات/الثوابت مؤكدة (من الاستدعاءات اللي نجحت)، السطور الداخلية محتاجة diff |
| 14 | `Scripts/Cinematics/CardSequence.cs` | 49 | `Scripts/Cinematics/` | ⚠️ عقد برمجي (بنية الكارت/السلسلة) |
| 15 | `Scripts/Cinematics/PostCreditSequence.cs` | 92 | `Scripts/Cinematics/` | 🟡 مؤلَّف: البنية والثوابت («A/B/C»، variant 1/2/3) مؤكدة، **نصوص الكروت** مش منقولة حرفيًا |
| 16 | `Scripts/Cinematics/ChapterTwoTeaser.cs` | 67 | `Scripts/Cinematics/` | 🟡 مؤلَّف: العنوان «THE NAME THAT NEVER EXISTED» + ٦ كروت + متغيّر لكل نهاية مؤكدة، النصوص لأ |
| 17 | `Scripts/Bootstrap/GameBootstrap.Phase44.cs` | 533 | `Scripts/Bootstrap/` | 🟠 **مُعاد بناءً** — ربط 4.4 كـ`partial class` + ٣٠ عضوا `Debug*`؛ يحتاج `partial` + ٤ خصائص وصول في الملف الأصلي |

**الإجمالي:** ١٧ ملف · ٣٨٢٣ سطر.  (يضاف لهم: `CHAT_RECOVERY_LEDGER.md` — سجل ما اترجّع من المحادثة نفسها)

### إيه اللي **مش** في الباكدج وبيتعمل بالـpatch

`GameBootstrap.cs` — **بقى له كود فعلي** في الملف ١٧ أعلاه (المطلوب منه سطر `partial` + ٤ خصائص وصول فقط)،
و`PATCHES/01_GameBootstrap.md` فضل كمرجع تفصيلي · `MenuModel/MenuSystem`
هما ملفين جداد فعلاً (موجودين هنا) · `Localization.cs` · `NarrativeRunner.cs` ·
`CinematicDirector.cs` · `GameState.cs` · `Progress.cs` · `SaveSystem.cs` · `manifest.json`.

## ٥) من أين نبدأ

1. اقرأ `02_SEARCH_AND_BASELINE.md` — يعرفك بالظبط إيه اللي ناقص عشان ترجع اللقطة.
2. لو اللقطة رجعت: `CHAT_RECOVERY_LEDGER.md` (مين بيرجّع إيه ومن فين) ← `PATCHES/00_ORDER.md` ← `tools/RUN_PHASE44.md`.
3. لو اللقطة مجتش: **متلمسش** شجرة 4.2. الباكدج ده يتسيب مرجع لحد ما يجي مصدر.
