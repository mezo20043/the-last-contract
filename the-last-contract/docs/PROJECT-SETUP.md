# فتح المشروع في Unity | Project Setup

> اتبع بالترتيب. الوقت المتوقع: ٣٠–٤٥ دقيقة.

---

## 1. إنشاء المشروع

1. نزّل **Unity 6 LTS** من Unity Hub.
2. أنشئ مشروع جديد بقالب **Universal 3D (URP)**.
3. الاسم: `TheLastContract`. المكان: على SSD لو ممكن.

## 2. إعدادات المشروع

`Edit → Project Settings`:

| الصفحة | الإعداد |
|---|---|
| Player → Other Settings | **Active Input Handling: Both** (عشان QTEs تستخدم Touch والـ UI يستخدم Pointer) |
| Player → Other Settings | Color Space: **Linear** |
| Player → Other Settings | API Compatibility Level: **.NET Standard 2.1** |
| Player → Android | Minimum API: 26، Target API: أحدث |
| Player → Android | **Split Application Binary: ON** (AAB + Play Asset Delivery) |
| Player → iOS | Minimum: iOS 15 |
| Quality | **Single quality level** — لأن QualityTier هي اللي بتتحكم، مش إعدادات Unity |
| Graphics | Scriptable Render Pipeline: URP Asset ✓ |
| Editor | Asset Serialization: **Force Text** (ضروري لـ Git) |

## 3. الحزم

`Window → Package Manager` — نزّل:

```
Cinemachine         (3.x)
Timeline
Addressables
TextMeshPro         (غالباً مثبت تلقائياً — لازم تستورد Essentials)
Unity Recorder
Animation Rigging
2D Sprite / UI
```

## 4. نسخ الملفات

انسخ من الريبو:

```
unity/Assets/Scripts/   →  Assets/Scripts/
unity/Assets/Story/     →  Assets/Story/
```

انتظر الـ compile. لازم صفر أخطاء.

## 5. ربط نظام السرد

1. `Create → TLC → Story Asset` → سمّيه `Ep01`.
2. ارمي عليه ملف `ep01-the-contract.json` في خانة **Json**.
3. أنشئ GameObject فاضي اسمه `_Narrative` → ضيف عليه `StoryRunner`.
4. اربط `Story Asset` = Ep01.

## 6. بناء واجهة الحوار (أبسط نسخة)

```
Canvas (Screen Space - Overlay, Scale With Screen Size 1080×1920)
├── Background        (Image, أسود 60%)
├── SpeakerLabel      (TMP, أعلى الشمال)
├── BodyText          (TMP, مساحة كبيرة, RTL)
├── TapHint           ("اضغط للمتابعة")
└── ChoicePanel       (RectTransform + VerticalLayoutGroup)
    └── ChoiceButton (Button + TMP_Text) ← ده الـ Prefab
```

- ضيف `DialoguePresenter` على كائن `DialoguePanel` (لازم يكون فيه CanvasGroup).
- ضيف `ChoicePresenter` على `ChoicePanel` (فيه CanvasGroup).
- اربط المراجع.

## 7. النص العربي — مهم جداً

1. استورد ملف خط عربي (مقترح: **Noto Naskh Arabic** أو **Cairo** من Google Fonts).
2. اعمل له `Font Asset` من `Window → TextMeshPro → Font Asset Creator`.
3. **Setting: Right-to-Left — مفعّل** (في الـ Font Asset).
4. على الـ TMP_Mesh: `Horizontal Alignment: Right`, `Text Component Settings → Extra Settings → Direction` غيرها لو محتاج.
5. لو الأرقام بتظهر مقلوبة: استخدم الضوابط `\u200F` (RLM) قبل الرقم.

## 8. تحكم اللاعب

```
── UI Canvas (Overlay)
    ├── JoystickArea   (Image شفافة) → VirtualJoystick (background + handle)
    └── LookArea       (Image شفافة، نص الشاشة اليمين) → LookPad
── Player
    ├── CharacterController (height 1.8, radius 0.3)
    ├── PlayerMotor  → اربط moveStick + cameraTransform
    └── Animator
── CameraPivot (يتبع Player) → Camera (Cinemachine Camera)
    └── PlayerCameraRig → اربط LookPad + pivot
```

**تلميح أداء:** `LookArea` لازم تغطي الجزء اليمين بس، و`JoystickArea` الجزء الأيسر — عشان الإيد اليمين تسحب الكاميرا والإيد الشمال تحرك من غير تعارض.

## 9. أول تجربة تشغيل

1. اضغط Play.
2. لازم تشوف سطر `open_1` وأزرار القرارات.
3. لو ظهرت أخطاء "العقدة مش موجودة" → اسم `entry` في JSON مختلف عن id أول عقدة.

## 10. السينمائيات

- اعمل Timeline جديد لكل مشهد سينمائي.
- ضيف `PlayableDirector` + `SignalEmitter` في آخر التراكات.
- `CinematicBridge` بيستدعى من `StoryRunner.OnEvent` لما الـ action = `cinematic`.

```csharp
runner.OnEvent += (action, param) =>
{
    if (action == "cinematic")
    {
        var clip = cinematics[param];        // Dictionary<string, CinematicBridge>
        clip.OnCinematicFinished += runner.ResumeAfterEvent;
        clip.Play();
    }
};
```

---

## الأخطاء الشائعة

| المشكلة | السبب |
|---|---|
| النص العربي بيظهر خطوط مقلوبة | اتجاه الـ TMP غلط أو الخط مش بيدعم العربية |
| الخيارات مش بتظهر | `ChoicePresenter` مش موصول في `OnEnable` أو الـ CanvasGroup `blocksRaycasts=false` |
| اللعبة بتعمل crash عند التحميل | `Cardboard`-style — تأكد إن Addressables groups متسجلة |
| ٣٠ إطار ثابت حتى على جهاز قوي | `QualityTier` مش في المشهد |
