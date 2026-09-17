// Cách đọc của từng chữ trong từ — port 1-1 `features/dictionary/lib/characterReading.ts` web.
import '../../../core/pinyin/pinyin.dart';

final RegExp _whitespace = RegExp(r'\s+');

/// Khoá so sánh pinyin số: chuẩn hoá (`ü` ⇒ `v`, gộp khoảng trắng) rồi về chữ thường.
String _toKey(String s) => (normalizeNumbered(s) ?? s.trim()).toLowerCase();

/// Cách đọc (pinyin số) của chữ ở vị trí [index] trong từ: ưu tiên âm tiết tương ứng trong [wordPinyin] (`hao3` của
/// 你好 ở vị trí 1) nếu nó nằm trong [readings] của chữ; không thì lấy cách đọc đầu tiên; chữ không có cách đọc ⇒
/// `null`. Âm tiết `r5` (nhi hoá) không có chữ riêng nên bị bỏ khi đếm vị trí (`na3 r5` ⇒ 哪 = `na3`).
String? readingAt(String wordPinyin, int index, List<String>? readings) {
  final list = (readings ?? const []).map(_toKey).where((s) => s.isNotEmpty).toList();
  final syllables = wordPinyin.trim().split(_whitespace).where((s) => s.isNotEmpty && s.toLowerCase() != 'r5').toList();
  final want = index >= 0 && index < syllables.length ? _toKey(syllables[index]) : null;
  if (want != null && want.isNotEmpty && list.contains(want)) return want;
  // Chữ không có danh sách cách đọc (dữ liệu thiếu) ⇒ vẫn dùng âm tiết trong từ.
  if (want != null && want.isNotEmpty && list.isEmpty) return want;
  return list.isEmpty ? null : list.first;
}
