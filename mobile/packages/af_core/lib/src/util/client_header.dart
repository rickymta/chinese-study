import 'package:flutter/foundation.dart';
import 'package:package_info_plus/package_info_plus.dart';

/// Tên header nhận diện client bắt buộc ở mọi endpoint `/api/auth/mobile/*` (hợp đồng mobile RM-A5).
const kClientHeaderName = 'X-AF-Client';

/// Định dạng backend kiểm: `^[a-z0-9-]{1,32}/[0-9A-Za-z.+-]{1,32} \((android|ios|web)\)$`.
final RegExp clientHeaderPattern = RegExp(r'^[a-z0-9-]{1,32}/[0-9A-Za-z.+-]{1,32} \((android|ios|web)\)$');

/// Nền tảng ghi trong header — chỉ 3 giá trị backend chấp nhận.
enum ClientPlatform {
  android,
  ios,
  web;

  /// Suy từ nền tảng đang chạy; nền tảng khác (macOS/Linux khi chạy test/desktop) quy về `web` để header vẫn hợp lệ.
  static ClientPlatform current() {
    if (kIsWeb) return ClientPlatform.web;
    return switch (defaultTargetPlatform) {
      TargetPlatform.android => ClientPlatform.android,
      TargetPlatform.iOS => ClientPlatform.ios,
      _ => ClientPlatform.web,
    };
  }
}

/// Ghép chuỗi `X-AF-Client`, vd `chinese-mobile/0.1.0+1 (android)`.
///
/// [appSlug] chỉ `a-z0-9-` (≤ 32); [version] dạng `0.1.0+1` (≤ 32). Chuỗi kết quả luôn khớp [clientHeaderPattern]
/// — ký tự lạ bị loại, rỗng thay bằng giá trị an toàn để backend không trả 400 vì lỗi ghép header.
String buildClientHeader({required String appSlug, required String version, required ClientPlatform platform}) {
  var slug = appSlug.toLowerCase().replaceAll(RegExp('[^a-z0-9-]'), '');
  if (slug.isEmpty) slug = 'app';
  if (slug.length > 32) slug = slug.substring(0, 32);

  var ver = version.replaceAll(RegExp('[^0-9A-Za-z.+-]'), '');
  if (ver.isEmpty) ver = '0';
  if (ver.length > 32) ver = ver.substring(0, 32);

  return '$slug/$ver (${platform.name})';
}

/// Đọc phiên bản app từ `package_info_plus` rồi ghép header. Plugin lỗi (test, nền tảng lạ) ⇒ dùng [fallbackVersion].
Future<String> resolveClientHeader({
  required String appSlug,
  String fallbackVersion = '0.0.0+0',
  ClientPlatform? platform,
}) async {
  var version = fallbackVersion;
  try {
    final info = await PackageInfo.fromPlatform();
    if (info.version.isNotEmpty) {
      version = info.buildNumber.isEmpty ? info.version : '${info.version}+${info.buildNumber}';
    }
  } on Object {
    // Không có plugin (unit test, nền tảng chưa hỗ trợ) ⇒ giữ fallback.
  }
  return buildClientHeader(appSlug: appSlug, version: version, platform: platform ?? ClientPlatform.current());
}
