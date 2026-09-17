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

/// Danh sách dự phòng (~36 múi giờ phổ biến, port `FALLBACK_TIME_ZONES` web) khi plugin không liệt kê được
/// (`flutter_timezone` lỗi, test, nền tảng lạ). Luôn có `Asia/Ho_Chi_Minh`.
const kFallbackTimeZones = <String>[
  'Pacific/Honolulu',
  'America/Anchorage',
  'America/Los_Angeles',
  'America/Denver',
  'America/Chicago',
  'America/New_York',
  'America/Toronto',
  'America/Sao_Paulo',
  'Atlantic/Azores',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Europe/Madrid',
  'Europe/Rome',
  'Europe/Warsaw',
  'Europe/Kyiv',
  'Europe/Moscow',
  'Asia/Dubai',
  'Asia/Karachi',
  'Asia/Kolkata',
  'Asia/Dhaka',
  'Asia/Yangon',
  'Asia/Bangkok',
  'Asia/Ho_Chi_Minh',
  'Asia/Jakarta',
  'Asia/Singapore',
  'Asia/Shanghai',
  'Asia/Hong_Kong',
  'Asia/Taipei',
  'Asia/Manila',
  'Asia/Seoul',
  'Asia/Tokyo',
  'Australia/Perth',
  'Australia/Sydney',
  'Pacific/Auckland',
  'UTC',
];

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

/// Chuẩn hoá danh sách múi giờ để chọn (port `listTimeZoneOptions` web, bỏ phần offset — nhãn chỉ là ID, DB-M4):
/// quy bí danh → bỏ giá trị không giống múi giờ → gộp [extra] (vd giá trị đang lưu của tài khoản, múi giờ máy — để
/// luôn hiện được) → bỏ trùng → sắp theo tên.
List<String> normalizeTimeZoneList(Iterable<String> ids, {Iterable<String> extra = const []}) {
  final seen = <String>{};
  for (final raw in [...ids, ...extra]) {
    final id = normalizeTimeZone(raw);
    if (!looksLikeTimeZone(id)) continue;
    seen.add(id);
  }
  return seen.toList()..sort();
}

/// Danh sách múi giờ IANA của máy (`FlutterTimezone.getAvailableTimezones()` — Android/iOS/web `Intl.supportedValuesOf`)
/// đã chuẩn hoá bằng [normalizeTimeZoneList]; plugin lỗi hoặc trả rỗng/quá ngắn ⇒ [kFallbackTimeZones].
Future<List<String>> listTimeZones({Iterable<String> extra = const []}) async {
  List<String> ids;
  try {
    final infos = await FlutterTimezone.getAvailableTimezones();
    ids = infos.map((i) => i.identifier).toList();
  } on Object catch (e) {
    afLog('listTimeZones: không liệt kê được múi giờ (${e.runtimeType}) — dùng danh sách dự phòng');
    ids = const [];
  }
  // Web cũ (Safari < 15.4) chỉ trả múi giờ hiện tại ⇒ coi như không có danh sách.
  if (ids.length < 5) ids = kFallbackTimeZones;
  return normalizeTimeZoneList(ids, extra: extra);
}

/// Chuẩn hoá để so khớp: thường hoá, `_`/`/`/`-` thành khoảng trắng, gộp khoảng trắng.
String foldTimeZoneForSearch(String s) =>
    s.toLowerCase().replaceAll(RegExp(r'[_/\-]+'), ' ').replaceAll(RegExp(r'\s+'), ' ').trim();

/// So khớp không phân biệt hoa thường, coi `_` như khoảng trắng: gõ "Ho_Chi", "ho chi", "ho chi minh", "asia/ho"
/// đều khớp `Asia/Ho_Chi_Minh`; mỗi từ trong câu hỏi phải xuất hiện (thứ tự tự do — "minh ho chi" vẫn ra).
/// Chuỗi rỗng ⇒ khớp tất cả. Port `matchesTimeZoneQuery` web (không có phần offset).
bool matchesTimeZoneQuery(String id, String query) {
  final q = foldTimeZoneForSearch(query);
  if (q.isEmpty) return true;
  final hay = foldTimeZoneForSearch(id);
  return q.split(' ').every(hay.contains);
}
