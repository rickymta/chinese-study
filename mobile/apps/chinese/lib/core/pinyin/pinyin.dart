// Tiện ích pinyin của app tiếng Trung — port TOÀN BỘ `frontend/apps/chinese/src/lib/pinyin.ts` (hợp đồng mobile M3,
// HĐ web F5 §5.3.C). Nguồn sự thật cho HIỂN THỊ; thuần Dart (không import Flutter) để test nhanh.
// Quy ước dự án: pinyin LƯU dạng số thanh (`ni3 hao3`, thanh nhẹ `5`, `ü` = `v`), HIỂN THỊ dạng dấu (`nǐ hǎo`).
//
// Khác biệt với bản web: JS có `String.prototype.normalize('NFC')`, Dart không — thay bằng [_composePinyin] ghép các
// dấu tổ hợp (U+0304 macron, U+0301 sắc, U+030C caron, U+0300 huyền, U+0308 hai chấm) trên 6 nguyên âm pinyin thành
// ký tự dựng sẵn. Đủ cho mọi chuỗi pinyin; không nhằm thay thế NFC tổng quát.

/// Thanh điệu 1–5 (5 = thanh nhẹ).
typedef Tone = int;

/// Một âm tiết đã tách từ dạng số thanh.
class ParsedSyllable {
  const ParsedSyllable({required this.letters, required this.tone, required this.capitalized});

  /// Chữ thường, `ü` viết `v`.
  final String letters;
  final Tone tone;

  /// Chữ cái đầu viết hoa (tên riêng, đầu câu).
  final bool capitalized;

  @override
  bool operator ==(Object other) =>
      other is ParsedSyllable && other.letters == letters && other.tone == tone && other.capitalized == capitalized;

  @override
  int get hashCode => Object.hash(letters, tone, capitalized);

  @override
  String toString() => 'ParsedSyllable($letters, $tone, capitalized: $capitalized)';
}

/// Dấu thanh Unicode có sẵn (precomposed) cho 6 nguyên âm, thứ tự thanh 1–4.
const Map<String, List<String>> toneMarks = {
  'a': ['ā', 'á', 'ǎ', 'à'],
  'e': ['ē', 'é', 'ě', 'è'],
  'i': ['ī', 'í', 'ǐ', 'ì'],
  'o': ['ō', 'ó', 'ǒ', 'ò'],
  'u': ['ū', 'ú', 'ǔ', 'ù'],
  'v': ['ǖ', 'ǘ', 'ǚ', 'ǜ'],
};

/// Bảng ngược: ký tự có dấu ⇒ (nguyên âm gốc, thanh) — cả chữ hoa.
final Map<String, ({String base, Tone tone})> _markToBase = () {
  final out = <String, ({String base, Tone tone})>{};
  for (final entry in toneMarks.entries) {
    for (var i = 0; i < entry.value.length; i++) {
      final mark = entry.value[i];
      final tone = i + 1;
      out[mark] = (base: entry.key, tone: tone);
      out[mark.toUpperCase()] = (base: entry.key.toUpperCase(), tone: tone);
    }
  }
  return out;
}();

final RegExp _numberedToken = RegExp(r'^([A-Za-z]+)([1-5])$');
final RegExp _whitespace = RegExp(r'\s+');
final RegExp _latinOnly = RegExp(r'^[A-Za-z]+$');

/// Dấu tổ hợp ⇒ chỉ số thanh (0..3) trong [toneMarks]; U+0308 (hai chấm) xử lý riêng.
const Map<int, int> _combiningTone = {0x0304: 0, 0x0301: 1, 0x030C: 2, 0x0300: 3};
const int _combiningDiaeresis = 0x0308;
const Map<String, String> _diaeresisBase = {'u': 'ü', 'U': 'Ü'};

/// Ghép dạng tổ hợp (NFD) của các nguyên âm pinyin thành ký tự dựng sẵn (NFC): `u + ̈ + ̌` ⇒ `ǚ`, `a + ̄` ⇒ `ā`.
/// Chuỗi không có dấu tổ hợp trả nguyên (không cấp phát thêm).
String _composePinyin(String input) {
  var hasCombining = false;
  for (final r in input.runes) {
    if (r == _combiningDiaeresis || _combiningTone.containsKey(r)) {
      hasCombining = true;
      break;
    }
  }
  if (!hasCombining) return input;

  final runes = input.runes.toList();
  final out = StringBuffer();
  var i = 0;
  while (i < runes.length) {
    var ch = String.fromCharCode(runes[i]);
    i++;
    // `u` + hai chấm ⇒ `ü` (rồi có thể tiếp dấu thanh).
    if (i < runes.length && runes[i] == _combiningDiaeresis && _diaeresisBase.containsKey(ch)) {
      ch = _diaeresisBase[ch]!;
      i++;
    }
    if (i < runes.length && _combiningTone.containsKey(runes[i])) {
      final toneIdx = _combiningTone[runes[i]]!;
      final lower = ch == 'ü' ? 'v' : (ch == 'Ü' ? 'V' : ch);
      final marks = toneMarks[lower.toLowerCase()];
      if (marks != null && lower.length == 1) {
        final marked = marks[toneIdx];
        ch = lower == lower.toLowerCase() ? marked : marked.toUpperCase();
        i++;
      }
    }
    out.write(ch);
  }
  return out.toString();
}

/// `ü`, `u:` (cả hoa) ⇒ `v`/`V` — dạng lưu trữ của dự án.
String _replaceUmlaut(String s) =>
    s.replaceAll('ü', 'v').replaceAll('u:', 'v').replaceAll('Ü', 'V').replaceAll('U:', 'V');

/// Chữ cái đầu giữ nguyên, phần còn lại chữ thường, nối số thanh.
String _capitalizeToken(String letters, String tone) => '${letters[0]}${letters.substring(1).toLowerCase()}$tone';

/// Chuẩn hoá chuỗi pinyin số: trim, gộp khoảng trắng, `ü`/`u:` ⇒ `v`, mỗi token phải khớp `^[A-Za-z]+[1-5]$`;
/// giữ chữ hoa ở chữ cái đầu mỗi âm tiết, phần còn lại về chữ thường. Sai bất kỳ token nào ⇒ `null`.
/// Tương đương `PinyinText.NormalizeNumbered` phía backend.
String? normalizeNumbered(String input) {
  final tokens = _replaceUmlaut(_composePinyin(input)).trim().split(_whitespace).where((t) => t.isNotEmpty).toList();
  if (tokens.isEmpty) return null;
  final out = <String>[];
  for (final token in tokens) {
    final m = _numberedToken.firstMatch(token);
    if (m == null) return null;
    out.add(_capitalizeToken(m.group(1)!, m.group(2)!));
  }
  return out.join(' ');
}

/// `'Lv4'` ⇒ `ParsedSyllable(letters: 'lv', tone: 4, capitalized: true)`; token không đúng dạng số thanh ⇒ `null`.
ParsedSyllable? parseSyllable(String token) {
  final m = _numberedToken.firstMatch(_replaceUmlaut(_composePinyin(token).trim()));
  if (m == null) return null;
  final raw = m.group(1)!;
  return ParsedSyllable(
    letters: raw.toLowerCase(),
    tone: int.parse(m.group(2)!),
    capitalized: raw[0] != raw[0].toLowerCase(),
  );
}

Tone? toneOf(String token) => parseSyllable(token)?.tone;

/// `'lv4'` ⇒ `'lv'`; token không có số ⇒ trả nguyên (đã trim).
String stripTone(String token) => parseSyllable(token)?.letters ?? token.trim();

/// Vị trí nguyên âm nhận dấu theo quy tắc chuẩn: có `a` ⇒ `a`; không `a` mà có `e` ⇒ `e`; có `ou` ⇒ `o`;
/// còn lại ⇒ nguyên âm CUỐI (`gui4 → guì`, `liu2 → liú`, `huo3 → huǒ`). Không có nguyên âm ⇒ -1.
int _markIndex(String letters) {
  final a = letters.indexOf('a');
  if (a >= 0) return a;
  final e = letters.indexOf('e');
  if (e >= 0) return e;
  final ou = letters.indexOf('ou');
  if (ou >= 0) return ou;
  for (var i = letters.length - 1; i >= 0; i--) {
    if ('aeiouv'.contains(letters[i])) return i;
  }
  return -1;
}

/// Đặt dấu lên một âm tiết đã tách: `letters` thường (`v` = ü), thanh 1–5, tuỳ chọn viết hoa chữ đầu.
String _markLetters(String letters, Tone tone, bool capitalized) {
  final chars = letters.split('');
  if (tone != 5) {
    final idx = _markIndex(letters);
    if (idx >= 0) chars[idx] = toneMarks[chars[idx]]![tone - 1];
  }
  final marked = chars.map((c) => c == 'v' ? 'ü' : c).toList();
  // 'ā'.toUpperCase() == 'Ā' — Dart ánh xạ hoa/thường theo Unicode.
  if (capitalized && marked.isNotEmpty) marked[0] = marked[0].toUpperCase();
  return marked.join();
}

/// `'lve4'` ⇒ `'lüè'`; token sai dạng ⇒ trả NGUYÊN VĂN (không ném).
String syllableToMarked(String token) {
  final parsed = parseSyllable(token);
  if (parsed == null) return token;
  return _markLetters(parsed.letters, parsed.tone, parsed.capitalized);
}

/// Dấu câu có thể dính đầu/cuối một âm tiết trong pinyin của hội thoại/ví dụ (F9): ASCII `!?,.;:"'()` và toàn khổ
/// `，。！？、；：“”‘’…（）`. KHÔNG gồm `'` giữa âm tiết của dạng nối (`Xī'ān`) — chỉ cắt ở hai đầu token.
const String _punctClass = '!?,.;:"\'()\\-–—…，。！？、；：“”‘’（）';
final RegExp _tokenPunctRe = RegExp('^([$_punctClass]*)(.*?)([$_punctClass]*)\$');
final RegExp _punctOnlyRe = RegExp('^[$_punctClass\\s]+\$');
final RegExp _punctOrSpaceRe = RegExp('[$_punctClass\\s]');

/// Kết quả tách dấu câu hai đầu token.
typedef PunctuationSplit = ({String lead, String core, String trail});

/// Tách dấu câu hai đầu token: `'hao3!'` ⇒ `(lead: '', core: 'hao3', trail: '!')`.
PunctuationSplit splitPunctuation(String token) {
  final m = _tokenPunctRe.firstMatch(token);
  if (m == null) return (lead: '', core: token, trail: '');
  return (lead: m.group(1) ?? '', core: m.group(2) ?? '', trail: m.group(3) ?? '');
}

/// Bỏ mọi dấu câu (và khoảng trắng) khỏi chuỗi chữ Hán — để đếm/ánh xạ chữ ↔ âm tiết.
String stripPunctuation(String text) => text.replaceAll(_punctOrSpaceRe, '');

/// Chuỗi pinyin số ⇒ dạng dấu: `'ni3 hao3'` ⇒ `'nǐ hǎo'`. Âm tiết `r5` (nhi hoá) dính vào âm tiết trước
/// (`'na3 r5'` ⇒ `'nǎr'`, R5-6). Dấu câu dính đầu/cuối âm tiết được giữ nguyên chỗ (`'Ni3 hao3!'` ⇒ `'Nǐ hǎo!'`,
/// `'Lao3 shi1, nin2 hao3 ma5?'` ⇒ `'Lǎo shī, nín hǎo ma?'`). Token không hợp lệ giữ nguyên văn.
///
/// [join] `true` ⇒ nối liền các âm tiết thành một từ, chèn dấu nháy `'` khi âm tiết sau bắt đầu bằng a/o/e
/// (`Xi1 an1` ⇒ `Xī'ān`). Mặc định cách nhau bằng khoảng trắng.
String numberedToMarked(String pinyin, {bool join = false}) {
  final tokens = _composePinyin(pinyin).trim().split(_whitespace).where((t) => t.isNotEmpty);
  final texts = <String>[];
  final startsWithAOE = <bool>[];
  for (final token in tokens) {
    final split = splitPunctuation(token);
    final parsed = split.core.isNotEmpty ? parseSyllable(split.core) : null;
    if (parsed != null && parsed.letters == 'r' && parsed.tone == 5 && texts.isNotEmpty && split.lead.isEmpty) {
      texts[texts.length - 1] = '${texts.last}r${split.trail}';
      continue;
    }
    texts.add(
      parsed != null
          ? '${split.lead}${_markLetters(parsed.letters, parsed.tone, parsed.capitalized)}${split.trail}'
          : token,
    );
    startsWithAOE.add(parsed != null && split.lead.isEmpty && 'aoe'.contains(parsed.letters[0]));
  }
  if (!join) return texts.join(' ');
  final buf = StringBuffer();
  for (var i = 0; i < texts.length; i++) {
    if (i > 0 && startsWithAOE[i]) buf.write("'");
    buf.write(texts[i]);
  }
  return buf.toString();
}

final RegExp _markedSeparator = RegExp("[\\s'’]+");

/// Dạng dấu ⇒ dạng số: `'nǐ hǎo'` ⇒ `'ni3 hao3'`; không dấu ⇒ thanh nhẹ (`'ma'` ⇒ `'ma5'`); tách theo khoảng trắng
/// hoặc dấu nháy (`"Xī'ān"` ⇒ `'Xi1 an1'`); `ü` ⇒ `v`. Token có ký tự lạ (số, dấu câu) ⇒ `null`.
/// Token đã ở dạng số hợp lệ được giữ nguyên (chuẩn hoá hoa/thường).
String? markedToNumbered(String marked) {
  final tokens = _composePinyin(marked).trim().split(_markedSeparator).where((t) => t.isNotEmpty).toList();
  if (tokens.isEmpty) return null;
  final out = <String>[];
  for (final token in tokens) {
    final numbered = _numberedToken.firstMatch(_replaceUmlaut(token));
    if (numbered != null) {
      out.add(_capitalizeToken(numbered.group(1)!, numbered.group(2)!));
      continue;
    }
    var tone = 5;
    final letters = StringBuffer();
    for (final rune in token.runes) {
      final ch = String.fromCharCode(rune);
      final hit = _markToBase[ch];
      if (hit != null) {
        if (tone != 5) return null; // hai dấu thanh trong một âm tiết
        tone = hit.tone;
        letters.write(hit.base);
      } else if (ch == 'ü') {
        letters.write('v');
      } else if (ch == 'Ü') {
        letters.write('V');
      } else {
        letters.write(ch);
      }
    }
    final s = letters.toString();
    if (!_latinOnly.hasMatch(s)) return null;
    out.add(_capitalizeToken(s, '$tone'));
  }
  return out.join(' ');
}

/// Khoá âm tiết của bảng (R5-1, `v` = ü) ⇒ chữ hiển thị: `'nv'` ⇒ `'nü'`, `'lve'` ⇒ `'lüe'`, `'ju'` ⇒ `'ju'`.
String displaySyllableKey(String key) => key.replaceAll('v', 'ü');

/// Loại gợi ý biến điệu.
enum SandhiKind { thirdTone, bu, yi }

/// Gợi ý biến điệu cho một âm tiết (chỉ để hiển thị — app vẫn lưu thanh gốc).
class SandhiHint {
  const SandhiHint({required this.index, required this.kind, required this.suggestedTone, required this.text});

  /// Chỉ số âm tiết (theo token của chuỗi pinyin số, đã bỏ token chỉ có dấu câu).
  final int index;
  final SandhiKind kind;

  /// 2 hoặc 4.
  final Tone suggestedTone;

  /// Lời gợi ý ngắn tiếng Việt.
  final String text;

  @override
  bool operator ==(Object other) =>
      other is SandhiHint &&
      other.index == index &&
      other.kind == kind &&
      other.suggestedTone == suggestedTone &&
      other.text == text;

  @override
  int get hashCode => Object.hash(index, kind, suggestedTone, text);

  @override
  String toString() => 'SandhiHint($index, $kind, $suggestedTone, "$text")';
}

/// Gợi ý biến điệu CHỈ ĐỂ HIỂN THỊ (R5-5, R-C3 — app vẫn lưu thanh gốc):
/// - Thanh 3 đứng ngay trước thanh 3 ⇒ đọc gần thanh 2; chuỗi ≥ 3 thanh 3 ⇒ mọi âm tiết trừ cuối, kèm "tuỳ ngắt nhịp".
/// - 不 (`bu4`) trước thanh 4 ⇒ bú. 一 (`yi1`) trước thanh 4 ⇒ yí; trước thanh 1/2/3 ⇒ yì; cuối/trước thanh nhẹ ⇒ không.
/// - 不/一 nhận diện bằng CHỮ HÁN cùng vị trí (`hanzi`), không bằng pinyin (`yi1` còn là 衣, 医...). Không có `hanzi`
///   ⇒ chỉ gợi ý 3-3.
List<SandhiHint> sandhiHints(String pinyin, [String? hanzi]) {
  // Bỏ dấu câu dính hai đầu âm tiết (`hao3!`, `ma5?`) và token chỉ có dấu câu trước khi đếm — F9 truyền pinyin
  // của cả câu hội thoại.
  final tokens = _composePinyin(pinyin)
      .trim()
      .split(_whitespace)
      .where((t) => t.isNotEmpty && !_punctOnlyRe.hasMatch(t))
      .map((t) => splitPunctuation(t).core)
      .toList();
  final parsed = tokens.map(parseSyllable).toList();
  final tones = parsed.map((p) => p?.tone).toList();

  // Ánh xạ chữ Hán ↔ âm tiết theo vị trí (bỏ dấu câu trong chuỗi chữ Hán); `r5` (儿 nhi hoá) có thể có hoặc
  // không có chữ riêng ⇒ thử cả hai cách.
  final chars = hanzi == null ? const <String>[] : stripPunctuation(hanzi).runes.map(String.fromCharCode).toList();
  final nonErhua = <int>[];
  for (var i = 0; i < parsed.length; i++) {
    final p = parsed[i];
    if (p != null && p.letters == 'r' && p.tone == 5 && i > 0) continue;
    nonErhua.add(i);
  }
  String? charAt(int index) {
    if (chars.isEmpty) return null;
    if (chars.length == tokens.length) return chars[index];
    if (chars.length == nonErhua.length) {
      final pos = nonErhua.indexOf(index);
      return pos >= 0 ? chars[pos] : null;
    }
    return null;
  }

  final hints = <SandhiHint>[];

  // 3-3: tìm các chuỗi thanh 3 liên tiếp.
  var i = 0;
  while (i < tones.length) {
    if (tones[i] != 3) {
      i++;
      continue;
    }
    var j = i;
    while (j < tones.length && tones[j] == 3) {
      j++;
    }
    final run = j - i;
    if (run >= 2) {
      for (var k = i; k < j - 1; k++) {
        hints.add(
          SandhiHint(
            index: k,
            kind: SandhiKind.thirdTone,
            suggestedTone: 2,
            text: run >= 3 ? 'đọc gần thanh 2 (chuỗi nhiều thanh 3 — tuỳ cách ngắt nhịp)' : 'đọc gần thanh 2 (3-3)',
          ),
        );
      }
    }
    i = j;
  }

  // 不 / 一 theo chữ Hán.
  for (var k = 0; k < tokens.length - 1; k++) {
    final p = parsed[k];
    if (p == null) continue;
    final ch = charAt(k);
    final next = tones[k + 1];
    if (ch == '不' && p.letters == 'bu' && p.tone == 4 && next == 4) {
      hints.add(SandhiHint(index: k, kind: SandhiKind.bu, suggestedTone: 2, text: 'đọc bú (不 trước thanh 4)'));
    } else if (ch == '一' && p.letters == 'yi' && p.tone == 1) {
      if (next == 4) {
        hints.add(SandhiHint(index: k, kind: SandhiKind.yi, suggestedTone: 2, text: 'đọc yí (一 trước thanh 4)'));
      } else if (next == 1 || next == 2 || next == 3) {
        hints.add(SandhiHint(index: k, kind: SandhiKind.yi, suggestedTone: 4, text: 'đọc yì (一 trước thanh 1/2/3)'));
      }
    }
  }

  hints.sort((a, b) => a.index.compareTo(b.index));
  return hints;
}
