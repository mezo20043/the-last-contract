#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
مولّد الفصل الأول «الظرف الأخير» — نسخة 1.0 من السيناريو المعتمد.

بيقرأ مفيش حاجة: كل الحوار مكتوب هنا كنص مصدر، والمخرج ملف JSON
بيقراه NarrativeRunner في Unity. السبب إننا بنولّد بـPython بدل ما نكتب
JSON بإيدينا: ١) التحقق من إن كل `next` بيشاور على عقدة موجودة،
٢) إن كل خيار ليه decisionId فريد، ٣) إن شروط النهايات متسقة.

التشغيل:  python3 tools/make_chapter01.py
المخرج:  Assets/_Project/Resources/Story/chapter01.json
"""

import json
import os
import sys

# ─────────────────────────── أدوات بناء ───────────────────────────

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Story/chapter01.json")

nodes = []
_ids = set()


def _add(node):
    nid = node["id"]
    if nid in _ids:
        raise SystemExit(f"❌ معرّف مكرر: {nid}")
    _ids.add(nid)
    nodes.append(node)
    return nid


def line(nid, speaker, text, nxt, camera=None, act=None, on_enter=None, require=None, else_next=None):
    n = {"id": nid, "type": 0, "speaker": speaker or "", "text": text, "next": nxt}
    if camera: n["camera"] = camera
    if act: n["act"] = act
    if on_enter: n["onEnter"] = on_enter
    if require: n["require"] = require
    if else_next: n["elseNext"] = else_next
    return _add(n)


def narration(nid, text, nxt, camera=None, act=None, on_enter=None):
    return line(nid, "", text, nxt, camera=camera, act=act, on_enter=on_enter)


def choice(nid, prompt, options, next_if_empty=None, camera=None):
    n = {"id": nid, "type": 1, "speaker": "", "text": prompt, "options": options}
    if next_if_empty: n["next"] = next_if_empty
    if camera: n["camera"] = camera
    return _add(n)


def opt(oid, text, nxt, stats=None, set_flags=None, clear_flags=None, decision=None, require=None):
    o = {"id": oid, "text": text, "next": nxt, "decisionId": decision or oid.upper()}
    eff = {}
    if stats: eff["stats"] = [{"key": k, "add": v} for k, v in stats]
    if set_flags: eff["setFlags"] = list(set_flags)
    if clear_flags: eff["clearFlags"] = list(clear_flags)
    if eff: o["effect"] = eff
    if require: o["require"] = require
    return o


def branch(nid, options, fallback=None):
    """تفريع شرطي: أول خيار شرطه متحقق بيمشي عليه."""
    n = {"id": nid, "type": 2, "speaker": "", "text": "", "options": options}
    if fallback: n["next"] = fallback
    return _add(n)


def event(nid, action, param, nxt, camera=None):
    n = {"id": nid, "type": 3, "speaker": "", "text": "", "action": action, "param": param, "next": nxt}
    if camera: n["camera"] = camera
    return _add(n)


def ending(nid, eid, title, body):
    return _add({"id": nid, "type": 4, "speaker": "", "text": "", "endingId": eid,
                 "endingTitle": title, "endingBody": body})


def req_flags(all_flags=None, none_flags=None, any_flags=None):
    r = {}
    if all_flags: r["allFlags"] = all_flags
    if none_flags: r["noneFlags"] = none_flags
    if any_flags: r["anyFlags"] = any_flags
    return r


def req_stats(pairs):
    return {"stats": [{"key": k, "op": op, "value": v} for (k, op, v) in pairs]}


def merge(*parts):
    out = {}
    for p in parts:
        if not p: continue
        for k, v in p.items():
            out[k] = v
    return out


# ═══════════════════════ المشهد ١ — ليلة عادية ═══════════════════════

narration("c1_open",
          "١١:٣٢ مساءً. شقة في مدينة نصر، الدور التاسع. المطر بيخبط على الشباك بهدوء، "
          "والشارع تحت مبِل من النور الأصفر.",
          "c1_sms1", camera="wide_room", act="scene01")

line("c1_sms1", "murad", "«يا عم إنت مختفي ليه؟ بكرة لازم أشوفك.»", "c1_adam_reply")

line("c1_adam_reply", "adam", "«بكرة إن شاء الله، أنا مرهق جدًا.»",
     "c1_put_phone", camera="cu_adam")

narration("c1_put_phone", "بيحط الموبايل جنبه على الكنبة. سكوته بقى أعمق من صوت المطر.",
          "c1_doorbell")

narration("c1_doorbell", "…جرس الباب.", "c1_adam_who", camera="wide_room")

line("c1_adam_who", "adam", "مين في الوقت ده؟", "c1_choice_1", camera="cu_adam")

choice("c1_choice_1", "قبل ما تفتح… إيه أول حاجة تعملها؟", [
    opt("c1_o_phone", "[تفحص الموبايل]", "c1_check_phone",
        set_flags=["CHECKED_PHONE"], decision="CH1_CHECK_PHONE"),
    opt("c1_o_peep", "[تبص من العين السحرية]", "c1_check_peep",
        set_flags=["CHECKED_PEEPHOLE"], decision="CH1_CHECK_PEEPHOLE"),
    opt("c1_o_door", "[تفتح الباب على طول]", "c1_open_door",
        decision="CH1_OPEN_DOOR_DIRECT"),
], next_if_empty="c1_open_door")

line("c1_check_phone", "adam",
     "مفيش رسائل غير رسالة مراد… مفيش حد تاني يعرف إني صاحي.",
     "c1_choice_peep", camera="cu_adam")

line("c1_check_peep", "adam",
     "مفيش حد في الدهليز… بس فيه حاجة على السجادة.",
     "c1_open_door", camera="cu_adam")

line("c1_choice_peep", "adam",
     "وباب الجيران مقفول زي العادة. يعني اللي جه… جه مخصوص.",
     "c1_open_door", camera="cu_adam")

narration("c1_open_door", "بيفتح الباب. الدهليز فاضي. السجادة عليها ظرف بني.",
          "c1_envelope_found", camera="wide_doorway")

narration("c1_envelope_found",
          "ظرف بني، مكتوب عليه اسمه بخط إيد: «آدم». مفيش اسم مرسل. "
          "وعلى ظهره أثر ختم قديم باهت.",
          "c1_envelope_choice")

choice("c1_envelope_choice", "الظرف في إيدك…", [
    opt("c1_o_inspect", "[تفحص الظرف كويس]", "c1_inspect",
        set_flags=["INSPECTED_ENVELOPE"], decision="CH1_INSPECT_ENVELOPE"),
    opt("c1_o_open", "[تفتحه على طول]", "c1_open_env",
        decision="CH1_OPEN_ENVELOPE"),
], next_if_empty="c1_open_env")

line("c1_inspect", "adam",
     "حد جايبلي ظرف باسمي… ومش كاتب اسمه. والختم ده… ده مش ختم بوستة.",
     "c1_open_env", camera="cu_adam")

line("c1_open_env", "adam", "…", "c1_contents", camera="cu_prop_envelope",
     on_enter={"setFlags": ["ENVELOPE_KEPT"]})

narration("c1_contents",
          "جوه الظرف: صورة قديمة لرجل واقف قدام مبنى إداري، وورقة مكتوب عليها بخط اليد: "
          "«العقد الأخير — لا تثق في النسخة التي معك»، وتحتها رقم موبايل.",
          "c1_phone_rings", camera="cu_prop_contents")

narration("c1_phone_rings", "وفجأة… الموبايل بيرن. رقم غريب.",
          "c1_call_1", camera="cu_phone")

# ═══════════════════════ المشهد ٢ — المكالمة الأولى ═══════════════════════

line("c1_call_1", "caller", "مساء الخير يا آدم.", "c1_call_2", act="scene02")
line("c1_call_2", "adam", "مين معايا؟", "c1_call_3", camera="cu_adam")
line("c1_call_3", "caller", "وصلت لك الصورة؟", "c1_call_4")
line("c1_call_4", "adam", "إنت اللي بعت الظرف؟", "c1_call_5")
line("c1_call_5", "caller", "اسمعني كويس. متروحش للشرطة.", "c1_call_6")
line("c1_call_6", "adam", "طب أروح لمين؟ وإنت مين أصلًا؟", "c1_call_7")
line("c1_call_7", "caller", "اسأل مراد عن سليم.", "c1_call_8")
line("c1_call_8", "adam", "مين سليم؟", "c1_call_9")
line("c1_call_9", "caller", "الشخص اللي اختفى من سبع سنين.", "c1_call_10")
line("c1_call_10", "adam", "أنا مالي بالموضوع ده؟", "c1_call_11")
line("c1_call_11", "caller", "لأن اسمك موجود في العقد.", "c1_call_end")
narration("c1_call_end", "الخط بينقطع. آدم بيبص للموبايل كإنه بيستنى يرد تاني… مفيش حاجة.",
          "c1_sms_threat")
narration("c1_sms_threat",
          "رسالة من نفس الرقم: «دي فرصتك الأخيرة تبعد عن الموضوع.»",
          "c1_choice_2", camera="cu_phone")

choice("c1_choice_2", "الساعة ١١:٤٧. إيه اللي هتعمله؟", [
    opt("c1_o_call_murad", "[تتصل بمراد فورًا]", "c1_scene03_entry",
        stats=[("Trust_Murad", 1)], set_flags=["CALLED_MURAD"], decision="CH1_CALL_MURAD"),
    opt("c1_o_search_selim", "[تدور على سليم على الإنترنت]", "c1_search",
        stats=[("Evidence_Count", 1)], set_flags=["KNOWS_SELIM"], decision="CH1_SEARCH_SELIM"),
    opt("c1_o_dispose", "[تتخلص من الظرف]", "c1_dispose",
        clear_flags=["ENVELOPE_KEPT"], decision="CH1_DISPOSE_ENVELOPE"),
], next_if_empty="c1_scene03_entry")

line("c1_search", "adam", "سليم… سبع سنين. إيه اللي حصل؟", "c1_search2", camera="cu_adam")
narration("c1_search2",
          "خبر قديم من ٢٠١٩: «اختفاء موظف في شركة النور للاستثمارات — التحقيق اتقفل بدون نتيجة.»",
          "c1_scene03_entry", camera="cu_screen")

narration("c1_dispose",
          "بيحط الظرف في كيس بلاستيك وينزل يرميه في صندوق الزبالة. "
          "الصورة راحت… لكن الاسم اللي في المكالمة مش راح.",
          "c1_scene03_entry", camera="cu_prop_envelope")

# ═══════════════════════ المشهد ٣ — مراد ═══════════════════════

narration("c1_scene03_entry",
          "١٠:١٥ صباحًا. مقهى في مدينة نصر. مراد قاعد جانب الشباك وبيحرك كوب القهوة.",
          "c1_murad_hi", camera="wide_cafe", act="scene03")

line("c1_murad_hi", "murad", "أخيرًا! إنت كنت فين يا بني؟", "c1_murad_ask", camera="ots_murad")
line("c1_murad_ask", "adam", "حصلت حاجة غريبة امبارح.", "c1_branch_murad_open")

branch("c1_branch_murad_open", [
    {"id": "b_called", "next": "c1_murad_called", "require": req_flags(all_flags=["CALLED_MURAD"])},
    {"id": "b_not", "next": "c1_murad_normal"},
])

line("c1_murad_called", "murad",
     "إنت كنت بتكلمني امبارح وكنت متوتر. حصل إيه؟",
     "c1_branch_selim", camera="ots_murad")

line("c1_murad_normal", "murad", "مالك؟ شكلك مش طبيعي.", "c1_branch_selim", camera="ots_murad")

branch("c1_branch_selim", [
    {"id": "b_selim", "next": "c1_murad_selim", "require": req_flags(all_flags=["KNOWS_SELIM"])},
    {"id": "b_no_selim", "next": "c1_branch_env"},
])

line("c1_murad_selim", "adam",
     "لقيت خبر قديم عن واحد اسمه سليم. اختفى من سبع سنين.",
     "c1_murad_selim2", camera="ots_adam")
narration("c1_murad_selim2",
          "مراد بيوقف تحريك الكوب. إيده بتفضل في الهوا نص ثانية قبل ما تنزل.",
          "c1_murad_selim3", camera="cu_murad")
line("c1_murad_selim3", "murad", "مين قالك الاسم ده؟", "c1_branch_env")

branch("c1_branch_env", [
    {"id": "b_env", "next": "c1_show_photo", "require": req_flags(all_flags=["ENVELOPE_KEPT"])},
    {"id": "b_noenv", "next": "c1_murad_deny"},
])

line("c1_show_photo", "adam", "تعرف الراجل ده؟", "c1_murad_photo1", camera="cu_prop_photo")
narration("c1_murad_photo1",
          "مراد بياخد الصورة، بيبص لها لثانيتين… وبيرجعها بسرعة.",
          "c1_murad_photo2", camera="cu_murad")
line("c1_murad_photo2", "murad", "لا.", "c1_murad_press")
line("c1_murad_press", "adam", "بص كويس.", "c1_murad_raise")
line("c1_murad_raise", "murad", "بقولك معرفوش!", "c1_murad_fear")
line("c1_murad_fear", "adam", "إنت خايف من الصورة ليه؟", "c1_murad_warn")
line("c1_murad_warn", "murad", "اسمعني، الصورة دي لو معاك، متوريهاش لأي حد.",
     "c1_murad_why", camera="ots_murad")
line("c1_murad_why", "adam", "ليه؟", "c1_murad_answer")
line("c1_murad_answer", "murad", "لأن مش كل اللي في الصور بيكونوا عايشين.",
     "c1_murad_stop")

line("c1_murad_deny", "adam", "طب سليم؟ أنت تعرفه؟", "c1_murad_stop", camera="ots_adam")

narration("c1_murad_stop",
          "مراد بيسكت، وبيرفع عينه ناحية باب المقهى.",
          "c1_murad_stop2", camera="cu_murad")
line("c1_murad_stop2", "murad", "اسمعني يا آدم. اقفل الموضوع ده.", "c1_murad_why2")
line("c1_murad_why2", "adam", "هو أنا مش فاهم حاجة! إيه اللي بيحصل؟", "c1_murad_warning")
line("c1_murad_warning", "murad",
     "في حاجات لما تعرفها، مش هتعرف ترجع زي الأول.",
     "c1_choice_3", camera="ots_murad")

choice("c1_choice_3", "مراد مستني إجابتك…", [
    opt("c1_o_press", "[تضغط عليه]", "c1_press_1",
        stats=[("Trust_Murad", -1), ("Suspicion", 1)],
        set_flags=["PRESSED_MURAD"], decision="CH1_PRESS_MURAD"),
    opt("c1_o_pretend", "[تتظاهر بالتصديق]", "c1_pretend_1",
        stats=[("Trust_Murad", 1)], set_flags=["PRETENDED_MURAD"], decision="CH1_PRETEND_MURAD"),
    opt("c1_o_tell_call", "[تحكيله عن المكالمة كاملة]", "c1_tell_1",
        set_flags=["TOLD_MURAD_CALL"], decision="CH1_TELL_MURAD_CALL"),
], next_if_empty="c1_press_1")

line("c1_press_1", "adam", "مش همشي غير لما تحكي كل حاجة.", "c1_press_2", camera="cu_adam")
line("c1_press_2", "murad", "إنت مش فاهم حجم الموضوع.", "c1_press_3")
narration("c1_press_3", "مراد بيقوم ويسيب المقهى من غير وداع.", "c1_murad_after")

line("c1_pretend_1", "adam", "تمام. هنسى الموضوع.", "c1_pretend_2")
line("c1_pretend_2", "murad", "ده أحسن ليك.", "c1_pretend_3")
line("c1_pretend_3", "adam", "بس عندي سؤال أخير. سليم كان صاحبك؟",
     "c1_pretend_4", camera="ots_adam")
narration("c1_pretend_4", "مراد بيبص ناحية الشباك.", "c1_pretend_5", camera="cu_murad")
line("c1_pretend_5", "murad", "كان شخص أعرفه.", "c1_pretend_6")
line("c1_pretend_6", "adam", "وراح فين؟", "c1_pretend_7")
line("c1_pretend_7", "murad", "دي الحكاية اللي محدش عرف إجابتها.", "c1_murad_after")

line("c1_tell_1", "adam", "الراجل قال إن اسمي موجود في العقد.", "c1_tell_2", camera="ots_adam")
narration("c1_tell_2", "وش مراد بيتغير.", "c1_tell_3", camera="cu_murad")
line("c1_tell_3", "murad", "قالك كده؟", "c1_tell_4")
line("c1_tell_4", "adam", "أيوه. إنت عارف حاجة؟", "c1_tell_5")
line("c1_tell_5", "murad", "أنا… محتاج أفكر.", "c1_murad_after")

narration("c1_murad_after",
          "مراد بيمشي. تحت الكوب ورقة صغيرة مكتوب عليها: «شارع جانبي — وسط البلد — الدور التالت»، "
          "وتحتها كلمة واحدة: «الأرشيف».",
          "c1_branch_murad_bonus", camera="cu_prop_note")

# حوار إضافي عند ثقة مراد ≥ 2
branch("c1_branch_murad_bonus", [
    {"id": "b_bonus", "next": "c1_murad_bonus_1", "require": req_stats([("Trust_Murad", ">=", 56)])},
    {"id": "b_nobonus", "next": "c1_scene04_entry"},
])
line("c1_murad_bonus_1", "murad", "آدم… أنا مكنتش عايزك تعرف بالطريقة دي.",
     "c1_murad_bonus_2", camera="ots_murad")
line("c1_murad_bonus_2", "adam", "تعرف إيه؟", "c1_murad_bonus_3")
line("c1_murad_bonus_3", "murad", "سليم قبل ما يختفي بيوم، جه عندي.", "c1_murad_bonus_4")
line("c1_murad_bonus_4", "adam", "وقالك إيه؟", "c1_murad_bonus_5")
line("c1_murad_bonus_5", "murad", "قال إن في حد جوه الشركة بيغير سجلات قديمة.",
     "c1_murad_bonus_6", camera="cu_murad")
line("c1_murad_bonus_6", "adam", "مين؟", "c1_murad_bonus_7")
line("c1_murad_bonus_7", "murad", "مذكرش اسم. بس ساب عندي رقم ملف.", "c1_murad_bonus_8")
line("c1_murad_bonus_8", "adam", "والرقم معاك؟", "c1_murad_bonus_9")
line("c1_murad_bonus_9", "murad", "لأ… أنا سلمته لشخص تاني.", "c1_murad_bonus_10")
line("c1_murad_bonus_10", "adam", "مين الشخص ده؟", "c1_murad_bonus_11", camera="cu_adam")
line("c1_murad_bonus_11", "murad", "ليلى.", "c1_scene04_entry",
     on_enter={"setFlags": ["MURAD_NAMED_LAILA"]}, camera="cu_murad")

# ═══════════════════════ المشهد ٤ — ليلى ═══════════════════════

narration("c1_scene04_entry",
          "٢:٤٠ ظهرًا. مبنى قديم في وسط البلد. آدم بيطلع السلم وبيطرق باب المكتب.",
          "c1_laila_open", camera="wide_stairs", act="scene04")

line("c1_laila_open", "laila", "حضرتك آدم؟", "c1_laila_2", camera="ots_laila")
line("c1_laila_2", "adam", "أيوه. إنتِ مين؟", "c1_laila_3")
line("c1_laila_3", "laila", "ليلى. أنا اللي بعتلك رسالة.", "c1_laila_4")
line("c1_laila_4", "adam", "رسالة إيه؟", "c1_laila_5")
narration("c1_laila_5",
          "بتفتح موبايلها وبتوريه رسالة من رقم مجهول: «آدم لازم يعرف الحقيقة قبل ما يكون هو الضحية التالية.»",
          "c1_laila_6", camera="cu_phone")
line("c1_laila_6", "adam", "إنتِ تعرفي مين بعتها؟", "c1_laila_7")
line("c1_laila_7", "laila", "لا. لكن أنا بدور على سليم من فترة طويلة.", "c1_laila_8")
narration("c1_laila_8", "بتحط صور على المكتب.", "c1_laila_9", camera="cu_prop_photos")
line("c1_laila_9", "laila", "سليم اختفى من سبع سنين. آخر مكان اتشاف فيه كان المبنى ده.",
     "c1_laila_10", camera="ots_laila")
line("c1_laila_10", "adam", "وإيه علاقة مراد بالموضوع؟", "c1_laila_11")
line("c1_laila_11", "laila", "السؤال مش عن مراد بس. السؤال: ليه اسمك ظهر دلوقتي؟",
     "c1_branch_laila_selim")

branch("c1_branch_laila_selim", [
    {"id": "b_knows", "next": "c1_laila_knows", "require": req_flags(all_flags=["KNOWS_SELIM"])},
    {"id": "b_noknows", "next": "c1_laila_env"},
])
line("c1_laila_knows", "laila", "واضح إنك بحثت عن اسمه قبل ما تيجي.", "c1_laila_env")

branch("c1_laila_env", [
    {"id": "b_have", "next": "c1_laila_photo", "require": req_flags(all_flags=["ENVELOPE_KEPT"])},
    {"id": "b_missing", "next": "c1_laila_nophoto"},
])
line("c1_laila_nophoto", "laila", "معاكش الصورة؟", "c1_laila_nophoto2")
line("c1_laila_nophoto2", "adam", "اتخلصت منها.", "c1_laila_nophoto3")
line("c1_laila_nophoto3", "laila", "كان ممكن تساعدنا.", "c1_choice_4")

line("c1_laila_photo", "laila", "استنى… إنت جبت الصورة دي منين؟", "c1_laila_photo2",
     camera="cu_laila")
line("c1_laila_photo2", "adam", "وصلتلي في ظرف.", "c1_laila_photo3")
line("c1_laila_photo3", "laila", "الظرف كان عليه اسمك؟", "c1_laila_photo4")
line("c1_laila_photo4", "adam", "أيوه.", "c1_laila_photo5")
line("c1_laila_photo5", "laila", "يبقى اللي بعتلك الصورة عارفك من زمان.", "c1_laila_photo6")
line("c1_laila_photo6", "adam", "تعرفي الشخص اللي واقف جنب سليم؟", "c1_laila_photo7")
line("c1_laila_photo7", "laila", "يوسف. رجل الأعمال اللي كان ماسك الشركة وقت اختفاء سليم.",
     "c1_laila_photo8")
line("c1_laila_photo8", "adam", "وليه محدش اتكلم عنه؟", "c1_laila_photo9")
line("c1_laila_photo9", "laila", "لأن التحقيق اتقفل بسرعة غريبة.", "c1_laila_photo10")
line("c1_laila_photo10", "adam", "وإنتِ بتدوري على إيه بالضبط؟", "c1_laila_photo11")
line("c1_laila_photo11", "laila",
     "توقيع واحد. توقيع يثبت إن سليم مخرجش من الشركة بإرادته.",
     "c1_choice_4", camera="ots_laila")

choice("c1_choice_4", "ليلى مستنية…", [
    opt("c1_o_give_photo", "[تديها صورة الظرف]", "c1_give_1",
        stats=[("Trust_Laila", 2), ("Evidence_Count", 1)],
        set_flags=["REVEALED_LAILA"], decision="CH1_GIVE_PHOTO",
        require=req_flags(all_flags=["ENVELOPE_KEPT"])),
    opt("c1_o_limited", "[تديها معلومات محدودة]", "c1_limited_1",
        stats=[("Trust_Laila", 1)], decision="CH1_LIMITED_INFO"),
    opt("c1_o_refuse", "[ترفض التعاون]", "c1_refuse_1",
        stats=[("Trust_Laila", -1)], decision="CH1_REFUSE_LAILA"),
], next_if_empty="c1_limited_1")

line("c1_give_1", "laila", "الصورة دي مهمة جدًا. المبنى ده تابع لشركة النور.",
     "c1_laila_bonus", camera="cu_laila")

line("c1_limited_1", "adam", "أنا مش هحكيلك كل حاجة دلوقتي.", "c1_limited_2")
line("c1_limited_2", "laila", "حقك.", "c1_limited_3")
line("c1_limited_3", "adam", "بس عايز أعرف إنتِ وصلتي لاسمي إزاي.", "c1_limited_4")
narration("c1_limited_4", "بتفتح درج وبتطلع نسخة ورقية.", "c1_limited_5")
line("c1_limited_5", "laila", "الاسم ده ظهر في سجل قديم.", "c1_limited_6")
line("c1_limited_6", "adam", "أنا كنت عندي ١٧ سنة وقتها!", "c1_limited_7")
line("c1_limited_7", "laila", "عشان كده الموضوع غريب.", "c1_limited_8")
line("c1_limited_8", "adam", "يعني حد استخدم اسمي؟", "c1_limited_9")
line("c1_limited_9", "laila", "أو حد كان بيجهز حاجة من وقتها.", "c1_laila_bonus")

line("c1_refuse_1", "adam", "أنا مش واثق فيكي.", "c1_refuse_2")
line("c1_refuse_2", "laila", "الثقة مش مطلوبة. المطلوب إنك تفكر.", "c1_refuse_3")
line("c1_refuse_3", "adam", "مش عايز أتورط.", "c1_refuse_4")
line("c1_refuse_4", "laila", "أنت متورط بالفعل، سواء عايز أو لأ.", "c1_refuse_5")
narration("c1_refuse_5", "بتقفل الملف.", "c1_refuse_6")
line("c1_refuse_6", "laila", "لما تقرر تعرف الحقيقة، ابحث عن رقم العقد ٧١٤.",
     "c1_laila_bonus", on_enter={"setFlags": ["HEARD_CONTRACT_714"]})

branch("c1_laila_bonus", [
    {"id": "b_ltrust", "next": "c1_laila_bonus_1", "require": req_stats([("Trust_Laila", ">=", 42)])},
    {"id": "b_noltrust", "next": "c1_scene05_entry"},
])
line("c1_laila_bonus_1", "laila", "في حاجة لازم تعرفها.", "c1_laila_bonus_2", camera="cu_laila")
line("c1_laila_bonus_2", "adam", "إيه؟", "c1_laila_bonus_3")
line("c1_laila_bonus_3", "laila", "سليم كان بيبعتلي رسائل قبل اختفائه.", "c1_laila_bonus_4")
line("c1_laila_bonus_4", "adam", "وإنتِ كنتِ تعرفيه؟", "c1_laila_bonus_5")
line("c1_laila_bonus_5", "laila", "كنت بساعده في جمع مستندات عن الشركة.", "c1_laila_bonus_6")
line("c1_laila_bonus_6", "adam", "وليه مدخلتيش الشرطة؟", "c1_laila_bonus_7")
line("c1_laila_bonus_7", "laila", "دخلت. وبعدها اختفت المستندات واتقفل التحقيق.", "c1_laila_bonus_8")
line("c1_laila_bonus_8", "adam", "مين قفل التحقيق؟", "c1_laila_bonus_9")
narration("c1_laila_bonus_9", "صمت قصير.", "c1_laila_bonus_10")
line("c1_laila_bonus_10", "laila", "ياسين.", "c1_laila_bonus_11")
line("c1_laila_bonus_11", "adam", "المحقق ياسين؟", "c1_laila_bonus_12")
line("c1_laila_bonus_12", "laila",
     "أيوه. ومش متأكدة إذا كان بيحمي القضية ولا بيحمي حد منها.",
     "c1_scene05_entry", on_enter={"setFlags": ["LAILA_SUSPECTS_YASSIN"]})

# ═══════════════════════ المشهد ٥ — الأرشيف ═══════════════════════

narration("c1_scene05_entry",
          "٦:٢٠ مساءً. مبنى إداري قديم. ممر طويل، إضاءة فلورسنت بتترعش، وخزائن ملفات معدنية "
          "والغبار مغطي المكاتب وصدى خطواته بيرجع له.",
          "c1_archive_hub", camera="wide_archive", act="scene05")

choice("c1_archive_hub", "بتفتح الخزائن… تدور على إيه؟", [
    opt("c1_ev_file", "[ملف سليم]", "c1_ev_file_1",
        stats=[("Evidence_Count", 1)],
        set_flags=["FOUND_ARCHIVE_FILE"], decision="CH1_ARCHIVE_FILE",
        require=req_flags(none_flags=["FOUND_ARCHIVE_FILE"])),
    opt("c1_ev_photo", "[صورة يوسف]", "c1_ev_photo_1",
        stats=[("Evidence_Count", 1)],
        set_flags=["FOUND_YUSUF_PHOTO"], decision="CH1_ARCHIVE_PHOTO",
        require=req_flags(none_flags=["FOUND_YUSUF_PHOTO"])),
    opt("c1_ev_contract", "[ورقة فيها رقم عقد]", "c1_ev_contract_1",
        stats=[("Evidence_Count", 1)],
        set_flags=["FOUND_CONTRACT_NUMBER"], decision="CH1_ARCHIVE_CONTRACT",
        require=req_flags(none_flags=["FOUND_CONTRACT_NUMBER"])),
    opt("c1_ev_log", "[سجل الدخول]", "c1_ev_log_1",
        set_flags=["FOUND_ENTRY_LOG"], decision="CH1_ARCHIVE_LOG",
        require=req_flags(none_flags=["FOUND_ENTRY_LOG"])),
    opt("c1_ev_leave", "[تخرج من الأرشيف]", "c1_archive_event",
        decision="CH1_ARCHIVE_LEAVE"),
], next_if_empty="c1_archive_event")

line("c1_ev_file_1", "adam", "يعني الراجل ده كان شغال هنا فعلًا.", "c1_archive_hub",
     camera="cu_prop_file")
line("c1_ev_photo_1", "adam", "هو ده الراجل اللي في الصورة القديمة.", "c1_archive_hub",
     camera="cu_prop_photo")
line("c1_ev_contract_1", "adam", "العقد الأخير… النور للاستثمارات.", "c1_archive_hub",
     camera="cu_prop_contract")
line("c1_ev_log_1", "adam", "توقيع في تاريخ الاختفاء… بس مش واضح.", "c1_archive_hub",
     camera="cu_prop_log")

narration("c1_archive_event", "صوت باب بيتقفل في آخر الممر.", "c1_archive_who")
line("c1_archive_who", "adam", "مين هنا؟", "c1_archive_silence", camera="cu_adam")
narration("c1_archive_silence",
          "مفيش رد. على الأرض موبايل قديم… وبيرن.",
          "c1_archive_call_1", camera="cu_phone_old")

line("c1_archive_call_1", "caller", "أنت بتدور في المكان الغلط.", "c1_archive_call_2")
line("c1_archive_call_2", "adam", "إنت مين؟!", "c1_archive_call_3", camera="cu_adam")
line("c1_archive_call_3", "caller",
     "أنا الشخص الوحيد اللي لسه بيحاول يمنع اللي حصل زمان من إنه يتكرر.", "c1_archive_call_4")
line("c1_archive_call_4", "adam", "حصل إيه زمان؟", "c1_archive_call_5")
line("c1_archive_call_5", "caller", "بص وراك.", "c1_archive_turn")
narration("c1_archive_turn",
          "الكاميرا بتلف ورا آدم. الباب مقفول. وعلى الحيطة… صورة لآدم نفسه وهو بيدخل الأرشيف.",
          "c1_archive_shock", camera="cu_wall_photo")
line("c1_archive_shock", "adam", "إزاي؟!", "c1_archive_black", camera="cu_adam")
narration("c1_archive_black", "قطع سريع للسواد.", "c1_archive_black_fx")
event("c1_archive_black_fx", "audio", "ending_sting", "c1_scene06_entry")

# ═══════════════════════ المشهد ٦ — المحقق ياسين ═══════════════════════

narration("c1_scene06_entry",
          "٨:١٠ مساءً. مكتب التحقيقات. آدم قاعد قدام ياسين، وياسين بيفتح ملف قديم.",
          "c1_yassin_1", camera="wide_interrogation", act="scene06")

line("c1_yassin_1", "yassin", "اسمك آدم؟", "c1_yassin_2", camera="ots_yassin")
line("c1_yassin_2", "adam", "أيوه.", "c1_yassin_3")
line("c1_yassin_3", "yassin", "شغال فين؟", "c1_yassin_4")
line("c1_yassin_4", "adam", "موظف أرشيف.", "c1_yassin_5")
line("c1_yassin_5", "yassin", "غريبة.", "c1_yassin_6", camera="cu_yassin")
line("c1_yassin_6", "adam", "إيه الغريب؟", "c1_yassin_7")
line("c1_yassin_7", "yassin", "لأننا لقينا اسمك في ملف قديم متعلق باختفاء سليم.", "c1_yassin_8")
line("c1_yassin_8", "adam", "أنا عمري ما قابلت الراجل ده.", "c1_yassin_9")
line("c1_yassin_9", "yassin",
     "أنا مصدق إنك ممكن تكون مش فاهم اللي بيحصل. لكن ده مش معناه إنك بريء من كل حاجة.",
     "c1_yassin_10", camera="ots_yassin")
line("c1_yassin_10", "adam", "هو أنا متهم؟", "c1_yassin_11")
line("c1_yassin_11", "yassin", "لسه.", "c1_yassin_12")
narration("c1_yassin_12", "بيطلع صورة يوسف.", "c1_yassin_13", camera="cu_prop_photo")
line("c1_yassin_13", "yassin", "تعرف الشخص ده؟", "c1_yassin_14")
line("c1_yassin_14", "adam", "شفته في صورة عند ليلى.", "c1_choice_5", camera="cu_adam")

choice("c1_choice_5", "ياسين مستني…", [
    opt("c1_o_tell_all", "[تحكيله عن ليلى بكل حاجة]", "c1_yassin_all_1",
        stats=[("Trust_Yassin", 1), ("Suspicion", -1)],
        set_flags=["TOLD_YASSIN_ABOUT_LAILA"], decision="CH1_TELL_YASSIN_LAILA"),
    opt("c1_o_hide", "[تخفي اسم ليلى]", "c1_yassin_hide_1",
        stats=[("Trust_Yassin", -1)], decision="CH1_HIDE_LAILA"),
    opt("c1_o_demand", "[تطلب تشوف الملف القديم]", "c1_yassin_file_1",
        stats=[("Evidence_Count", 1)], decision="CH1_DEMAND_FILE"),
], next_if_empty="c1_yassin_hide_1")

line("c1_yassin_all_1", "adam", "ليلى صحفية. هي اللي قالتلي إن سليم اختفى.", "c1_yassin_all_2")
line("c1_yassin_all_2", "yassin", "ليلى؟ من إمتى وهي بتدور في القضية دي؟",
     "c1_yassin_all_3", camera="cu_yassin")
line("c1_yassin_all_3", "adam", "إن سليم كان بيجمع مستندات عن الشركة.", "c1_yassin_all_4")
line("c1_yassin_all_4", "yassin", "وهي قالتلك إنها كانت بتساعده؟", "c1_yassin_all_5")
narration("c1_yassin_all_5", "ياسين بيقفل الملف ببطء.", "c1_yassin_all_6")
line("c1_yassin_all_6", "yassin",
     "خلي بالك من المعلومات اللي بتسمعها. مش كل واحد بيقول إنه بيدور على الحقيقة بيكون عايزها.",
     "c1_yassin_all_7")
line("c1_yassin_all_7", "adam", "حضرتك تعرفها؟", "c1_yassin_all_8")
line("c1_yassin_all_8", "yassin", "أعرف ملفها.", "c1_yassin_all_9")
line("c1_yassin_all_9", "adam", "وده معناه إيه؟", "c1_yassin_all_10")
line("c1_yassin_all_10", "yassin", "معناه إنك لازم تتأكد من كل كلمة قبل ما تصدقها.",
     "c1_yassin_bonus")

line("c1_yassin_hide_1", "yassin", "في حد تاني كلمك؟", "c1_yassin_hide_2")
line("c1_yassin_hide_2", "adam", "لأ.", "c1_yassin_hide_3")
line("c1_yassin_hide_3", "yassin", "متأكد؟", "c1_yassin_hide_4")
line("c1_yassin_hide_4", "adam", "أيوه.", "c1_yassin_hide_5")
narration("c1_yassin_hide_5", "ياسين بيراقب وشه.", "c1_yassin_hide_6", camera="cu_yassin")
line("c1_yassin_hide_6", "yassin", "كويس. لأن أي معلومة ناقصة ممكن تقلب القضية كلها.",
     "c1_yassin_hide_7")
line("c1_yassin_hide_7", "adam", "طب أنا أعمل إيه؟", "c1_yassin_hide_8")
line("c1_yassin_hide_8", "yassin", "متتحركش لوحدك، ومتخبيش دليل.", "c1_yassin_bonus")

line("c1_yassin_file_1", "adam", "عايز أشوف الملف القديم.", "c1_yassin_file_2")
line("c1_yassin_file_2", "yassin", "مش متاح.", "c1_yassin_file_3")
line("c1_yassin_file_3", "adam", "ليه؟", "c1_yassin_file_4")
line("c1_yassin_file_4", "yassin", "لأنه ملف تحقيق، مش ملف عام.", "c1_yassin_file_5")
line("c1_yassin_file_5", "adam", "بس اسمي موجود فيه!", "c1_yassin_file_6")
narration("c1_yassin_file_6", "ياسين بيبص له طويل.", "c1_yassin_file_7", camera="cu_yassin")
line("c1_yassin_file_7", "yassin",
     "عندك حق في سؤال واحد. اسمك ظهر في سجل قديم، لكن مش كمتهم.", "c1_yassin_file_8")
line("c1_yassin_file_8", "adam", "أمال كإيه؟", "c1_yassin_file_9")
line("c1_yassin_file_9", "yassin", "كشاهد محتمل.", "c1_yassin_file_10")
line("c1_yassin_file_10", "adam", "أنا عمري ما شفت سليم!", "c1_yassin_file_11")
line("c1_yassin_file_11", "yassin", "دي بالضبط المشكلة اللي محتاجين نفهمها.", "c1_yassin_bonus")

branch("c1_yassin_bonus", [
    {"id": "b_ytrust", "next": "c1_yassin_bonus_1", "require": req_stats([("Trust_Yassin", ">=", 21)])},
    {"id": "b_noytrust", "next": "c1_yassin_end"},
])
line("c1_yassin_bonus_1", "yassin", "أنا هقولك حاجة خارج المحضر.", "c1_yassin_bonus_2",
     camera="cu_yassin")
line("c1_yassin_bonus_2", "adam", "اتفضل.", "c1_yassin_bonus_3")
line("c1_yassin_bonus_3", "yassin",
     "ليلة اختفاء سليم، كان في بلاغ عن دخول شخص للمبنى بعد إغلاقه.", "c1_yassin_bonus_4")
line("c1_yassin_bonus_4", "adam", "مين؟", "c1_yassin_bonus_5")
line("c1_yassin_bonus_5", "yassin", "الاسم في السجل كان اسمك.", "c1_yassin_bonus_6")
line("c1_yassin_bonus_6", "adam", "مستحيل!", "c1_yassin_bonus_7", camera="cu_adam")
line("c1_yassin_bonus_7", "yassin", "عارف إنك كنت صغير وقتها.", "c1_yassin_bonus_8")
line("c1_yassin_bonus_8", "adam", "مين كتب اسمي؟", "c1_yassin_bonus_9")
line("c1_yassin_bonus_9", "yassin",
     "التوقيع مش واضح. وده السبب اللي خلاني أطلب إعادة فتح الملف.",
     "c1_yassin_bonus_10", on_enter={"setFlags": ["YASSIN_REOPENED_CASE"]})
line("c1_yassin_bonus_10", "adam", "يعني حضرتك مصدقني؟", "c1_yassin_bonus_11")
line("c1_yassin_bonus_11", "yassin", "أنا مصدق إن الحقيقة لسه ناقصة.", "c1_yassin_end")

line("c1_yassin_end", "yassin",
     "يا آدم، أنا هقولك حاجة واحدة. لو حد اتصل بيك تاني، متحاولش تواجهه لوحدك.",
     "c1_yassin_end2", camera="ots_yassin")
line("c1_yassin_end2", "adam", "حضرتك تعرف مين بيتصل؟", "c1_scene07_entry")

# ═══════════════════════ المشهد ٧ — شخص كان هنا ═══════════════════════

narration("c1_scene07_entry",
          "١١:٠٥ مساءً. آدم راجع شقته. الباب مش مقفول بالكامل.",
          "c1_home_1", camera="wide_apartment", act="scene07")
line("c1_home_1", "adam", "أنا قفلت الباب قبل ما أنزل…", "c1_home_2", camera="cu_adam")
narration("c1_home_2",
          "الأثاث زي ما هو. بس في حاجة ناقصة… وعلى الطاولة ورقة جديدة.",
          "c1_home_3", camera="wide_room")
narration("c1_home_3",
          "«متسألش عن سليم. اسأل عن الشخص اللي وقّع مكانه.»",
          "c1_home_4", camera="cu_prop_note")
narration("c1_home_4",
          "الموبايل بيصوّت. صورة من كاميرا مراقبة: آدم نفسه وهو بيدخل الأرشيف.",
          "c1_home_5", camera="cu_phone")
narration("c1_home_5", "«إنت مش لوحدك في اللعبة دي».", "c1_choice_6")

choice("c1_choice_6", "الساعة ١١:٠٥ مساءً… مين هتتصل بيه؟", [
    opt("c1_o_call_murad2", "[تتصل بمراد]", "c1_ally_murad",
        stats=[("Trust_Murad", 1)], set_flags=["ALLY_MURAD"], decision="CH1_ALLY_MURAD"),
    opt("c1_o_call_laila2", "[تتصل بليلى]", "c1_ally_laila",
        stats=[("Trust_Laila", 1)], set_flags=["ALLY_LAILA"], decision="CH1_ALLY_LAILA"),
    opt("c1_o_call_yassin2", "[تروح لياسين]", "c1_ally_yassin",
        stats=[("Trust_Yassin", 1)], set_flags=["ALLY_YASSIN"], decision="CH1_ALLY_YASSIN"),
], next_if_empty="c1_ally_murad")

line("c1_ally_murad", "adam", "لازم تيجي دلوقتي. في حاجة حصلت.", "c1_ally_murad_2")
line("c1_ally_murad_2", "murad", "حاضر. بس متفتحش الباب لأي حد.", "c1_scene08_entry")

line("c1_ally_laila", "adam", "ليلى، حد دخل شقتي.", "c1_ally_laila_2")
line("c1_ally_laila_2", "laila", "محدش يقدر يدخل عندك من غير ما يعرف عنوانك. خليك مكانك.",
     "c1_scene08_entry")

line("c1_ally_yassin", "adam", "حضرتك كنت محق. الموضوع أكبر مني.", "c1_ally_yassin_2")
line("c1_ally_yassin_2", "yassin", "استنى مكانك. هنتصرف.", "c1_scene08_entry")

# ═══════════════════════ المشهد ٨ — المواجهة الأولى ═══════════════════════

narration("c1_scene08_entry",
          "اليوم التالي، ٩:٠٠ صباحًا. مكتب فاخر بإطلالة على القاهرة. يوسف قاعد ورا مكتبه وموبايله بيرن.",
          "c1_yusuf_1", camera="wide_office", act="scene08")
line("c1_yusuf_1", "yusuf", "وصل الظرف؟", "c1_yusuf_2", camera="ots_yusuf")
line("c1_yusuf_2", "caller", "أيوه.", "c1_yusuf_3")
line("c1_yusuf_3", "yusuf", "وآدم؟", "c1_yusuf_4")
line("c1_yusuf_4", "caller", "بدأ يدور.", "c1_yusuf_5")
narration("c1_yusuf_5", "يوسف بيسكت.", "c1_yusuf_6", camera="cu_yusuf")
line("c1_yusuf_6", "yusuf", "كنت عارف إن اليوم ده هييجي.", "c1_yusuf_7")
line("c1_yusuf_7", "caller", "نعمل إيه؟", "c1_yusuf_8")
narration("c1_yusuf_8", "بيصب بصره على صورة قديمة على المكتب.", "c1_yusuf_9",
          camera="cu_prop_office_photo")
line("c1_yusuf_9", "yusuf", "مفيش رجوع دلوقتي.", "c1_yusuf_10")
narration("c1_yusuf_10",
          "الكاميرا بتقرّب من الصورة: يوسف واقف جنب سليم… وفي الخلفية شخص تالت مش واضح.",
          "c1_ending_eval", camera="cu_prop_office_photo")

# ═══════════════════════ تقييم النهايات ═══════════════════════

branch("c1_ending_eval", [
    # القيم الافتراضية للثقة: مراد 55 · ليلى 40 · ياسين 20 (من GameState).
    # فالعتبات هنا "افتراضي + المكسب المطلوب"، مش أرقام مطلقة.
    {"id": "b_end_a", "next": "c1_endA_scene",
     "require": merge(req_stats([("Evidence_Count", ">=", 3), ("Trust_Laila", ">=", 42)]),
                      req_flags(all_flags=["FOUND_ARCHIVE_FILE"]))},
    {"id": "b_end_a2", "next": "c1_endA_scene",
     "require": merge(req_stats([("Evidence_Count", ">=", 3), ("Trust_Murad", ">=", 56)]),
                      req_flags(all_flags=["FOUND_ARCHIVE_FILE"]))},
    {"id": "b_end_c", "next": "c1_endC_scene",
     "require": merge(req_stats([("Evidence_Count", "<=", 1), ("Trust_Laila", "<=", 39)]),
                      req_flags(none_flags=["FOUND_ARCHIVE_FILE"]))},
], fallback="c1_endB_scene")

# ── النهاية A — الحقيقة الأولى
narration("c1_endA_scene",
          "غرفة ليلى. آدم قاعد قدام الكمبيوتر. ملف صوتي قديم بيوصل.",
          "c1_endA_1", camera="wide_office_laila", act="ending_a")
narration("c1_endA_1", "بيشغّل التسجيل.", "c1_endA_2")
line("c1_endA_2", "caller", "«لو التسجيل ده وصل لحد، يبقى أنا فشلت في إني أرجع.»",
     "c1_endA_3", camera="cu_screen")
line("c1_endA_3", "adam", "ده صوت سليم…", "c1_endA_4", camera="cu_adam")
line("c1_endA_4", "caller", "«آدم… لو بتسمع صوتي، يبقى هم وصلوا ليك.»", "c1_endA_5")
narration("c1_endA_5", "الشاشة بتسود.", "c1_postcredit_a")
ending("c1_endA_end", "CH1_A", "النهاية الأولى — الحقيقة",
       "سمعت صوت سليم. القضية اتفتحت تاني — بس الملف اللي في إيدك ناقص صفحة.")

# ── النهاية B — المشتبه به
narration("c1_endB_scene", "قسم الشرطة. ياسين بيفتح ملف جديد.", "c1_endB_1",
          camera="wide_interrogation", act="ending_b")
line("c1_endB_1", "yassin", "المرة دي مش هقدر أحميك.", "c1_endB_2", camera="ots_yassin")
line("c1_endB_2", "adam", "أنا معملتش حاجة.", "c1_endB_3")
line("c1_endB_3", "yassin", "أتمنى تكون بتقول الحقيقة.", "c1_endB_4")
narration("c1_endB_4", "الكاميرا بتبعد عن الغرفة. بنسمع صوت الباب وهو بيتقفل.", "c1_postcredit_b")
ending("c1_endB_end", "CH1_B", "النهاية الثانية — المشتبه به",
       "القضية اتقلبت عليك. ياسين مش قادر يحميك، والملف بقى باسمك.")

# ── النهاية C — الاختفاء
narration("c1_endC_scene", "تاكسي بالليل. آدم بيبص من الشباك على شوارع القاهرة.",
          "c1_endC_1", camera="wide_taxi", act="ending_c")
narration("c1_endC_1", "بينزل شقة… جنبه ظرف بني على المقعد.", "c1_endC_2")
narration("c1_endC_2", "«كنت مستنيك.»", "c1_endC_3", camera="cu_prop_envelope")
line("c1_endC_3", "adam", "إنت جبت الظرف ده منين؟", "c1_endC_4", camera="cu_adam")
narration("c1_endC_4", "السواق ماردش. قطع للسواد.", "c1_postcredit_c")
ending("c1_endC_end", "CH1_C", "النهاية الثالثة — الاختفاء",
       "هربت من كل حاجة… والظرف لقاك في التاكسي.")

# ═══════════════════════ ما بعد النهاية ═══════════════════════

narration("c1_postcredit_a",
          "شاشة سودا. صوت مسجّل قديم: «آدم… التوقيع اللي بتدور عليه مش توقيع يوسف. "
          "الشخص ده المفروض يكون مات من سبع سنين.»",
          "c1_endA_end", camera="black")
narration("c1_postcredit_b",
          "غرفة تحقيق مقفولة. ياسين بيطلع صورة قديمة لآدم في السابعة عشرة. "
          "مكتوب على ظهرها: «آدم — شاهد رقم ٠٤».",
          "c1_endB_end", camera="cu_prop_photo_old")
narration("c1_postcredit_c",
          "طريق صحراوي. السواق بيرجع له الظرف… وجواه صورة لسليم وخلفها صورة لآدم وهو طفل. "
          "لما بيرفع راسه، السواق مش موجود.",
          "c1_endC_end", camera="wide_taxi")

# ═══════════════════════ التحقق والكتابة ═══════════════════════

doc = {
    "schemaVersion": 1,
    "id": "chapter01",
    "title": "الفصل الأول — الظرف الأخير",
    "entry": "c1_open",
    "nodes": nodes,
}

errors = []
for n in nodes:
    for key in ("next", "elseNext"):
        t = n.get(key)
        if t and t not in _ids:
            errors.append(f"{n['id']}.{key} → {t}")
    for o in n.get("options", []) or []:
        if o.get("next") and o["next"] not in _ids:
            errors.append(f"{n['id']}.{o['id']} → {o['next']}")
    if n["type"] == 4:
        if not n.get("endingId"): errors.append(f"{n['id']}: نهاية بدون endingId")

if errors:
    print("❌ مراجع مكسورة:")
    for e in errors: print("   ", e)
    sys.exit(1)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(doc, f, ensure_ascii=False, indent=2)

types = {}
for n in nodes: types[n["type"]] = types.get(n["type"], 0) + 1
opts = sum(len(n.get("options", []) or []) for n in nodes if n["type"] == 1)
decisions = {o["decisionId"] for n in nodes if n["type"] == 1 for o in n.get("options", [])}

print(f"✓ {OUT}")
print(f"  عُقد: {len(nodes)} · سطور: {types.get(0,0)} · قرارات: {types.get(1,0)} "
      f"· تفريعات: {types.get(2,0)} · أحداث: {types.get(3,0)} · نهايات: {types.get(4,0)}")
print(f"  خيارات: {opts} · قرارات فريدة: {len(decisions)}")
