# ترتيب التطبيق على لقطة `0a7490f`

هذا المجلد **مش كود بيتنسخ أعمى** — دي توجيهات دقيقة (patch instructions) للتعديلات اللي
اتعملت في 4.4 على الملفات الموجودة من 4.3. كل ملف هنا بيقول: الملف، والمكان، والقاعدة، والكود.

| # | الملف | نوع التعديل | لازم لوحده؟ |
|---|---|---|---|
| 0 | `Scripts/Core/SaveManager.cs` | ملف جديد (يحل محل محتوى `SaveSystem` القديم كطبقة عليا) | ✅ |
| 1 | `Scripts/Core/GameState.cs` | DTO + `ToDto/FromDto` + `Stats` عامّ | ✅ |
| 2 | `Scripts/Core/Progress.cs` | `HasScene/HasCinematic/HasSeenPostCredit/InjectForTests` | ✅ |
| 3 | `Scripts/Core/SaveSystem.cs` | ثوابت الخانات القديمة تفضل (توافق) + توجيه للـSaveManager | ⚠️ |
| 4 | `Scripts/Settings/Localization.cs` | `LoadHook/SaveHook` + `LoadPersisted` + مفاتيح 4.4 | ✅ |
| 5 | `Scripts/Narrative/NarrativeRunner.cs` | `Story` عام + `OnNodeChanged` + `LoadAt` | ✅ |
| 6 | `Scripts/Cinematics/CinematicDirector.cs` | `IsPlaying` مصحّح + `CleanupAborted` | ✅ |
| 7 | `Scripts/Bootstrap/GameBootstrap.cs` | الربط الكامل: حفظ/قوائم/كروت + `Debug*` | ✅ |
| 8 | `Packages/manifest.json` | مفيش إضافة حقيقية (TMP/ugui من 4.3) | ➖ |
| 9 | `Editor/Phase44Tests.cs` | ملف جديد — سكربت القبول | ✅ |

## الترتيب الإلزامي

1. `SaveManager` + `CardSequence`/`PostCreditSequence`/`ChapterTwoTeaser` + `Toast`/`CardPlayer`/`ExtrasCatalog`/`AudioLevels`/`SettingsPersistence` (ملفات جديدة، مش بتعتمد على بعض بترتيب).
2. `GameState` DTO ← `SaveManager` بيعتمد عليه.
3. `Progress` ← `SaveManager` + `ExtrasCatalog`.
4. `SaveSystem` (توافق) ← `Phase44Tests` بينادي `SaveSystem.ManualSlotA/B/C`.
5. `Localization` ← القوائم بتقرأ مفاتيح 4.4.
6. `NarrativeRunner` ← `SaveController.NodeLabel`.
7. `CinematicDirector` ← من غيره كل حفظ يدوي هيترفض.
8. `GameBootstrap` ← آخر خطوة، فيه كل الربط.
9. `Phase44Tests` + `ImportTmpEssentials` + `BuildAmiriFontAsset`.

## تحقق سريع بعد كل خطوة

```bash
grep -c "Compilation failed" ~/.cache/tmp/compile.log     # لازم 0
```

وبعد الخطوة ٨ تحديداً:

```bash
grep -n "DebugRunnerReady\|DebugManualSave\|DebugContinue\|DebugCards\|DebugSuppressSceneChange" \
     ~/.cache/work/tlc/Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs | wc -l
# المتوقع: 6+ (كل عضو موجود)
```

## فخّان معروفان

1. **`CinematicDirector.IsPlaying` بيفضل `true` للأبد** لو الاتنين (`_isPlaying` و `_playing`) مش متأكدين — النتيجة: «مينفعش تحفظ دلوقتي: حالة غير مستقرة» على كل حفظ. راجع `04_CinematicDirector.md`.
2. **`-batchmode` بيمنع اللقطات** (الـGPU بيبقى ناقص) — التشغيل بـ`Xvfb` على `:99` و`-force-glcore` (شوف `tools/RUN_PHASE44.md`).
