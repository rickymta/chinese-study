import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth_controller.dart';
import '../auth_state.dart';

/// Mã quyền tối thiểu để dùng mọi màn học (RM-S5).
const kStudyUsePermission = 'study.use';

/// `returnTo` chỉ được là đường dẫn NỘI BỘ (`/on-tap?x=1`) — chặn open redirect kiểu `//evil.com`, `http://...`,
/// `/\evil.com`. Không hợp lệ ⇒ null. Port `sanitizeReturnTo` của `@af/auth`.
String? sanitizeReturnTo(String? value) {
  if (value == null || value.isEmpty) return null;
  if (!value.startsWith('/')) return null;
  if (value.startsWith('//') || value.startsWith(r'/\')) return null;
  return value;
}

/// Dựng `/dang-nhap?returnTo=<đường dẫn hiện tại>&reason=expired`. Không đưa `/` hay chính trang đăng nhập vào
/// `returnTo`. Port `buildLoginUrl` của `@af/auth`.
String buildLoginUrl(String loginPath, Uri current, AuthLostReason? reason) {
  final params = <String, String>{};
  final target = current.hasQuery ? '${current.path}?${current.query}' : current.path;
  if (target != '/' && target.isNotEmpty && !target.startsWith(loginPath)) params['returnTo'] = target;
  if (reason != null) params['reason'] = reason.queryValue;
  if (params.isEmpty) return loginPath;
  return Uri(path: loginPath, queryParameters: params).toString();
}

/// Cấu hình đường dẫn cho [authRedirect] — mặc định theo slug tiếng Việt của mọi app AntFarm.
class AuthRoutePaths {
  const AuthRoutePaths({
    this.login = '/dang-nhap',
    this.register = '/dang-ky',
    this.home = '/',
    this.forbidden = '/403',
    this.public = const {'/401', '/404'},
    this.allowedWithoutStudy = const {'/403', '/them', '/ho-so', '/giay-phep'},
    this.requiredPermission = kStudyUsePermission,
  });

  final String login;
  final String register;
  final String home;
  final String forbidden;

  /// Trang không cần đăng nhập ngoài đăng nhập/đăng ký.
  final Set<String> public;

  /// Trang vẫn vào được khi thiếu [requiredPermission] (để đăng xuất/đọc giấy phép — RM-S5).
  final Set<String> allowedWithoutStudy;
  final String requiredPermission;
}

/// Quy tắc điều hướng theo phiên (hợp đồng mobile §5.3.8), hàm THUẦN để test:
/// - [AuthLoading] ⇒ null (splash do `AuthGate` hiện, không đổi URL).
/// - [AuthUnreachable] ⇒ null, TRỪ khi đang ở đăng nhập/đăng ký (vừa đăng nhập xong mà `/me` lỗi mạng) ⇒ về
///   `returnTo`/`/` để `AuthGate` hiện màn "Không kết nối được" (không để form im lặng, bấm lại tạo họ token mồ côi).
/// - [AuthAnonymous] & đích không phải đăng nhập/đăng ký/public ⇒ `/dang-nhap?returnTo=<đích>[&reason=expired]`.
/// - [AuthAuthenticated] & đang ở đăng nhập/đăng ký ⇒ `returnTo` hợp lệ hoặc `/`.
/// - [AuthAuthenticated] thiếu `study.use` & đích không nằm trong `allowedWithoutStudy` ⇒ `/403`.
String? authRedirect(AuthState state, Uri location, {AuthRoutePaths paths = const AuthRoutePaths()}) {
  final path = location.path.isEmpty ? '/' : location.path;
  final isAuthPage = path == paths.login || path == paths.register;
  switch (state) {
    case AuthLoading():
      return null;
    case AuthUnreachable():
      return isAuthPage ? (sanitizeReturnTo(location.queryParameters['returnTo']) ?? paths.home) : null;
    case AuthAnonymous(:final reason):
      if (isAuthPage || paths.public.contains(path)) return null;
      return buildLoginUrl(paths.login, location, reason);
    case AuthAuthenticated(:final me):
      if (isAuthPage) return sanitizeReturnTo(location.queryParameters['returnTo']) ?? paths.home;
      if (!me.permissions.contains(paths.requiredPermission) && !paths.allowedWithoutStudy.contains(path)) {
        return paths.forbidden;
      }
      return null;
  }
}

/// `Listenable` cho `GoRouter(refreshListenable:)` — phát mỗi khi trạng thái phiên đổi để router chạy lại redirect.
/// Gọi trong thân provider của router (`ref` sống cùng router); tự huỷ khi provider bị huỷ.
Listenable authRefreshListenable(Ref ref) {
  final notifier = _BumpNotifier();
  ref.listen<AuthState>(authControllerProvider, (_, _) => notifier.bump());
  ref.onDispose(notifier.dispose);
  return notifier;
}

class _BumpNotifier extends ChangeNotifier {
  void bump() => notifyListeners();
}
