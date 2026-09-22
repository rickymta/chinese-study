/// Tiện ích ngày dạng `yyyy-MM-dd` (ngày địa phương theo múi giờ HỒ SƠ do server trả) — port `features/progress/lib/dates.ts`.
///
/// Mọi phép tính dùng `DateTime.utc` để máy ở múi giờ nào cũng ra cùng kết quả — tuyệt đối không `DateTime.parse`
/// chuỗi ngày rồi đọc `.day` theo giờ máy (lệch một ngày ở múi giờ âm).
library;

final RegExp _isoDateRe = RegExp(r'^(\d{4})-(\d{2})-(\d{2})$');

/// `yyyy-MM-dd` ⇒ số ngày kể từ epoch (UTC); chuỗi sai hoặc ngày không tồn tại (31/02) ⇒ `null`.
int? dayNumber(String date) {
  final m = _isoDateRe.firstMatch(date);
  if (m == null) return null;
  final y = int.parse(m.group(1)!);
  final mo = int.parse(m.group(2)!);
  final d = int.parse(m.group(3)!);
  final dt = DateTime.utc(y, mo, d);
  // Ngày không tồn tại ⇒ DateTime.utc tự cuộn sang tháng sau ⇒ coi là sai.
  if (dt.year != y || dt.month != mo || dt.day != d) return null;
  // Nửa đêm UTC luôn là bội số của một ngày ⇒ chia nguyên không mất phần dư (kể cả trước epoch).
  return dt.millisecondsSinceEpoch ~/ Duration.millisecondsPerDay;
}

/// Ngược lại của [dayNumber].
String dateFromDayNumber(int n) {
  final d = DateTime.fromMillisecondsSinceEpoch(n * Duration.millisecondsPerDay, isUtc: true);
  return '${d.year.toString().padLeft(4, '0')}-${_pad2(d.month)}-${_pad2(d.day)}';
}

/// Cộng/trừ ngày; chuỗi sai ⇒ trả nguyên văn.
String addDays(String date, int days) {
  final n = dayNumber(date);
  if (n == null) return date;
  return dateFromDayNumber(n + days);
}

/// 0 = Thứ Hai … 6 = Chủ nhật (tuần bắt đầu Thứ Hai — R-PG6/heatmap). Chuỗi sai ⇒ 0.
int mondayIndex(String date) {
  final n = dayNumber(date);
  if (n == null) return 0;
  // 1970-01-01 là Thứ Năm ⇒ Monday-index 3. `%` của Dart luôn không âm với số chia dương.
  return (n + 3) % 7;
}

/// Tháng (1–12) của ngày; chuỗi sai ⇒ `null`.
int? monthOf(String date) {
  final m = _isoDateRe.firstMatch(date);
  return m == null ? null : int.parse(m.group(2)!);
}

/// `dd/MM` cho nhãn ngắn (nhãn ô lịch). Chuỗi sai ⇒ trả nguyên văn.
String formatDdMm(String date) {
  final m = _isoDateRe.firstMatch(date);
  return m == null ? date : '${m.group(3)}/${m.group(2)}';
}

const _kWeekdayVi = ['Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ nhật'];

/// Tên thứ tiếng Việt theo Monday-index.
String weekdayName(String date) => _kWeekdayVi[mondayIndex(date)];

/// Nhãn thứ viết tắt cho cột trái của lịch: T2…T7, CN.
const kWeekdayShortVi = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

String _pad2(int n) => n < 10 ? '0$n' : '$n';
