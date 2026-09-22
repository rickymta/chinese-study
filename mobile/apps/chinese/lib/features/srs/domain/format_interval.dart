// Định dạng khoảng thời gian dự kiến trên 4 nút chấm (hợp đồng F6/F7 §5.3.2) — port 1-1 `lib/formatInterval.ts` web.
// Đầu vào chính: ISO-8601 duration (`PT1M`, `PT5M30S`, `P8D`, `P1DT2H`). Phòng backend .NET trả `TimeSpan` mặc định
// (`d.hh:mm:ss[.fffffff]`) — nhận luôn dạng đó để nút không hiện "—" oan.

final RegExp _isoDuration = RegExp(
  r'^P(?:(\d+(?:[.,]\d+)?)Y)?(?:(\d+(?:[.,]\d+)?)M)?(?:(\d+(?:[.,]\d+)?)W)?(?:(\d+(?:[.,]\d+)?)D)?'
  r'(?:T(?:(\d+(?:[.,]\d+)?)H)?(?:(\d+(?:[.,]\d+)?)M)?(?:(\d+(?:[.,]\d+)?)S)?)?$',
  caseSensitive: false,
);
final RegExp _dotnetTimeSpan = RegExp(r'^(-)?(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$');

double _num(String? s) => s == null ? 0 : (double.tryParse(s.replaceAll(',', '.')) ?? 0);

/// Đổi chuỗi khoảng thời gian sang tổng số giây. Trả `null` khi không hiểu. Năm/tháng ISO quy ước 365/30 ngày
/// (server không sinh, chỉ phòng hờ).
double? parseDurationSeconds(String? input) {
  if (input == null || input.isEmpty) return null;
  final s = input.trim();
  final iso = _isoDuration.firstMatch(s);
  if (iso != null && s.length > 1) {
    final parts = [for (var i = 1; i <= 7; i++) iso.group(i)];
    if (parts.every((x) => x == null)) return null;
    final days = _num(parts[0]) * 365 + _num(parts[1]) * 30 + _num(parts[2]) * 7 + _num(parts[3]);
    return days * 86400 + _num(parts[4]) * 3600 + _num(parts[5]) * 60 + _num(parts[6]);
  }
  final ts = _dotnetTimeSpan.firstMatch(s);
  if (ts != null) {
    final neg = ts.group(1) != null;
    final frac = ts.group(6);
    final total =
        _num(ts.group(2)) * 86400 +
        _num(ts.group(3)) * 3600 +
        _num(ts.group(4)) * 60 +
        _num(ts.group(5)) +
        (frac == null ? 0 : _num('0.$frac'));
    return neg ? -total : total;
  }
  return null;
}

/// Số thập phân kiểu Việt: 1 chữ số sau dấu phẩy, bỏ `,0` (`5.5` ⇒ "5,5"; `2.0` ⇒ "2").
String _oneDecimal(double v) {
  final rounded = (v * 10).round() / 10;
  var text = rounded.toStringAsFixed(1);
  if (text.endsWith('.0')) text = text.substring(0, text.length - 2);
  return text.replaceAll('.', ',');
}

/// Nhãn Việt ngắn cho khoảng thời gian: `< 1 giờ` ⇒ phút (dưới 10 phút giữ 1 chữ số thập phân), `< 1 ngày` ⇒ giờ,
/// `< 30 ngày` ⇒ ngày, `< 365 ngày` ⇒ tháng (ngày/30, 1 chữ số thập phân), còn lại năm (ngày/365).
/// Không hiểu đầu vào ⇒ "—".
String formatInterval(String? input) {
  final seconds = parseDurationSeconds(input);
  if (seconds == null || !seconds.isFinite || seconds < 0) return '—';
  final minutes = seconds / 60;
  if (minutes < 60) {
    if (minutes < 10) return '${_oneDecimal(minutes)} phút';
    final m = minutes.round();
    if (m < 60) return '$m phút';
    // 59,6 phút làm tròn thành 60 ⇒ rơi xuống nhánh giờ.
  }
  // Làm tròn có thể đẩy lên bậc trên (23 giờ 50 ⇒ "1 ngày") ⇒ kiểm giá trị ĐÃ làm tròn.
  final hours = minutes / 60;
  if (hours < 24) {
    final h = hours.round().clamp(1, 1 << 30);
    if (h < 24) return '$h giờ';
  }
  final days = hours / 24;
  if (days < 30) {
    final d = days.round().clamp(1, 1 << 30);
    if (d < 30) return '$d ngày';
  }
  if (days < 365) return '${_oneDecimal(days / 30)} tháng';
  return '${_oneDecimal(days / 365)} năm';
}
