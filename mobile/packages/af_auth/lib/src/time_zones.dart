import 'package:af_core/af_core.dart';
import 'package:flutter_timezone/flutter_timezone.dart';

import 'models.dart';

/// Bí danh CLDR cũ → tên IANA hiện hành (R4-2; port `timeZones.ts` của `@af/utils`). Backend nhận cả hai nhưng gửi
/// tên hiện hành để dữ liệu nhất quán.
const kTimeZoneAliases = <String, String>{
  'Asia/Saigon': 'Asia/Ho_Chi_Minh',
  'Asia/Calcutta': 'Asia/Kolkata',
  'Asia/Katmandu': 'Asia/Kathmandu',
  'Asia/Rangoon': 'Asia/Yangon',
  'Europe/Kiev': 'Europe/Kyiv',
};

/// Quy bí danh cũ về tên hiện hành; tên khác giữ nguyên (đã trim).
String normalizeTimeZone(String id) {
  final trimmed = id.trim();
  return kTimeZoneAliases[trimmed] ?? trimmed;
}

/// Múi giờ IANA hợp lệ về mặt hình thức: `Vùng/Thành_phố` (có thể 3 cấp), hoặc `UTC`/`Etc/...`; ≤ 64 ký tự
/// (cột `time_zone varchar(64)`). Tính hợp lệ thật do backend kiểm (`422 INVALID_TIME_ZONE`).
bool looksLikeTimeZone(String id) {
  if (id.isEmpty || id.length > 64) return false;
  if (id == 'UTC' || id == 'GMT') return true;
  return RegExp(r'^[A-Za-z]+(/[A-Za-z0-9_+\-]+){1,2}$').hasMatch(id);
}

/// Múi giờ của thiết bị qua `flutter_timezone` (đã quy bí danh). Plugin lỗi/không có (test, nền tảng lạ) hoặc
/// giá trị không hợp lệ ⇒ [fallback] (mặc định `Asia/Ho_Chi_Minh`).
Future<String> deviceTimeZone({String fallback = kDefaultTimeZone}) async {
  try {
    final info = await FlutterTimezone.getLocalTimezone();
    final tz = normalizeTimeZone(info.identifier);
    return looksLikeTimeZone(tz) ? tz : fallback;
  } on Object catch (e) {
    afLog('deviceTimeZone: không đọc được múi giờ máy (${e.runtimeType}) — dùng $fallback');
    return fallback;
  }
}
