#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
مولّد الفصلين الثاني والثالث لـ THE LAST CONTRACT.
بيبني JSON ويتحقق من كل الروابط.
"""
import json, os, sys

N = []
def L(id, text, speaker="", next=None, act=None, **kw):
    n = {"id": id, "type": 0, "speaker": speaker, "text": text}
    if next: n["next"] = next
    if act: n["act"] = str(act)
    for k in ("onEnter", "require", "elseNext"):
        if kw.get(k): n[k] = kw[k]
    N.append(n); return id

def E(stats=None, flags=None):
    d = {}
    if stats: d["stats"] = [{"key": k, "add": v} for k, v in stats.items()]
    if flags: d["setFlags"] = flags
    return d

def C(id, text, opts, act=None):
    n = {"id": id, "type": 1, "speaker": "", "text": text, "options": opts}
    if act: n["act"] = str(act)
    N.append(n); return id

def O(text, nxt, eff=None, req=None, dec=None):
    o = {"text": text, "next": nxt}
    if eff: o["effect"] = eff
    if req: o["require"] = req
    if dec: o["decisionId"] = dec
    return o

def END(id, eid, title, body):
    N.append({"id": id, "type": 4, "endingId": eid, "endingTitle": title,
              "endingBody": body, "text": "", "speaker": "", "act": "end"})

def S(key, op, val): return {"stats": [{"key": key, "op": op, "value": val}]}
def AND(*cs):
    out = {"allFlags": [], "stats": []}
    for c in cs:
        out["allFlags"] += c.get("allFlags", [])
        out["stats"] += c.get("stats", [])
    return out

# ══════════════════════════════════════════════════════════════
#  الفصل الثاني — البيت القديم
# ══════════════════════════════════════════════════════════════

L("ch2_from_ch1_a", "مراد مشي قدامك، ودخل البيت الأول. بيت العيلة في العباسية اللي أبوكم سابه من ٢٠١٨، ومحدش رجع له من ساعتها.", act=1, next="ch2_01")
L("ch2_from_ch1_b", "الصوت على التليفون قال عنوان، وأنا روحت. بيت العيلة في العباسية. الباب مفتوح ومحدش جوه.", act=1, next="ch2_01")
L("ch2_from_ch1_c", "الراجل اللي كان واقف تحت عمود النور مشي وأنا مشيت وراه. مشي لحد بيت العيلة في العباسية، ودخل.", act=1, next="ch2_01")

L("ch2_01", "الفجر لسه مطلعش. البيت ساكت سكوت حاجة متكنّسة ومغطّاة بالملاءة.\nآدم ماسك مفتاح عمره ما استخدمه.", act=2, next="ch2_02")
L("ch2_02", "قبل ما يفتح الباب، سمع صوت جوه. صوت ست. صوت بتاعت اللي بيقول الحقيقة عشان مسؤولة.", act=2, next="ch2_laila_1")

L("ch2_laila_1", "أنا عارفة إنك جاي.\nليلى قالت، ووقفت في نص الصالة مش بتتحرك.", speaker="laila", act=2, next="ch2_laila_2")
L("ch2_laila_2", "الحاجة الوحيدة اللي أبوكم سابها هنا هي الورق.\nغير كده البيت ده مقبرة.", speaker="laila", act=2, next="ch2_c1")

C("ch2_c1", "ليلى واقفة قصادك. عندك تلات طرق تدخل بيهم البيت.", [
  O("«أنا عارف اللي أبوكم عمله. ومش هكدب عليكي.»", "ch2_l_truth", E({"Trust_Laila": 18, "Family_Truth": 8}), dec="CH2_TRUTH_LAILA"),
  O("«أنا جاي أدوّر على ياسين. وانت لازم تساعديني.»", "ch2_l_help", E({"Trust_Laila": 8, "Trust_Yassin": 6}), dec="CH2_ASK_HELP"),
  O("«مفيش حاجة مذكورة في ورق. أنا جاي أشفط البيت.»", "ch2_l_lie", E({"Trust_Laila": -12, "Organization_Suspicion": -5}), dec="CH2_LIE_LAILA"),
], act=2)

L("ch2_l_truth", "ليلى سندت على الحيطة. مش من الحزن — من الإنهاك.\n«أنا قاعدة هنا أربع سنين بستنى حد يقولي إني مش مجنونة.»", speaker="laila", act=2, next="ch2_safe_1")
L("ch2_l_help", "«أنا مش هساعدك على أخوك. أنا هساعدك على أبوكم — وده حاجة تانية خالص.»", speaker="laila", act=2, next="ch2_safe_1")
L("ch2_l_lie", "ليلى بصت لك بصّة واحدة، وبعدها مشيت عند الشباك وسابتلك البيت.\nالكذبة الأولى في البيت ده بتتقفل زي باب.", speaker="laila", act=2, next="ch2_safe_1",
  onEnter=E({"Family_Truth": -4}))

L("ch2_safe_1", "في الحجرة التانية، في الحيطة اللي ورا الدولاب، في خزنة حديد صغيرة. الباب مش مقفول — ومتساب مفتوح بالقصد.", act=2, next="ch2_c2")

C("ch2_c2", "الخزنة مفتوحة. جواها ورق.", [
  O("أفتح الخزنة وأقرا.", "ch2_letters", E({"Family_Truth": 20, "Yusuf_Connection": 10}), dec="CH2_OPEN_SAFE"),
  O("أسأل ليلى الأول إيه ده.", "ch2_ask_laila", E({"Trust_Laila": 10}), dec="CH2_ASK_LAILA_SAFE"),
  O("أقفلها وأمشي. الورق ده قتل ناس.", "ch2_refuse", E({"Family_Truth": -12, "Organization_Suspicion": -8}), dec="CH2_REFUSE_SAFE"),
], act=2)

L("ch2_letters", "ثلاث رسايل بخط أبوك.\nالرسالة الأولى: «اللي عملته مش اختيار. أنا حاولت أخرّج ياسين من الملف، وفشلت.»\nالتانية: «يوسف مش عدو. يوسف هو اللي مدّ الإيد.»\nالتالتة مش بخط أبوك.", act=2, next="ch2_letters_2")
L("ch2_letters_2", "التالتة مكتوبة على ورقة من نت، وبخط مزنوق:\n«لو وصلت لحد هنا، يبقى آدم فتح الخزنة. آدم — تعالى السطح. لوحدك. — ي»\n\nحرف الياء ده آدم عارفه.", act=2, next="ch2_murad_1",
  onEnter=E({"Family_Truth": 10, "Trust_Yassin": 12}))

L("ch2_ask_laila", "«الورق ده بتاع أبوك. والراجل اللي عنده عناوين كل الناس اللي دفعوا التمن هو يوسف.»\nليلى قالت الاسم زي اللي بيرمي حاجة تقيلة من إيده.", speaker="laila", act=2, next="ch2_ask_laila_2")
L("ch2_ask_laila_2", "«وأنا مش عارفة يوسف ده عايز منك إيه، بس هو اللي بعتلك الظرف.»", speaker="laila", act=2, next="ch2_murad_1",
  onEnter=E({"Yusuf_Connection": 12, "Trust_Laila": 6}))

L("ch2_refuse", "قفلت الخزنة وحطّيت المفتاح في جيبك.\nليلى كانت واقفة في الباب وشافت. ماقالتش حاجة، وده كان أسوأ.", act=2, next="ch2_murad_1")

L("ch2_murad_1", "مراد كان واقف عند باب البيت. مش دخل، ومش خرج.\n«أنا مش هدخل البيت ده. أنا وقفت على بابه سنة كاملة بعد ما أبوك مات.»", speaker="murad", act=2, next="ch2_c3")

C("ch2_c3", "", [
  O("«إنت عارف يوسف مين؟»", "ch2_murad_known", E({"Trust_Murad": -6, "Yusuf_Connection": 12}), dec="CH2_ASK_MURAD_YUSUF"),
  O("«أنا خارج من ده كله. خلاص.»", "ch2_murad_out", E({"Trust_Murad": 10, "Organization_Suspicion": -10}), dec="CH2_QUIT"),
  O("«إنت اللي بعت الظرف؟»", "ch2_murad_accuse", E({"Trust_Murad": -15, "Organization_Suspicion": 15}), dec="CH2_ACCUSE_MURAD_2"),
], act=2)

L("ch2_murad_known", "مراد بصّ في الأرض.\n«يوسف كان ضابط. وأنا كنت الملف اللي بيحميه. وأبوك كان اللي بيغطّيه.\nكلنا كنا في نفس الغرفة يا آدم، وانت كنت في البيت بتراجع امتحان.»", speaker="murad", act=2, next="ch2_yusuf_call")
L("ch2_murad_out", "«مفيش خروج يا آدم. في ناس بتخرج، بس مش بالطريقة دي.»\nمراد مشي تلات خطوات وبعدها وقف.\n«لو عايز تخرج بجد، تبقى محتاج حد يكرهك كفاية إنه يقول عليك الحقيقة.»", speaker="murad", act=2, next="ch2_yusuf_call")
L("ch2_murad_accuse", "مراد مابتسمش.\n«لو أنا كنت بعت الظرف، كنت هكتب اسمي عليه. أنا راجل بموت مكتوب اسمي.»\nوطلع من جيبه ظرف تاني، مقفول.", speaker="murad", act=2, next="ch2_yusuf_call")

L("ch2_yusuf_call", "التليفون رنّ. رقم مجهول تاني.\nالمرة دي آدم عرف مين قبل ما يجاوب.", act=2, next="ch2_c4")

C("ch2_c4", "", [
  O("أرد.", "ch2_yusuf_talk", E({"Yusuf_Connection": 15, "Trust_Yassin": 8}), dec="CH2_TALK_YUSUF"),
  O("أسجّل المكالمة وأرد.", "ch2_yusuf_record", E({"Yusuf_Connection": 8, "Organization_Suspicion": 12}), dec="CH2_RECORD_YUSUF"),
  O("أقفل وخلاص.", "ch2_yusuf_ignore", E({"Yusuf_Connection": -15, "Trust_Yassin": -10}), dec="CH2_IGNORE_YUSUF"),
], act=2)

L("ch2_yusuf_talk", "«أنا يوسف. وأنا اللي كنت واقف في الجنازة.\nمش هطلب منك تسامحني — هطلب منك حاجة واحدة: متصدّقش مراد وهو بيتكلم عن أبوك.»", speaker="yusuf", act=2, next="ch2_yusuf_talk_2")
L("ch2_yusuf_talk_2", "«أبوك مات وهو بيحمي ملف. ومراد عنده الملف ده من تمان سنين، ومستخدمه يحمي نفسه.»", speaker="yusuf", act=2, next="ch2_end")

L("ch2_yusuf_record", "«أنا يوسف.»\n\nآدم سجّل أول تلات ثواني، وبعدها الراجل على الناحية التانية قال:\n\n«عارف إنك بتسجّل. متقلقش. أنا عايز التسجيل ده يبقى موجود.»", speaker="yusuf", act=2, next="ch2_end",
  onEnter=E({"Family_Truth": 12}))

L("ch2_yusuf_ignore", "قفلت الخط.\nبعدها بشوية، جالك رسالة:\n«أنا مش هتصل تاني. الباب اللي إنت بتقفله ده آخر باب.»\nولقيت نفسك بتنده على مراد.", act=2, next="ch2_end")

L("ch2_end", "الساعة ٥:٤٠. البيت في العباسية، والشمس بتحاول تطلع من غير إذن.\nآدم في نص الطريق، ومحتاج يقرر هو مين النهاردة.", act=2, next="ch2_end_choice")

C("ch2_end_choice", "الفجر. والورق. وليلى. ومراد. ويوسف.", [
  O("أصدّق يوسف.", "ch2_end_yusuf", E({"Yusuf_Connection": 15, "Family_Truth": 10, "Trust_Murad": -10}), dec="CH2_BELIEVE_YUSUF"),
  O("أصدّق مراد.", "ch2_end_murad", E({"Trust_Murad": 18, "Yusuf_Connection": -12}), dec="CH2_BELIEVE_MURAD"),
  O("أصدّق ليلى.", "ch2_end_laila", E({"Trust_Laila": 20, "Family_Truth": 8}), dec="CH2_BELIEVE_LAILA"),
], act=2)

L("ch2_end_yusuf", "آدم خرج برّه البيت ومعاه ورقة واحدة. الورقة اللي مش بخط أبوه.", act=2, next="ch2_E1")
L("ch2_end_murad", "آدم خرج ومعاه مراد. والراجل مش بيبص وراه.", act=2, next="ch2_E2")
L("ch2_end_laila", "آدم خرج وليلى جوه. حلف عشان يسيبها برّه الحكاية، وعارف إن الدنيا مش بتسيب حد برّه.", act=2, next="ch2_E3")

END("ch2_E1", "CH2_A", "الفصل الثاني — الورقة",
    "الورقة كانت عنوان.\n\nشدّها آدم بين صوابعه لحد ما الورق بقى ناعم. وعلى العنوان: مستودع قديم على طريق الفيوم، رقم عشرين.\n\n*يوسف مش عدو. وإنت مش موظف. وإنت طلع كنت واحد من ناس كتير اتلغى الاسم بتاعهم على ورق أبيض.*")
END("ch2_E2", "CH2_B", "الفصل الثاني — الراجل اللي وقف على الباب",
    "مراد مشي جنب آدم لحد العربية من غير كلمة.\n\nبعد ما قعدوا، قال: «أنا ما خونتش أبوك. بس أنا عشت في الخوف اللي هو ملوش شكل. سبع سنين وفي حاجة جواه بتقولي كل يوم: لو اتكلمت، اللي بيموت هو أنت.»\n\n*آدم صدّقه. وكان صح. لكن الصدق مش نفس الذنب — والعربية مشي في الشارع الهادي.*")
END("ch2_E3", "CH2_C", "الفصل الثاني — ليلى",
    "ليلى وقفت في إطار الباب وشدّت شالها.\n\n«أنا هروح أوادي لإلياس. ثمّال لو رجعت البيت ده تاني، هيكون مجرد شقة.»\n\nقربت منه، وحطّت إيدها على كتفه، وقامت.\n\n*آدم ماقالش خلاص. وعدم الكلام ده كان أول حاجة في حياته اسمها اختيار.*")

# ══════════════════════════════════════════════════════════════
#  الفصل الثالث — الحساب
# ══════════════════════════════════════════════════════════════

L("ch3_from_ch2_a", "مستودع عشرين. الأبواب مفتوحة والباب الكبير مش قافل.", act=1, next="ch3_01")
L("ch3_from_ch2_b", "مشي وراه الإشارة اللي مراد قايله. مستودع عشرين على طريق الفيوم.", act=1, next="ch3_01")
L("ch3_from_ch2_c", "دخل من غير عربية. مشي المسافة كلها. مستودع عشرين، ومازال الفجر.", act=1, next="ch3_01")

L("ch3_01", "٦:١٢. مستودع قديم على طريق الفيوم. سقفه صاج، وأرضه أسمنت، والريحة ريحة حديد مش مصدّي.", act=3, next="ch3_02")
L("ch3_02", "في النص، راجل قاعد على كرسي بضهره لآدم.\nقبل ما آدم يقول حاجة، الراجل قال:", act=3, next="ch3_yassin_1")

L("ch3_yassin_1", "لما كنت تقعد تحت السلم، كنت بتقعد في نفس المكان اللي أنا كنت بقعد فيه وأنا مش موجود.", speaker="yassin", act=3, next="ch3_yassin_2")
L("ch3_yassin_2", "ياسين. بس بطول ٨ سنين زيادة، وعينين مختلفين.\nآدم قعد؟ ولا وقف؟", act=3, next="ch3_yassin_3")
L("ch3_yassin_3", "أنا مش هطلب منك تسامحني.\nأنا بس عايزك تسألني الحاجة اللي بيحميك من سؤالها.", speaker="yassin", act=3, next="ch3_c1")

C("ch3_c1", "", [
  O("«قتلت مين؟»", "ch3_ask_kill", E({"Family_Truth": 12, "Organization_Suspicion": 6}), dec="CH3_ASK_KILL"),
  O("«إنت طلعت من العقد بنفسك. وأنا خدت مكانك.»", "ch3_ask_deal", E({"Trust_Yassin": 15, "Family_Truth": 8}), dec="CH3_ASK_DEAL"),
  O("«أنا مش عايز أعرف. أنا عايز أعرف أنا مين دلوقتي.»", "ch3_ask_self", E({"Trust_Yassin": 6, "Family_Truth": 16}), dec="CH3_ASK_SELF"),
], act=3)

L("ch3_ask_kill", "ياسين حطّ إيديه على حجره.\n«أنا قتلت أربعة. عشان أنقذ تلاتماية اللي في الملف.\nوهو نفس المنطق اللي إنت بتمشي بيه من أول يوم مع مراد.»", speaker="yassin", act=3, next="ch3_reveal")
L("ch3_ask_deal", "«ماكنش معايا طريقة تانية.\nالاوعد الوحيد اللي عرضوه عليّا كان إني أبقى ميت، وإنت تبقى برّه القايمة.»", speaker="yassin", act=3, next="ch3_reveal")
L("ch3_ask_self", "ياسين سكت طويل.\n«إنت اللي اختار يقعد بدالي.\nأنا مش هقولك طيب. كلنا في الوقت ده كنا مش حلوين.»", speaker="yassin", act=3, next="ch3_reveal")

L("ch3_reveal", "الباب الكبير من الناحية التانية اتفتح.\nمراد دخل، وماسك سلاحه، ومش بيبص على ياسين — بيبص عليك.", speaker="murad", act=3, next="ch3_reveal_2")
L("ch3_reveal_2", "«آدم. اللي إنت واقف عليه ده بطّل يبقى حقّك من ساعة ما فتحت الخزنة.»", speaker="murad", act=3, next="ch3_reveal_3")
L("ch3_reveal_3", "ومراد نادى على اسم واحد تالي. اسم يوسف.\nوالراجل اللي دخل من الباب التاني كان واقف على الشاشة طول الوقت من غير ما حد شافه.", act=3, next="ch3_c2")

C("ch3_c2", "تلات رجالة في مستودع، وانت في النص، ومحدش فيهم كان كداب وأنا مش عارف مين.", [
  O("أحطّ السلاح على مراد.", "ch3_gun_murad", E({"Trust_Murad": -20, "Organization_Suspicion": 15}), dec="CH3_POINT_MURAD"),
  O("أحطّ السلاح على يوسف.", "ch3_gun_yusuf", E({"Yusuf_Connection": -20, "Organization_Suspicion": 10}), dec="CH3_POINT_YUSUF"),
  O("أكوّر السلاح وأحطّه على الأرض.", "ch3_ground", E({"Family_Truth": 10, "Trust_Yassin": 10}), dec="CH3_DROP_GUN"),
], act=3)

L("ch3_gun_murad", "مراد رفع إيديه. مش خايف — مستغرب.\n«ماشي. أنا سايب السلاح. بس قدامك عشرين ثانية تختار، وبعدها هيتحسم كل حاجة.»", speaker="murad", act=3, next="ch3_final_setup")
L("ch3_gun_yusuf", "يوسف حطّ إيديه في جيبه وأخرج ورقة مطبوعة.\n«دي نسخة من الملف. متبقاش حكمة. خدها واستعملها.»", speaker="yusuf", act=3, next="ch3_final_setup",
  onEnter=E({"Family_Truth": 15, "Yusuf_Connection": 10}))
L("ch3_ground", "السلاح نزل على الأسمنت بصوت واحد.\nالتلاتة سكتوا. وسكوت التلاتة معناه إن القرار بقى عليك لوحدك.", act=3, next="ch3_final_setup")

L("ch3_final_setup", "آدم في نص المستودع. الفجر دخل من الشباك العالي، والنور واقع على الأرض في خط مستقيم.\nآدم على الخط ده.", act=3, next="ch3_final")

C("ch3_final", "النهاية. مفيش حد هيعملها بدالك.", [
  O("«مراد. أنا شفت الملف. ومن النهاردة أنا مش بتاعك.»", "ch3_E_shadow", E({"Family_Truth": 15}), dec="CH3_FINAL_BREAK", req=S("Family_Truth", ">=", 55)),
  O("«يوسف. خد الملف وروح للمباحث.»", "ch3_E_witness", E({"Trust_Laila": 10}), dec="CH3_FINAL_WITNESS", req=AND(S("Yusuf_Connection", ">=", 45), S("Family_Truth", ">=", 45))),
  O("«ياسين. تعالى نخرج من هنا.»", "ch3_E_brother", E({"Trust_Yassin": 20}), dec="CH3_FINAL_BROTHER", req=S("Trust_Yassin", ">=", 45)),
  O("«مراد. أنا معاك.»", "ch3_E_loyal", E({"Trust_Murad": 20}), dec="CH3_FINAL_LOYAL", req=S("Trust_Murad", ">=", 60)),
  O("«مفيش حد فيكم هيمشي من هنا بحاجة.»", "ch3_E_reset", E({"Organization_Suspicion": 25}), dec="CH3_FINAL_RESET", req=S("Organization_Suspicion", ">=", 55)),
  O("[تسكت وتخرج من غير كلمة]", "ch3_E_silence", E({"Family_Truth": -15}), dec="CH3_FINAL_SILENCE"),
], act=3)

END("ch3_E_shadow", "CH3_1", "النهاية الأولى — الظلّ",
    "آدم ساب مراد في المستودع وساب معاه المفتاح.\n\nبعد تلات أيام، الورق طلع على السطح في كل الجرنال. وبعد خمس أيام، مراد اتقبض عليه في مطار القاهرة وهو عنده تذكرة بمكانين.\n\nيوسف اختفى. ياسين فتح ورشة نجارة صغيرة في المنصورة.\n\n*آدم ماعملش حاجة اسمها صح. عمل حاجة اسمها: وقّف. وفي عالمه، التوقّف ده كان كفاية.*")
END("ch3_E_witness", "CH3_2", "النهاية الثانية — الشاهد",
    "الورق اتسلّم قبل الساعة ٩.\n\nنادين استلمت الملف بإيدها، وفتحت أول صفحة، وقفلتها بعدها نص دقيقة.\n\n«إنت عارف إن ده هيعرّضك للتفتيش ألف مرة؟»\n«أنا عارف.»\n\nشريف حمدي اتقبض عليه بعد أسبوعين. وملف الشحنة بقى رقم قيد.\n\n*آدم دفع التمن. لكن لأول مرة، التمن ده كان مكتوب باسمه على ورق رسمي.*")
END("ch3_E_brother", "CH3_3", "النهاية الثالثة — الأخ",
    "خرجوا الاتنين من باب المستودع الجانبي.\n\nمراد حطّ إيده على آدم.\n«آدم. لو مشيت من الباب ده، مفيش رجوع.»\n«عارف.»\n\nمشي مع ياسين لحد طريق الفيوم، وركبا ميكروباص.\n\n*السر مش إنهم مش خايفين. السر إن حد فيهم قرر إن الخوف مبقالوش لازمة.*")
END("ch3_E_loyal", "CH3_4", "النهاية الرابعة — الراجل القديم",
    "آدم مشي ورا مراد.\n\nياسين بصّ له وهو بيمشي، ومقالش حاجة.\n\nبعد شهر، آدم أخد العقد اللي بعده. وبعده اللي بعده. ومراد بقى عنده مكتب في جاردن سيتي.\n\n*كل عقد كان بياخده، كان بيسيب في جيبه صورة قديمة. وفي كل مرة كان بيدخلها في الدرج قبل ما ينسى.*")
END("ch3_E_reset", "CH3_5", "النهاية الخامسة — التفريغ",
    "آدم فتح على التلاتة.\n\nالتحقيق استمر تمان شهور. النتيجة الرسمية: محظّات في ثلاث ملفات، وقيد جنائي على ستة عشر شخص، ومفيش اسم متهم واحد في قضية قتل.\n\nمراد خرج من البلد. ياسين اختفى. يوسف مات في العباسية بعد أربع سنين.\n\n*آدم ماعملهاش بعذاب. عملهها بطريقة حد بيحسبها حسابات. واللي بيحسب بيقدر يعيش.*")
END("ch3_E_silence", "CH3_6", "النهاية السادسة — الصمت",
    "آدم خرج من المستودع.\n\nماقعدش يبص على التلاتة. ماسمعش صوت مراد بينده وراه، ولا شاف إيد ياسين وهي بتتطلع.\n\n*في الحياة اللي بعده، آدم عمره ما حكى لحد.\nوهو نفسه نسي إيه اللي قرر يعمله الفجر ده. وقد كان ده أحسن حاجة عملها له.\nمفيش حد في القاهرة سأل عليه. ومحدش افتكره لمدة أسبوعين كاملين.*")
END("ch3_E_end", "CH3_X", "😩", "🙂")

# ── تقسيم + إخراج ──
def split_and_write():
    ch2, ch3 = [], []
    for n in N:
        (ch3 if n["id"].startswith("ch3") else ch2).append(n)
    return ch2, ch3

# ملاحظة: ربط السرد لازم يتّعدّل يدوياً عند أي تغيير في الـ graph
ch2, ch3 = [], []
for n in N:
    if n["id"].startswith("ch3"): ch3.append(n)
    else: ch2.append(n)

# احذف العقدة المؤقتة
ch3 = [n for n in ch3 if n["id"] != "ch3_E_end"]

def doc(cid, title, entry, nodes):
    return {"schemaVersion": 1, "id": cid, "title": title, "entry": entry, "nodes": nodes}

out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                       "Assets", "_Project", "Resources", "Story")
files = {
    "chapter02.json": doc("chapter02", "الفصل الثاني — البيت القديم", "ch2_from_ch1_a", ch2),
    "chapter03.json": doc("chapter03", "الفصل الثالث — الحساب", "ch3_from_ch2_a", ch3),
}

def validate(d):
    ids = {n["id"] for n in d["nodes"]}
    errs = []
    for n in d["nodes"]:
        if n.get("next") and n["next"] not in ids: errs.append(f"{n['id']}: next '{n['next']}'")
        if n.get("elseNext") and n["elseNext"] not in ids: errs.append(f"{n['id']}: elseNext")
        for o in n.get("options", []):
            if o.get("next") not in ids: errs.append(f"{n['id']}: opt '{o.get('next')}'")
        if n["type"] == 0 and not n.get("next"): errs.append(f"{n['id']}: بلا next")
    if d["entry"] not in ids: errs.append("entry مفقود")
    return errs

ok = True
for name, d in files.items():
    errs = validate(d)
    if errs:
        ok = False
        print(f"✗ {name}: {len(errs)} خطأ")
        for e in errs[:10]: print("   -", e)
    else:
        print(f"✓ {name}: {len(d['nodes'])} عقدة")
    with open(os.path.join(out_dir, name), "w", encoding="utf-8") as f:
        json.dump(d, f, ensure_ascii=False, indent=1)

sys.exit(0 if ok else 1)
