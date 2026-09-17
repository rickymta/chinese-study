// Thư mục `lib/config/` là NƠI DUY NHẤT trong app được chứa URL (luật `hardcoded-url` của
// `tool/check_conventions.dart`). Giá trị thật đến từ `--dart-define-from-file=config/<dev-web|dev-android|dev-ios|prod>.json`:
//
//   AF_ENV               dev | prod
//   AF_GATEWAY_URL       gốc gateway dev — Android emulator `http://10.0.2.2:5280`, iOS simulator `http://localhost:5280`;
//                        rỗng khi chạy web ⇒ dùng origin trang (proxy `web_dev_config.yaml` cùng origin)
//   AF_IDENTITY_API_URL  ưu tiên nếu có — prod `https://id.antfarms.xyz/api`
//   AF_CHINESE_API_URL   ưu tiên nếu có — prod `https://chinese.antfarms.xyz/chinese/api`
//
// `AppConfig.fromDartDefine` (af_core) đọc bằng `String.fromEnvironment`; thiếu/không hợp lệ ⇒ `main.dart` hiện màn lỗi.
import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Tên app trong header `X-AF-Client` (`chinese-mobile/<version> (<platform>)`).
const kAppSlug = 'chinese-mobile';

/// Phiên bản dự phòng khi `package_info_plus` không đọc được (khớp `version:` trong pubspec).
const kAppVersionFallback = '0.1.0+1';

/// Cấu hình đã giải — `main.dart` ghi đè bằng giá trị thật qua `ProviderScope(overrides:)`.
final appConfigProvider = Provider<AppConfig>(
  (ref) => throw UnimplementedError('appConfigProvider chưa được override'),
);

/// Chuỗi `X-AF-Client` đã tính ở `main.dart` (đọc phiên bản qua package_info_plus).
final clientHeaderProvider = Provider<String>(
  (ref) => buildClientHeader(appSlug: kAppSlug, version: kAppVersionFallback, platform: ClientPlatform.current()),
);
