/// Môi trường chạy của app (`AF_ENV`).
enum AfEnv { dev, prod }

/// Lỗi cấu hình lúc khởi động — app hiện màn lỗi với [message] thay vì chạy tiếp (hợp đồng mobile §5.3.5).
class AppConfigException implements Exception {
  const AppConfigException(this.message);

  final String message;

  @override
  String toString() => 'AppConfigException: $message';
}

/// Cấu hình máy chủ của app, đọc từ `--dart-define-from-file=config/<...>.json` (hợp đồng mobile §5.3.5, §5.3.9).
///
/// | Khoá | Ý nghĩa |
/// |---|---|
/// | `AF_ENV` | `dev` \| `prod` (mặc định `dev`) |
/// | `AF_GATEWAY_URL` | Gốc gateway dev (`http://10.0.2.2:5280`, `http://localhost:5280`). Rỗng + đang chạy web ⇒ dùng origin trang (proxy dev cùng origin) |
/// | `AF_IDENTITY_API_URL` | Ưu tiên nếu có (prod `https://id.antfarms.xyz/api`) |
/// | `AF_CHINESE_API_URL` | Ưu tiên nếu có (prod `https://chinese.antfarms.xyz/chinese/api`) |
class AppConfig {
  const AppConfig({required this.env, required this.identityApiUrl, required this.chineseApiUrl, required this.isWeb});

  /// Đọc từ `String.fromEnvironment` (phải là hằng biên dịch) rồi [resolve].
  factory AppConfig.fromDartDefine({required bool isWeb, Uri? base}) {
    const env = <String, String>{
      'AF_ENV': String.fromEnvironment('AF_ENV'),
      'AF_GATEWAY_URL': String.fromEnvironment('AF_GATEWAY_URL'),
      'AF_IDENTITY_API_URL': String.fromEnvironment('AF_IDENTITY_API_URL'),
      'AF_CHINESE_API_URL': String.fromEnvironment('AF_CHINESE_API_URL'),
    };
    return resolve(env, isWeb: isWeb, base: base);
  }

  final AfEnv env;

  /// Gốc API identity, KHÔNG có `/` cuối, vd `http://localhost:5280/identity/api`.
  final String identityApiUrl;

  /// Gốc API tiếng Trung, KHÔNG có `/` cuối, vd `http://localhost:5280/chinese/api`.
  final String chineseApiUrl;

  final bool isWeb;

  bool get isProd => env == AfEnv.prod;

  /// Thông điệp hiện khi thiếu cấu hình máy chủ (native không có gateway, không có URL riêng).
  static const missingMessage =
      'Thiếu cấu hình máy chủ — chạy với --dart-define-from-file=config/<dev-web|dev-android|dev-ios|prod>.json';

  /// Hàm thuần để test: suy ra URL từ [env] (giá trị rỗng coi như không đặt).
  ///
  /// - Thiếu cả URL riêng lẫn gateway (và không suy được từ [base] trên web) ⇒ ném [AppConfigException].
  /// - `AF_ENV=prod` mà URL không phải `https://` ⇒ ném [AppConfigException] (không chạy tiếp).
  static AppConfig resolve(Map<String, String> env, {required bool isWeb, Uri? base}) {
    String? read(String key) {
      final v = env[key]?.trim();
      return (v == null || v.isEmpty) ? null : v;
    }

    final envName = read('AF_ENV') ?? 'dev';
    final afEnv = switch (envName) {
      'dev' => AfEnv.dev,
      'prod' => AfEnv.prod,
      _ => throw AppConfigException('AF_ENV không hợp lệ: "$envName" (chỉ nhận dev | prod).'),
    };

    var gateway = read('AF_GATEWAY_URL');
    if (gateway == null && isWeb && base != null && base.hasScheme && base.hasAuthority) {
      // Bản web dev: cùng origin với trang ⇒ đi qua proxy của dev server Flutter (web_dev_config.yaml).
      gateway = base.origin;
    }
    gateway = _stripTrailingSlash(gateway);

    final identity =
        _stripTrailingSlash(read('AF_IDENTITY_API_URL')) ?? (gateway == null ? null : '$gateway/identity/api');
    final chinese =
        _stripTrailingSlash(read('AF_CHINESE_API_URL')) ?? (gateway == null ? null : '$gateway/chinese/api');
    if (identity == null || chinese == null) {
      throw const AppConfigException(missingMessage);
    }

    if (afEnv == AfEnv.prod) {
      for (final url in [identity, chinese]) {
        if (!url.startsWith('https://')) {
          throw AppConfigException('Cấu hình không hợp lệ: AF_ENV=prod yêu cầu URL https:// (nhận "$url").');
        }
      }
    }

    return AppConfig(env: afEnv, identityApiUrl: identity, chineseApiUrl: chinese, isWeb: isWeb);
  }

  static String? _stripTrailingSlash(String? url) {
    if (url == null) return null;
    var u = url;
    while (u.endsWith('/')) {
      u = u.substring(0, u.length - 1);
    }
    return u;
  }

  @override
  String toString() => 'AppConfig(env: $env, identity: $identityApiUrl, chinese: $chineseApiUrl, isWeb: $isWeb)';
}
