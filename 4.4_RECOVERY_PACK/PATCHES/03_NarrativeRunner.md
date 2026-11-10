# PATCH — `Assets/_Project/Scripts/Narrative/NarrativeRunner.cs`

## ١) لازم `Story` تبقى متاحة للقراءة

```csharp
public StoryGraph Story { get; private set; }     // كانت private
```

`SaveController.NodeLabel()` بيقرأ منها نص العقدة الحالية لعرضه في خانة الحفظ،
و`ExtrasCatalog` مش محتاجها بس الاختبارات بتستخدمها للمقارنة.

## ٢) حدث تغيير العقدة

```csharp
public event System.Action<string> NodeChanged;

void SetNode(string id)
{
    _state.CurrentNodeId = id;
    NodeChanged?.Invoke(id);
    ... منطق 4.3 ...
}
```

`GameBootstrap` بيسمع للحدث ده عشان يحفظ تلقائي كل ١٢ سطر وبعد كل مشهد.

## ٣) `LoadAt` — استعادة بدون `LoadScene`

```csharp
/// <summary>يرجّع الحكاية لمشهد/عقدة محددة بنفس الـbootstrap (مش بيعيد تحميل المشهد).</summary>
public void LoadAt(string nodeId)
{
    if (Story == null || string.IsNullOrEmpty(nodeId)) return;
    var node = Story.Get(nodeId);
    if (node == null) { Debug.LogWarning($"[TLC/Narrative] العقدة '{nodeId}' مش موجودة"); return; }

    _blocked = false;
    _waitingForChoice = false;
    SetNode(nodeId);          // بيبني الأسطر ويعرض أولها
    Debug.Log($"[TLC/Narrative] ✔ LoadAt {nodeId} ({node.lines?.Count ?? 0} سطر)");
}
```

**مهم:** الاستعادة ما لازمش تنادي `SceneManager.LoadScene("Play")` — ده بيهدّ الـbootstrap
وبيضيّع الحالة اللي لسه محمّلة، وكمان بيعلّق السويتة (شوف `Errors` في تقرير الحالة).

## ٤) الحفظ التلقائي كل ١٢ سطر

`GameBootstrap.Update` بيزوّد عدّاد أسطر ويبعت `Saves.Autosave("line-tick")` عند ١٢.
مفيش تعديل مطلوب في الـRunner نفسه، بس `NodeChanged` بيصفّر العدّاد لضمان نقطة آمنة عند حدود المشهد.
