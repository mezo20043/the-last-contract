# PHASE 4.3 — سجل الأصول الفعلية (أنيميشن + صوت)

> كل اللي تحت **اتولّد داخل المشروع** (مفيش أصول خارجية جديدة اتحمّلت في 4.3) —
> الكليبات في مساحة العضلات (Humanoid Muscle Space) والأصوات مولّدة أصلياً بـPython
> (numpy + wave) عشان مساحة القرص (كانت 1.2GB حرة).

---

## ١) كليبات الأنيميشن (10 كليبات — `Assets/_Project/Resources/Animators/`)

| الكليب | المدة | حلقة | المحتوى | الحالة |
|---|---|---|---|---|
| `anim_idle.anim` | 4.0ث | ✔ | تنفّس + ميل صدر + راس + دراعات متهدلة | مستخدم — الحالة الافتراضية |
| `anim_walk.anim` | 1.0ث | ✔ | دورة مشي كاملة: فخذ/ركبة/قدم متبادلين + دراعات معاكسة + ميل جسم + صعود/هبوط | مستخدم — Speed > 0.15 |
| `anim_run.anim` | 0.72ث | ✔ | نفس الدورة بمدى أكبر وسرعة أعلى وميل جسم أكتر | مستخدم — IsRunning |
| `anim_sit_down.anim` | 0.9ث | ✘ | انتقال من وقوف لقعود (فخذ/ركبة 90° + هبوط حوض 0.40م) | مستخدم — Sitting = true |
| `anim_sit_idle.anim` | 5.0ث | ✔ | قعود ثابت + تنفّس + دراعات على الحضن | مستخدم — بعد SitDown |
| `anim_stand_up.anim` | 0.85ث | ✘ | عكس SitDown | مستخدم — Sitting = false |
| `anim_talk.anim` | 3.2ث | ✔ | إيماءات كلام (راس + إيد يمين) | مستخدم — Talking |
| `anim_gesture.anim` | 1.4ث | ✘ | رفع إيد يمين + إشارة | مستخدم — Trigger |
| `anim_turn_l.anim` | 0.55ث | ✘ | ميل/لفّة نصفي لليسار | **موجود ومستخدمش في الـController** (اللف بالكود) |
| `anim_turn_r.anim` | 0.55ث | ✘ | نفس الحالة لليمين | **موجود ومستخدمش في الـController** |

**تفاصيل تقنية:**
- كل المنحنيات من نوع **Muscle Curves** (`EditorCurveBinding` مع `typeof(Animator)` واسم العضلة
  من `HumanTrait.MuscleName`) — يعني بتشتغل على أي موديل Humanoid بنفس الـRig.
- **مفيش Root Motion**: مفيش منحنيات `RootT.x/z` ولا `RootQ`، و`Animator.applyRootMotion=false`
  بيتظبط وقت الربط. الحركة الأفقية مسؤولية `CharacterController` بس.
- `RootT.y` مستخدم بس لصعود/هبوط الجسم في دورة المشي ولنزول الحوض في القعود.

## ٢) الـAnimator Controller

`Assets/_Project/Resources/Animators/TLC_Character.controller`

**Parameters:** `Speed` (float) · `IsRunning` (bool) · `Sitting` (bool) · `Talking` (bool) ·
`Grounded` (bool) · `Gesture` (trigger)

**States:** Idle · Walk · Run · Talk · Gesture · SitDown · SitIdle · StandUp

**Transitions:**
| من | إلى | الشرط |
|---|---|---|
| Idle | Walk | Speed > 0.15 |
| Walk | Idle | Speed < 0.12 |
| Walk | Run | IsRunning = true |
| Run | Walk | IsRunning = false |
| Idle | Talk | Talking = true |
| Talk | Idle | Talking = false |
| Idle | Gesture | Trigger Gesture |
| Gesture | Idle | Exit Time 0.92 |
| Idle | SitDown | Sitting = true |
| SitDown | SitIdle | Exit Time 0.95 |
| SitIdle | StandUp | Sitting = false |
| StandUp | Idle | Exit Time 0.95 |

## ٣) الأصوات (مولّدة أصلياً — 22,050Hz مونو)

### محيط لكل موقع (`Resources/Audio/Ambience/`)
| الملف | الحجم | الموقع |
|---|---|---|
| `cafe_murmur_loop.wav` | 215KB | مقهى مدينة نصر — ضجيج كلام + أكواب |
| `newsroom_office_loop.wav` | 215KB | مكتب الصحافة — هم مكتبي + تليفون بعيد |
| `archive_quiet_loop.wav` | 215KB | الأرشيف — سكون + قطرات بعيدة |
| `police_hum_loop.wav` | 215KB | التحقيقات — طنين فلورسنت |
| `office_city_loop.wav` | 215KB | مكتب يوسف — مدينة بعيدة + تكييف |
| `apartment_hum_loop.wav` | موجود من قبل | شقة آدم |

### صوت الحوار (`Resources/Audio/Voice/`) — **بديل مؤقت**
| الملف | الحجم | ملاحظة |
|---|---|---|
| `voice_murad.wav` · `voice_laila.wav` · `voice_yassin.wav` · `voice_adam.wav` · `voice_narrator.wav` | 60–77KB | **بليبات صوت مولّدة (نغمات كلام رمزية)، مش تعليق صوتي مسجّل**. البنية جاهزة: أي ملف بنفس الاسم بيتشغّل مكانه. |

### مؤثرات (`Resources/Audio/SFX/`) — من قبل + مستخدمة دلوقتي
`door_open` · `door_close` · `footstep_wood_01..04` · `footstep_concrete_01..04` ·
`paper_rustle` · `phone_vibrate` · `heartbeat` · `whoosh_transition` · `ui_click` · `ui_back` ·
`ui_hover` · `ending_sting`

### موسيقى (`Resources/Audio/Music/`)
`title_theme` · `act_tension` · `act_resolve` · `chapter_sting`

**إجمالي الصوت:** 12MB (منها 1.4MB مضافة في 4.3).
