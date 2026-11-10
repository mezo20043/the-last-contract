# PATCH — `Assets/_Project/Scripts/Cinematics/CinematicDirector.cs`

## المشكلة اللي وقفت كل الحفظ

```csharp
// 4.3 — غلط
public bool IsPlaying => _isPlaying || _playing != null;
```

لو الانتقال اتقطع (مشهد اتغيّر / اللاعب داس تخطّي / OnEnding اتنده والعقدة اتغيّرت)،
`_playing` بيفضل مش `null` و`_isPlaying` بيفضل `true` — فالنتيجة إن `SaveController`
شايف «حالة غير مستقرة» للأبد وكل حفظ يدوي بيترفض برسالة «مينفعش تحفظ دلوقتي».

## الإصلاح

```csharp
public bool IsPlaying => _isPlaying && _playing != null;

/// <summary>تنظيف أي انتقال اتقطع (بيتنادى من OnDisable/OnEnable ومن الحفظ).</summary>
public void CleanupAborted()
{
    if (_isPlaying && _playing == null)
    {
        Debug.LogWarning("[TLC/Cine] انتقال كان مرفوع من غير مشغّل — اتنضّف");
        _isPlaying = false;
    }
    if (!_isPlaying && _playing != null)
    {
        Debug.LogWarning("[TLC/Cine] مشغّل متعلّق من غير تشغيل — اتنضّف");
        _playing = null;
    }
}

void OnEnable()  { CleanupAborted(); }
void OnDisable() { CleanupAborted(); _isPlaying = false; _playing = null; }
```

## تحقق

بعد التشغيل، الجملة دي **مايصحّش** تظهر أكتر من مرتين في اللوج:

```
TLC/Cine] انتقال كان مرفوع من غير مشغّل
```

ولو ظهرت مع كل حفظ ⇒ الإصلاح مش متطبّق.
