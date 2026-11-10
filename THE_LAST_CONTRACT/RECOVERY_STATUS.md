# حالة الاستعادة — بعد تصفير بيئة التشغيل

**التاريخ:** 2026-11-10 · **آخر حالة موثّقة قبل التصفير:** جولة التصليح `t51` (من `bf20bd3` / tag `phase4-3-wip`)

---

## ١) اللي حصل بالظبط

بيئة التشغيل (الكونتينر) اتصفّرت بين جولتين. الملفات اللي بره `/home/user` **مش محفوظة**،
واللي جوه `/home/user` محفوظ بس على حالة أقدم من شغل 4.3 الجاري.

| المسار | الحالة |
|---|---|
| `/opt/unity/editor/…` (Unity 6000.0.28f1) | **اتمسح** — محرر Unity مش موجود |
| `~/.local/share/unity3d/Unity_lic.ulf` (الترخيص) | **اتمسح** — مفيش ترخيص ⇒ مفيش `-batchmode` |
| `/opt/tlc/work/THE_LAST_CONTRACT/` (نسخة العمل) | **اتمسح** |
| `/opt/tlc/*.log` (لوجات الجولات `t44`→`t51`) | **اتمسح** |
| `.git` (commits `2f0f1cf` · `bf20bd3` · tags) | **اتمسح** — مفيش تاريخ |
| `/home/user/THE_LAST_CONTRACT/` | موجودة، لكن على **شجرة 4.2** (68 ملف `.cs`) + تقارير 4.3 |
| `Screenshots/phase4_3/` (25 لقطة + `phase43_report.txt`) | **اتمسحت** |

### اللي ضاع من كود 4.3 (كان بيتبني في نسخة العمل بس)

1. `AnimationClipBuilder` + الـ10 كليبات (Idle/Walk/Run/SitDown/SitIdle/StandUp/Talk/Gesture/TurnL/TurnR)
   + `TLC_Character.controller` الجديد بمساحة العضلات.
2. رفعة `HumanoidPuppet` لتعمل بالكليبات (`ClipDriven`) + إصلاح `SetSeated` (شيل خفض
   `_modelRoot` اليدوي) + ياردات القياس (`HipHeight`/`FootHeight`/`MeshBottom`/`RootHeight`).
   **النسخة اللي في `/home/user` لسه القديمة (إجرائية، بلا كليبات).**
3. `FacialExpressionSystem` + نظام الصوت بـ4 باصات (Master/Music/Dialogue/SFX) + المحيطات.
4. `DialogueShotDirector` بجولة التصليح (`SetRoom`/`ClampToRoom`/`ClearLineOfSight`/
   `SolveShotPosition`/`MakeShot`/`DebugPlayShot`) — **النسخة في `/home/user` قديمة**.
5. `GameBootstrap.ApplyAct` wiring + `DebugFocusPoint`/`DebugVisibleCastCount`/accessors.
6. `Assets/_Project/Editor/Phase43Tests.cs` (50 اختبار + `Skip()` + ملخّص ثلاثي).

---

## ٢) تشخيص محفوظ من آخر جولة (`t51`) — لازم يتعامل معاه في إعادة البناء

### أ) أرقام الأوضاع كانت متطابقة قاعد/واقف ⇒ القياس كان بيسجّل حالة مجمّدة

```
آدم واقف (كليب): حوض=0.51م · قدم=-1.16م · أقل نقطة ميش=-2.43م · جذر=0.02م
آدم قاعد (كليب): حوض=0.51م · قدم=-1.16م · أقل نقطة ميش=-2.43م · جذر=0.02م   ← نفس الأرقام بالحرف
```

**الاستنتاج:** الأرقام متطابقة تمامًا ⇒ الـAnimator ما تقدّمش ولا الوضعية اتغيّرت بين
القراءتين ⇒ قسم الأوضاع كان بيشتغل في مشهد **متجمّد** (`Time.timeScale = 0` من قائمة
أو مشهد لسه مابُنيش)، أو `SetSeated` مش واصل للـAnimator. + `MeshBottom = −2.43م` مع
`RootHeight = 0.02م` دليل إن `Renderer.bounds` للميش المسلوّح **مش موثوق** كمرجع
للأرض ⇒ **ممنوع** استخدامه في اختبار «الأقدام على الأرض» ولا في `SettleFeet`
(الاعتماد على عظمة القدم + `Physics.Raycast` للأرض).

### ب) لقطات الحوار كانت الكاميرا على حدود الغرفة بعيد 191م عن الهدف

```
[wide] كاميرا=(194.0,2.2,-1.7) · مسافة للهدف=191.57م · محجوب=True (Cafe_WallW)
```

المواقع نفسها مبنية على محور X بمسافات 200م (`OriginOf`: cafe=200 · newsroom=400 …)،
فحد الغرفة لكافيه = 200 − 6.5 = **194**. والهدف كان عند ≈ 2.4 ⇒ **الممثلين كانوا لسه في
الأبارتمنت (الأصل) وحدود الغرفة المرسلة للمخرج كانت بتاعة الكافيه**. يعني
`ApplyAct` بعت `SetRoom` صح، لكن الانتقال/التليبورت للممثلين ما حصلش قبل اللقطات.
**الإصلاح المطلوب:** المخرج يحسب صندوق التقييد من **موضع الممثل نفسه** (مع تقاطع مع
حدود الغرفة كسقف أعلى)، والاختبار يعمل **assert** إن اللاعب جوه حدود الغرفة قبل أي لقطة،
وإلا يسجّل FAIL فوري بالسبب بدل 191م.

---

## ٣) خطة الاستعادة (بالترتيب)

| # | الخطوة | الحالة |
|---|---|---|
| 1 | `git init` في `/home/user/THE_LAST_CONTRACT` + commit أساس للشجرة الباقية | ✅ اتعمل |
| 2 | إعادة تنزيل Unity 6000.0.28f1 (4.3GB) لمكان **مؤقت** (`~/.cache`) لأن `/home/user` بيتحفظ بس لحد ~128MB | 🔄 جاري |
| 3 | توليد ملف ترخيص جديد `.alf` (مربوط بـMachineID الجديد) وتسليمه للمستخدم | ⏳ بعد فك الضغط |
| 4 | تركيب `.ulf` اللي المستخدم بيرجّعه | ⏳ **مستني المستخدم** |
| 5 | إعادة بناء كود 4.3 جوّه `/home/user` (مش في نسخة عمل مؤقتة) — كل ملف بيتعمل commit فورًا | ⏳ |
| 6 | إصلاح الأولوية 1 (قعود) والأولوية 2 (لقطات) بالتشخيص اللي فوق | ⏳ |
| 7 | إعادة كل اختبارات 4.3 (50) + أدلة بصرية | ⏳ |

### قاعدة جديدة للمنع من التكرار
- **كل** كود/تقارير 4.3 بيتكتب في `/home/user/THE_LAST_CONTRACT` أول بأول + commit.
- نسخة العمل في `/tmp` أو `~/.cache` تبقى **مرآة** بس (بتتنسخ من `/home/user` مش العكس).
- Unity بيتركّب في `~/.cache/unity` (مستثنى من الـsnapshot عمدًا) — يعني **أي تصفير جديد
  بيعني إعادة تنزيل تلقائي**، عشان كده الكود لازم يبقى مستقل عن المحرر.
- الترخيص لازم يترجّع من المستخدم بعد أي تصفير (مربوط بالـMachineID).

---

## ٤) اللي **مش** هيتقال في التقرير النهائي

- مفيش إعلان إن 4.3 خلصت. آخر رقم موثّق قبل التصفير: **49/50** ولقطات فيها عيوب مفتوحة.
- مفيش أي رقم اختبار جديد هيتكتب قبل ما Unity يشتغل تاني **فعليًا** ويدّي مخرج حقيقي.

---

## ٥) سجل إعادة البناء (بيتحدّث مع كل خطوة)

| التاريخ | الملف | إيه اللي اتعاد بناؤه |
|---|---|---|
| 2026-11-10 | `Assets/_Project/Editor/AnimationClipBuilder.cs` | **جديد** — 10 كليبات في مساحة العضلات (`HumanTrait.MuscleName`) + `TLC_Character.controller` بحالات Idle/Walk/Run/SitDown/SitIdle/StandUp/Talk/Gesture/TurnL/TurnR وبارامتر `Sitting`. مفيش منحنيات RootT/RootQ (Root Motion مقفول). |
| 2026-11-10 | `Assets/_Project/Scripts/World/HumanoidPuppet.cs` | `ClipDriven` + `HipHeight`/`FootHeight`/`MeshBottom`/`RootHeight` + `UpdateSeatOffset` (إنزال جذر الموديل بمقدار **مقاس من عظمة القدم** لكل شخصية) + `SetSeated(bool)` بيبعت `Sitting` للـAnimator بدل خفض يدوي ثابت + `SettleFeet` بيسكت في مسار الكليبات. |
| 2026-11-10 | `Assets/_Project/Scripts/Cinematics/DialogueShotDirector.cs` | `SetRoom`/`ClearRoom`/`InsideRoom`/`ClampToRoom`/`ClearLineOfSight`/`SolveShotPosition` + `MakeShot`/`DebugPlayShot` لأنواع Wide/OTS/Reverse/CloseUp/TwoShot. صندوق التقييد بقى **حوالين الممثل نفسه** (مش مركز الغرفة) + سحب الكاميرا لو حيطة قطعت خط النظر + `height()` بقى من عظمة الراس مش `Renderer.bounds`. |

**لسه متبقّي:** `FacialExpressionSystem` · ربط `GameBootstrap.ApplyAct` بالغرفة/القعود · `Phase43Tests` (50 اختبار) · الأدلة البصرية.

**القياس الجديد للاختبارات (بدل Renderer.bounds اللي أدّى −2.43م):**
- الحوض: `HipHeight` من عظمة `Hips` بالنسبة لجذر الشخصية.
- الأقدام: `FootHeight` من عظمتَي `LeftFoot`/`RightFoot` — هي اللي بيستخدمها `UpdateSeatOffset`.
- `MeshBottom` بقى **معلومة فقط**، ممنوع يستخدم كحكم PASS/FAIL.
