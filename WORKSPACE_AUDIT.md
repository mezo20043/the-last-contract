# فحص مساحة العمل — تقرير تدقيقي (Audit)

**التاريخ:** 2026-11-09 · **النوع:** فحص للقراءة فقط · **لم يُعدَّل أو يُنقل أو يُحذف أي ملف.**

---

## ٠) الحكم النهائي

| السؤال | الجواب |
|---|---|
| هل ملفات `4.4_RECOVERY_PACK` موجودة فعلًا ومحفوظة؟ | **آه — ٣٤ ملف · ٢,٠٩٩,٩٧٤ بايت (٢.١ ميجا).** الـ١٦ ملف `.cs` اتأكدوا بالبصمة مقابل `MANIFEST_CHECKSUMS.md` (١٦/١٦ مطابق). مفيش ملف صفر بايت. |
| هل عندنا النسخة الأصلية الكاملة للمشروع؟ | **لأ.** عندنا **شجرة 4.2** (٤٧٥ ملف) + **أرشيف أقدم منها** + **Recovery Pack**. مفيش شجرة 4.3 r2 ولا 4.4. |
| هل فيه Git repository أو commit `0a7490f` أو tag `phase4-3-final-r2`؟ | **لأ.** مفيش `.git` في أي مكان على القرص (بحث شامل، جوه `/home/user` وبره). الاسمين ظهروا **كنص في ٦ ملفات توثيق بس**. |
| هل `SaveManager.cs` / `Phase44Tests.cs` الأصليين موجودين؟ | **لأ.** الموجود نسخ **أعاد بناءها الـRecovery Pack** (٥٨٠ و٧١٠ سطر) — مش الأصل (٥٢٢ سطر لـ`SaveManager`). |
| هل يمكن استعادة المشروع دون فقدان بيانات؟ | **لأ، مش بالكامل.** الاستعادة الكاملة مستحيلة محليًا (شوف §٧). |
| هل `archive/` فيه نسخة من المشروع؟ | **لأ.** ٣ صور JPG فقط (٤٦٨ كيلو) من فكرة تانية اسمها `idea-borrowed-memory`. |

> **مهم:** وجود مجلد Recovery Pack **مش دليل** على وجود Git baseline. الاثنان منفصلان تمامًا:
> الباكدج توثيق + كود مُعاد كتابته؛ اللقطة (repo/tag) مش موجودة أصلًا.

---

## ١) خريطة مساحة العمل (كل حاجة في `/home/user`)

| المسار | النوع | الحجم | الحالة |
|---|---|---|---|
| `THE_LAST_CONTRACT/` | شجرة مشروع Unity (4.2 + تقارير 4.3) | 32.5 ميجا · ٤٧٥ ملف | موجودة · **مفيش `.git`** |
| `THE_LAST_CONTRACT_v2.0.zip` | أرشيف (أقدم من الشجرة) | 98.2 ميجا · ٤٠٤ ملف | سليم (`unzip -t` = No errors) |
| `4.4_RECOVERY_PACK/` | توثيق + إعادة بناء 4.4 | 2.1 ميجا · ٣٤ ملف | **كامل ومتحقق بالبصمة** |
| `the-last-contract/` | نموذج أولي قديم (ويب + سكربتات Unity صغيرة) | 4.9 ميجا · ٤٨ ملف | **مش المشروع** — مفيش git |
| `archive/idea-borrowed-memory/` | ٣ صور مرجعية | 468 كيلو | مش له علاقة بالنسخ |
| `compile-check/` | هاك ترجمة بـStubs + `TLCCheck.dll` مترجمة سابقًا | 1.1 ميجا | `csproj` بيشير لشجرة 4.2 بس |
| `shots_t2/` · `uploads/` · `lic/` | لقطات قديمة · صور مرفوعة · `dl.bin` (XML طلب ترخيص) | — | مش لها علاقة بالاستعادة |
| `run44.sh` (918 بايت) · `compile_check.sh` (660) | سكربتات تشغيل | — | **بتشتغلش**: بتشاور على `~/.cache/unity` و`~/.cache/work/tlc` (مش موجودين) |
| `PHASE_4_4_STATUS_AND_RECOVERY.md` · `WORKSPACE_NOTE.md` · `BLOCKER-AUDIT/` | تقارير | — | موجودة |

---

## ٢) `4.4_RECOVERY_PACK/` — جرد كامل بالحجم

**٣٤ ملف · ٢,٠٩٩,٩٧٤ بايت.** (٣ ملفات توثيق رئيسية + بصمات + ٥ لقطات + ١٦ `.cs` + ٧ patch + وصفة تشغيل + لوج)

| المسار | بايت |
|---|---|
| `00_READ_ME.md` | 6,112 |
| `01_STATUS_AND_RECOVERY.md` | 8,731 |
| `02_SEARCH_AND_BASELINE.md` | 4,179 |
| `EVIDENCE/03_run_logs_excerpt.md` | 7,249 |
| `EVIDENCE/ia_menu_main_after_4.4_fix.png` | 58,336 |
| `EVIDENCE/ia_play_restored.png` | 870,322 |
| `EVIDENCE/ia_postcredit_CH1_A.png` | 426,051 |
| `EVIDENCE/ia_save_load_slots.png` | 101,657 |
| `EVIDENCE/ia_teaser_ch2.png` | 434,019 |
| `MANIFEST_CHECKSUMS.md` | 1,167 |
| `NEW_FILES/Editor/BuildAmiriFontAsset.cs` | 3,186 |
| `NEW_FILES/Editor/DebugShaper.cs` | 1,418 |
| `NEW_FILES/Editor/ImportTmpEssentials.cs` | 2,927 |
| `NEW_FILES/Editor/Phase44Tests.cs` | 35,206 |
| `NEW_FILES/Scripts/Bootstrap/SaveController.cs` | 8,122 |
| `NEW_FILES/Scripts/Cinematics/CardSequence.cs` | 1,720 |
| `NEW_FILES/Scripts/Cinematics/ChapterTwoTeaser.cs` | 3,670 |
| `NEW_FILES/Scripts/Cinematics/PostCreditSequence.cs` | 5,567 |
| `NEW_FILES/Scripts/Core/ExtrasCatalog.cs` | 7,582 |
| `NEW_FILES/Scripts/Core/SaveManager.cs` | 25,574 |
| `NEW_FILES/Scripts/Settings/AudioLevels.cs` | 2,379 |
| `NEW_FILES/Scripts/Settings/SettingsPersistence.cs` | 1,689 |
| `NEW_FILES/Scripts/UI/CardPlayer.cs` | 7,290 |
| `NEW_FILES/Scripts/UI/MenuModel.cs` | 18,974 |
| `NEW_FILES/Scripts/UI/MenuSystem.cs` | 18,375 |
| `NEW_FILES/Scripts/UI/Toast.cs` | 2,584 |
| `PATCHES/00_ORDER.md` | 3,191 |
| `PATCHES/01_GameBootstrap.md` | 14,721 |
| `PATCHES/02_Localization.md` | 4,885 |
| `PATCHES/03_NarrativeRunner.md` | 2,061 |
| `PATCHES/04_CinematicDirector.md` | 1,618 |
| `PATCHES/05_GameState_Progress_SaveSystem.md` | 4,593 |
| `PATCHES/06_manifest.md` | 1,316 |
| `tools/RUN_PHASE44.md` | 3,503 |

### تحقق السلامة
- بصمة `SHA-256[:16]` لكل ملف `.cs` **مطابقة** لـ`MANIFEST_CHECKSUMS.md` (١٦/١٦).
- الـ٥ لقطات كلها PNG سليمة بمقاس **1280×720**، ومحتواها **مطابق بالبايت** لملفات في جذر مساحة العمل:
  `ia_menu_main_after_4.4_fix.png` = `_preview_main_menu.png` · `ia_play_restored.png` = `_preview_play.png` ·
  `ia_save_load_slots.png` = `_preview_slots.png` = `_pv_save_load_menu.png` ·
  `ia_postcredit_CH1_A.png` = `_pv_postcredit_A.png` · `ia_teaser_ch2.png` = `_pv_teaser_CH1_A.png`.
  ⇒ اللقطات **مكرّرة** (نسخة في الجذر ونسخة في الباكدج) — مش ضياع، بس مش أدلة مستقلة.
- ملاحظة صريحة: لقطتان من الخمس موثّق فيهما **عيوب معروفة** وقت التصوير (منيو رئيسي متراكب العناصر،
  وخلفية كارت ما-بعد-النهاية شفافة) — يعني هي أدلة «حالة»، مش إثبات قبول بصري نظيف.

---

## ٣) المشروع الأصلي — اللي موجود فعلًا

`THE_LAST_CONTRACT/` (٤٧٥ ملف · 32.5 ميجا · ٦٨ ملف `.cs` + ١٩ ملف Editor):

| الملف | سطور | بايت |
|---|---|---|
| `Scripts/Bootstrap/GameBootstrap.cs` | 1,500 | 63,639 |
| `Scripts/UI/MenuSystem.cs` | 311 | 12,141 |
| `Scripts/UI/MenuModel.cs` | 214 | 8,732 |
| `Scripts/Core/SaveSystem.cs` | 101 | 3,537 |
| `Scripts/Core/GameState.cs` | 164 | 6,255 |
| `Scripts/Core/Progress.cs` | 120 | 3,732 |
| `Scripts/Cinematics/CinematicDirector.cs` | 120 | 4,454 |
| `Scripts/Settings/Localization.cs` | 143 | 8,069 |
| `Resources/Story/chapter01.json` | 271 عقدة | 68,193 |

- **صفر** إشارات لرموز 4.4 في أي `.cs` (`SaveManager` · `PostCredit` · `CardPlayer` · `ExtrasCatalog` ·
  `MarkPostCredit` · `DebugContinue` · `phase4_4`) — البحث رجّع **لا شيء**.
- مفيش `Assets/_Project/Editor/Phase43Tests.cs` ولا `Phase44Tests.cs` ولا `SaveManager.cs`.
- موجود تقارير: `PHASE_4_3_ACCEPTANCE.md` (17,172 بايت) · `PHASE_4_3_ASSETS.md` · `tools/RUN_PHASE43.md`
  و`tools/RUN_PHASE44.md` (4,460 بايت — **وثيقة خطة تشغيل**، مش كود).
- `RECOVERY_STATUS.md` جوه الشجرة بيوثّق إن **بيئة التشغيل اتصفّرت** وإن الشجرة على حالة 4.2، وإن كود 4.3
  الجاري (AnimationClipBuilder + ١٠ كليبات · FacialExpressionSystem · باصات الصوت الأربعة ·
  تصليحات DialogueShotDirector · `ApplyAct` wiring · Phase43Tests) **كان في نسخة العمل بس واتمسح**.

---

## ٤) الأرشيف `THE_LAST_CONTRACT_v2.0.zip` — مش نسخة من الشجرة

- الحجم 98.2 ميجا · ٤٠٤ ملف · سلامة الأرشيف **سليمة**.
- ٣٧١ ملف مشترك، **١٧ منهم محتواهم مختلف**، وكل اختلاف لصالح الشجرة (نسخة الزيب **أصغر/أقدم**):

| ملف | الشجرة | الزيب | الفرق |
|---|---|---|---|
| `Resources/Story/chapter01.json` | 68,193 | 14,405 | **−53,788** |
| `Scripts/Bootstrap/GameBootstrap.cs` | 63,639 | 46,183 | −17,456 |
| `Scripts/World/HumanoidPuppet.cs` | 24,768 | 17,979 | −6,789 |
| `Scripts/UI/UIFactory.cs` | 9,574 | 5,938 | −3,636 |
| `Editor/TMPEssentials.cs` | 5,050 | 1,816 | −3,234 |
| `Scripts/UI/DialogueUI.cs` | 14,730 | 12,046 | −2,684 |
| `Scripts/World/CastDirector.cs` | 7,513 | 4,954 | −2,559 |
| … و١٠ ملفات تانية (CinematicDirector · DialogueShotDirector · ArabicFont · MobileControls · AdamBuilder · CharacterLibrary · LightingRig · ThirdPersonCameraRig · MenuSystem · manifest.json) | | | كلها أصغر في الزيب |

- **٣٣ ملف في الزيب مش في الشجرة**، منهم مهم جدًا: **٤ ملفات FBX للشخصيات** (`adam/man-base-character-rigged.fbx` ·
  `laila/woman-rigged.fbx` · `murad/elderly-man-rigged.fbx` · `yassin/man-rigged.fbx`) — الشجرة مستبعداهم
  عشان المساحة (موثّق في `WORKSPACE_NOTE.md`)، + ٣١ لقطة `Screenshots/chapter01/line_*.png` + `texture_manifest.json`.
- **١٠٤ ملف في الشجرة مش في الزيب**: تقارير 4.2/4.3، `RUN_PHASE43/44`، props FBX + تكستشراتها،
  `ArabicShaper.cs`/`ArabicTextBinder.cs`، `EnvironmentLibrary*`، `Screenshots/menu`, `VERIFICATION/`.
- الخلاصة: الزيب **مش باك أب لـ4.2 أو 4.3** — هو حالة أقدم (قبل تضخيم `chapter01.json`). قيمته الوحيدة
  الحقيقية: فيه الـ٤ موديلات FBX المفقودة من الشجرة.

---

## ٥) البحث عن Git / `0a7490f` / `phase4-3-final-r2`

| الفحص | النتيجة |
|---|---|
| `find / -xdev -name .git` (بره `/home/user` كذلك) | **صفر** |
| `find /home/user -name ".git*"` | `.github/` و`.gitignore` بس (مش repo) |
| ملفات git objects (`*.pack` · `packed-refs` · `HEAD`) | **صفر** |
| `grep -r "0a7490f"` في كل المساحة | ٦ ملفات — **كلها توثيق**: `PHASE_4_4_STATUS_AND_RECOVERY.md` · `4.4_RECOVERY_PACK/{00_READ_ME,01_STATUS_AND_RECOVERY,02_SEARCH_AND_BASELINE,PATCHES/00_ORDER}.md` · تعليق في `SaveManager.cs` |
| `grep -r "phase4-3-final-r2"` | نفس الملفات التوثيقية |
| Remote URL لمستودع المشروع | **مفيش** — `GITHUB_CI.md` بيشاور على `github.com/new` بس ⇒ الريبو **ما اتعملش/ما اترفعش** أصلًا |
| `git` مثبّت؟ | آه (2.47.3) — لكن **مفيش repo** يشتغل عليه |

**الاستنتاج:** الـcommit والتاج **مش قابلين للاسترجاع** من أي مصدر على الجهاز، ولأ من remote (لأنه مش موجود).

---

## ٦) ملفات 4.4 الأصلية

| الملف | الأصل | الموجود |
|---|---|---|
| `Scripts/Core/SaveManager.cs` | ٥٢٢ سطر (موثّق) | إعادة بناء ٥٨٠ سطر · 25,574 بايت — في الباكدج فقط |
| `Editor/Phase44Tests.cs` | نص كامل (اتعدّل في الجلسة) | ٧١٠ سطر · 35,206 بايت — مُعاد حرفيًا |
| `SaveController` · `Toast` · `CardPlayer` · `MenuModel` · `MenuSystem` · `ExtrasCatalog` · `AudioLevels` · `SettingsPersistence` · `ImportTmpEssentials` · `BuildAmiriFontAsset` · `DebugShaper` | — | موجودين في الباكدج (إعادة حرفية) |
| `CardSequence` · `PostCreditSequence` · `ChapterTwoTeaser` | — | موجودين، **بنية مؤكدة ونصوص الكروت مُعاد صياغتها** لا الأصلية |
| `GameBootstrap` 4.4 (النسخة اللي فيها `Debug*` + ربط القوائم/الكروت) | مفقودة | **مش موجودة** — فيه `PATCHES/01_GameBootstrap.md` (٣٩٩ سطر تعليمات) |
| `Localization` · `NarrativeRunner` · `CinematicDirector` · `GameState` · `Progress` · `SaveSystem` (نسخ 4.4) | مفقودة | **مش موجودة** — فيها ٥ ملفات patch |

---

## ٧) هل يمكن الاستعادة دون فقدان بيانات؟ — تفصيل

### مستحيل الاسترجاع (فقد نهائي محليًا)
1. **تاريخ Git بالكامل**: `2f0f1cf` (4.2) · `bf20bd3` (4.3) · `0a7490f` (4.3 r2) + تاجات `phase4-3-wip` / `phase4-3-final-r2`.
2. **كود 4.3 r2**: `AnimationClipBuilder` + ١٠ كليبات + `TLC_Character.controller` · `FacialExpressionSystem` ·
   نظام صوت بـ٤ باصات · تصليحات `DialogueShotDirector` (`SetRoom/ClampToRoom/ClearLineOfSight/SolveShotPosition/MakeShot`) ·
   `GameBootstrap.ApplyAct` + `DebugFocusPoint/DebugVisibleCastCount` · `Phase43Tests.cs` (٥٠ اختبار).
3. **نسخ 4.4 الأصلية**: `SaveManager` ٥٢٢ سطر · `GameBootstrap` المعدّل · نصوص الكروت الأصلية لما-بعد-النهاية والتيزر.
4. **بيئة التشغيل**: Unity 6000.0.28f1 · `Unity_lic.ulf` · `~/.cache/work/tlc` · Xvfb · لوجات `t44→t51` ·
   `Screenshots/phase4_3` (٢٥ لقطة) و`phase4_4` الأصلية.

### قابل للاسترجاع (موجود ومتحقق)
- شجرة **4.2** كاملة كأساس (٦٨ `.cs` + ١٩ Editor + `chapter01.json` بـ٢٧١ عقدة + المشاهد + الـPackages).
- **٤ موديلات FBX** للشخصيات — من الزيب فقط.
- ١٦ ملف 4.4 **مُعاد كتابتها** + ٧ ملفات patch + وصفة تشغيل + ٥ لقطات.
- تقارير القبول 4.2/4.3 r1 وتقارير الاستعادة.

### الطريقان الممكنان
| السيناريو | النتيجة | الفقد |
|---|---|---|
| **(أ)** رجوع لقطة/نسخة من مساحة العمل اللي فيها `.git` (أو ريبو مرفوع) | استعادة **كاملة** لـ4.3 r2 و4.4 | صفر |
| **(ب)** إعادة تطبيق الباكدج على شجرة 4.2 من غير اللقطة | **استعادة جزئية**: 4.4 شغّالة وظيفيًا (حفظ/قوائم/كروت) لكن مبنيّة على 4.2 ⇒ **فقد 4.3 r2 بالكامل** (كليبات/وجه/صوت/تصليحات كاميرا) + نصوص الكروت الأصلية | كبير في الطبقة البصرية والسردية |

**مفيش طريق تالت** بيحافظ على البيانات: أي «إعادة بناء» على 4.2 = السيناريو (ب) بكل فقدانه، وممنوع حسب تعليماتك.

### عوائق تقنية مؤكدة الآن
`dotnet` ✖ · `mono` ✖ · `csc` ✖ · Unity ✖ · `~/.cache` ✖ · `Xvfb` ✖ · `.ulf` ✖ ⇒ **لا ترجمة ولا تشغيل اختبارات ممكن**
في البيئة الحالية. `compile-check/` فيه `TLCCheck.dll` مترجمة سابقًا لكن أدوات الترجمة نفسها مش موجودة،
والباكدج مش مضاف لـ`csproj` أصلًا.

---

## ٨) قائمة الناقص (بالمسار المتوقع)

| # | المطلوب | المسار المتوقع | الحالة |
|---|---|---|---|
| 1 | `.git` (objects + refs + tags) | `THE_LAST_CONTRACT/.git/` | **ناقص** |
| 2 | شجرة 4.3 r2 | — | **ناقصة** (الشجرة الحالية 4.2) |
| 3 | `SaveManager.cs` الأصلي (٥٢٢) | `Assets/_Project/Scripts/Core/SaveManager.cs` | ناقص (بديل في الباكدج) |
| 4 | `GameBootstrap.cs` بنسخة 4.4 | `…/Scripts/Bootstrap/GameBootstrap.cs` | ناقص (patch فقط) |
| 5 | `Phase43Tests.cs` | `Assets/_Project/Editor/Phase43Tests.cs` | **ناقص** |
| 6 | `AnimationClipBuilder` + ١٠ كليبات | `Assets/_Project/Editor/` | **ناقص** |
| 7 | `FacialExpressionSystem` | `Assets/_Project/Scripts/` | **ناقص** |
| 8 | نصوص كروت ما-بعد-النهاية والتيزر الأصلية | — | ناقصة (بديل مُعاد صياغته) |
| 9 | `Screenshots/phase4_3` · `phase4_4` الأصلية | — | **ناقصة** (٥ لقطات بس نجت) |
| 10 | ٤ ملفات FBX للشخصيات | `Assets/_Project/Resources/Characters/*` | **موجودة في الزيب** — تُستخرج منه |
| 11 | Unity Editor + License + Xvfb | `~/.cache/unity` · `.ulf` | **ناقصة** |

---

## ٩) ملاحظات أمانة

- **لم يُعدَّل أي ملف**: العملية كلها `find` · `ls` · `wc` · `md5sum` · `unzip -t/-l/-Z1` · `grep` + قراءة بالـPython.
  مفيش `mv`/`rm`/`cp`/كتابة على أي مسار موجود. (ملفات مساعدة مؤقتة اتعملت في `/tmp` بس.)
- التقرير **مش إعلان استعادة ولا اكتمال 4.4**: مفيش سطر كود من الباكدج اتترجم أو اتشغّل هنا.
- المقارنة بين الزيب والشجرة اتعملت بالبصمة (`sha256`) على ٣٧١ ملف مشترك — مش تقدير بالأحجام.
