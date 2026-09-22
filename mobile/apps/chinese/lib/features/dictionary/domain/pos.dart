// Mã từ loại (theo `complete-hsk-vocabulary`, kiểu ICTCLAS rút gọn) → nhãn tiếng Việt (hợp đồng §5.3.1) — port 1-1
// `features/dictionary/lib/pos.ts` web. Mã KHÔNG có trong bảng (vd `vn`, `b`) ⇒ ẩn — không đoán, không hiện mã thô.

const Map<String, String> kPosLabels = {
  'n': 'danh từ',
  'v': 'động từ',
  'a': 'tính từ',
  'd': 'phó từ',
  'r': 'đại từ',
  'm': 'số từ',
  'q': 'lượng từ',
  'p': 'giới từ',
  'c': 'liên từ',
  'u': 'trợ từ',
  'y': 'trợ từ ngữ khí',
  'e': 'thán từ',
  't': 'từ chỉ thời gian',
  'f': 'từ phương vị',
  's': 'từ chỉ nơi chốn',
  'nr': 'tên người',
  'ns': 'địa danh',
  'nz': 'danh từ riêng',
  'i': 'thành ngữ',
  'l': 'cụm cố định',
  'o': 'từ tượng thanh',
};

/// Nhãn Việt của một mã; mã lạ ⇒ `null`. So khớp không phân biệt hoa/thường, bỏ khoảng trắng thừa.
String? posLabel(String code) => kPosLabels[code.trim().toLowerCase()];

/// Danh sách nhãn Việt (đã loại mã lạ + trùng, giữ thứ tự) từ mảng mã của API.
List<String> posLabels(Iterable<String>? codes) {
  if (codes == null) return const [];
  final out = <String>[];
  for (final c in codes) {
    final label = posLabel(c);
    if (label != null && !out.contains(label)) out.add(label);
  }
  return out;
}
