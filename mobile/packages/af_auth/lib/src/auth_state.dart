import 'models.dart';

/// Lý do phiên chuyển sang ẩn danh — đưa lên URL `?reason=` để trang đăng nhập giải thích.
enum AuthLostReason {
  /// Refresh bị từ chối (401/403) hoặc gửi lại vẫn 401.
  expired('expired'),

  /// Đổi mật khẩu mà không giữ được phiên hiện tại (M4).
  passwordChanged('password-changed');

  const AuthLostReason(this.queryValue);

  /// Giá trị trên URL (`expired`, `password-changed`).
  final String queryValue;

  static AuthLostReason? fromQuery(String? value) {
    for (final r in values) {
      if (r.queryValue == value) return r;
    }
    return null;
  }
}

/// Trạng thái phiên (hợp đồng mobile §5.3.6). Router (`authRedirect`) và `AuthGate` rẽ nhánh theo đây.
sealed class AuthState {
  const AuthState();

  bool get isAuthenticated => this is AuthAuthenticated;

  /// Tài khoản đang biết (kể cả khi chưa nối được máy chủ) — để hiện tên/email.
  Account? get account => switch (this) {
    AuthAuthenticated(:final account) => account,
    AuthUnreachable(:final account) => account,
    _ => null,
  };
}

/// Đang nạp phiên từ kho lúc khởi động — màn splash, chưa điều hướng.
final class AuthLoading extends AuthState {
  const AuthLoading();
}

/// Chưa đăng nhập; [reason] khác null khi vừa mất phiên (banner ở trang đăng nhập).
final class AuthAnonymous extends AuthState {
  const AuthAnonymous({this.reason});

  final AuthLostReason? reason;
}

/// Đã có access token + hồ sơ/quyền từ service ngôn ngữ.
final class AuthAuthenticated extends AuthState {
  const AuthAuthenticated({required this._account, required this.me});

  final Account _account;

  /// Hồ sơ + quyền từ `GET /me` (fail-closed: 403 ⇒ [MeInfo.empty]).
  final MeInfo me;

  @override
  Account get account => _account;

  Set<String> get permissions => me.permissions;

  bool hasPermission(String code) => me.permissions.contains(code);
}

/// Có phiên trong kho nhưng không nối được máy chủ (mạng/5xx) khi làm mới hoặc tải hồ sơ — KHÔNG đăng xuất
/// (RM-S3); màn "Không kết nối được máy chủ" với Thử lại / Đăng xuất.
final class AuthUnreachable extends AuthState {
  const AuthUnreachable({required this.message, this._account});

  final String message;
  final Account? _account;

  @override
  Account? get account => _account;
}
