import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_session.dart';
import 'auth_state.dart';
import 'identity_client.dart';
import 'install_guard.dart';
import 'models.dart';

/// Phụ thuộc mà APP cung cấp cho [AuthController] (override `authDepsProvider` ở `ProviderScope`).
class AuthDeps {
  const AuthDeps({
    required this.identity,
    required this.session,
    required this.loadMe,
    this.installGuard,
    this.deviceName,
    this.onSignedOut,
    this.logoutTimeout = const Duration(seconds: 5),
  });

  final IdentityClient identity;
  final AuthSession session;

  /// `GET /<ngôn-ngữ>/api/me` của service ngôn ngữ (fail-closed, `skipErrorRedirect`) — quyền CHỈ lấy từ đây (RM-S5).
  final Future<MeInfo> Function() loadMe;

  /// RM-S4 — null trong test.
  final InstallGuard? installGuard;

  /// `deviceName` gửi khi đăng nhập/đăng ký (RM-A6), vd `Android app`.
  final String? deviceName;

  /// Sau khi đăng xuất/mất phiên: app xoá outbox của người dùng, huỷ provider theo người dùng (RM-S7).
  final Future<void> Function(String? userId)? onSignedOut;

  /// `/mobile/logout` chờ tối đa bấy nhiêu, lỗi bỏ qua (RM-S7).
  final Duration logoutTimeout;
}

/// App PHẢI override: `authDepsProvider.overrideWith((ref) => AuthDeps(...))`.
final authDepsProvider = Provider<AuthDeps>((ref) => throw UnimplementedError('authDepsProvider chưa được override'));

/// Nguồn sự thật về phiên đăng nhập cho cả app (port `AuthProvider.tsx`).
///
/// Khởi động: `InstallGuard` → đọc kho → không có ⇒ [AuthAnonymous]; có ⇒ `refresh` ⇒ `loadMe` ⇒ [AuthAuthenticated].
/// Refresh/`loadMe` lỗi mạng ⇒ [AuthUnreachable] (không đăng xuất — RM-S3). `loadMe` 403 ⇒ vẫn authenticated với 0
/// quyền (router đưa `/403`). 401/403 khi refresh ⇒ [AuthAnonymous] `reason=expired`.
class AuthController extends Notifier<AuthState> {
  late AuthDeps _deps;

  /// Id người dùng gần nhất đã biết — để `onSignedOut` nhận đúng id cả khi mất phiên lúc còn `AuthLoading`.
  String? _lastUserId;

  /// Thế hệ luồng ở controller: tăng mỗi khi bắt đầu bootstrap/login/register hoặc đăng xuất/mất phiên. Kết quả
  /// `loadMe` về muộn thuộc thế hệ cũ bị bỏ (không đặt lại AuthAuthenticated cho người cũ).
  int _gen = 0;

  AuthSession get _session => _deps.session;

  @override
  AuthState build() {
    _deps = ref.watch(authDepsProvider);
    final sub = _session.events.listen(_onSessionEvent);
    ref.onDispose(sub.cancel);
    unawaited(bootstrap());
    return const AuthLoading();
  }

  void _set(AuthState next) {
    _lastUserId = next.account?.id ?? _lastUserId;
    if (ref.mounted) state = next;
  }

  /// Nạp phiên từ kho (khởi động, hoặc "Thử lại" từ màn không kết nối được).
  Future<void> bootstrap() async {
    _gen++;
    _set(const AuthLoading());
    try {
      await _deps.installGuard?.ensure();
    } on Object catch (e) {
      afLog('InstallGuard lỗi (${e.runtimeType}) — bỏ qua');
    }
    final stored = await _session.restore();
    if (stored == null) {
      _set(const AuthAnonymous());
      return;
    }
    _lastUserId = stored.account.id;
    try {
      await _session.refresh();
    } on AuthSessionChanged {
      return; // đăng xuất/đăng nhập khác đã xảy ra trong lúc làm mới — luồng kia đã đặt trạng thái
    } on ApiError catch (e) {
      if (e.status == 401 || e.status == 403) {
        _set(const AuthAnonymous(reason: AuthLostReason.expired));
      } else {
        _set(AuthUnreachable(message: e.message, account: stored.account));
      }
      return;
    } on Object catch (e) {
      _set(AuthUnreachable(message: ApiError.from(e).message, account: stored.account));
      return;
    }
    await _loadMeThenAuthenticate(_session.account ?? stored.account);
  }

  /// Trả `true` khi đã đặt được [AuthAuthenticated] (hồ sơ/quyền mới nhất); `false` khi `loadMe` lỗi mạng (trạng
  /// thái chuyển sang [AuthUnreachable]) hoặc kết quả bị bỏ vì phiên đã đổi.
  Future<bool> _loadMeThenAuthenticate(Account account) async {
    final gen = _gen;
    try {
      final me = await _deps.loadMe();
      if (gen != _gen || _session.account?.id != account.id) return false; // đã đăng xuất/đổi người trong lúc chờ
      _set(AuthAuthenticated(account: account, me: me));
      return true;
    } on ApiError catch (e) {
      if (gen != _gen || _session.account?.id != account.id) return false;
      if (e.status == 403) {
        // Tài khoản không có quyền nào ở service này ⇒ vẫn đăng nhập, router đưa /403 (có nút Đăng xuất).
        _set(AuthAuthenticated(account: account, me: MeInfo.empty));
        return true;
      } else if (e.status == 401) {
        // Interceptor trả lại 401 GỐC khi làm mới gặp lỗi mạng/5xx (phiên vẫn còn) — KHÔNG được xoá kho ở đây.
        // Mất phiên thật (refresh bị từ chối) đã đi qua onAuthLost ⇒ kho rỗng ⇒ ẩn danh.
        if (_session.hasStoredSession) {
          _set(AuthUnreachable(message: e.message, account: account));
        } else {
          _set(const AuthAnonymous(reason: AuthLostReason.expired));
        }
      } else {
        _set(AuthUnreachable(message: e.message, account: account));
      }
      return false;
    } on Object catch (e) {
      if (gen != _gen) return false;
      _set(AuthUnreachable(message: ApiError.from(e).message, account: account));
      return false;
    }
  }

  /// Đăng nhập; lỗi [ApiError] ném lên cho màn hình hiện tại chỗ (`describeAuthError`).
  Future<void> login({required String email, required String password}) async {
    final res = await _deps.identity.login(
      LoginRequest(email: email, password: password, deviceName: _deps.deviceName),
    );
    _gen++;
    await _session.setFromAuth(res); // ghi kho trước khi dùng (RM-S2)
    await _loadMeThenAuthenticate(res.account);
  }

  /// Đăng ký; lỗi [ApiError] ném lên cho màn hình.
  Future<void> register({
    required String email,
    required String password,
    required String displayName,
    required String timeZone,
  }) async {
    final res = await _deps.identity.register(
      RegisterRequest(
        email: email,
        password: password,
        displayName: displayName,
        timeZone: timeZone,
        deviceName: _deps.deviceName,
      ),
    );
    _gen++;
    await _session.setFromAuth(res);
    await _loadMeThenAuthenticate(res.account);
  }

  /// Đăng xuất (RM-S7): gọi `/mobile/logout` (tối đa [AuthDeps.logoutTimeout], lỗi bỏ qua) ⇒ xoá kho ⇒ hook app ⇒ ẩn danh.
  /// Xác nhận outbox (nếu có) do app hỏi TRƯỚC khi gọi hàm này.
  Future<void> logout() async {
    _gen++;
    final userId = state.account?.id;
    // Chờ lời gọi làm mới đang bay xong để gửi refresh token MỚI NHẤT (token vừa xoay) tới /mobile/logout.
    await _session.waitForInflight(timeout: _deps.logoutTimeout);
    final rt = _session.refreshToken;
    if (rt != null) {
      try {
        await _deps.identity.logout(rt).timeout(_deps.logoutTimeout);
      } on Object catch (e) {
        afLog('logout: bỏ qua lỗi gọi /mobile/logout (${e.runtimeType})');
      }
    }
    await _session.clear();
    await _signedOut(userId);
    _set(const AuthAnonymous());
  }

  /// Đăng xuất CỤC BỘ (không gọi server) với lý do — dùng sau đổi mật khẩu không giữ được phiên (M4).
  Future<void> signOutLocally(AuthLostReason reason) async {
    _gen++;
    final userId = state.account?.id;
    await _session.clear();
    await _signedOut(userId);
    _set(AuthAnonymous(reason: reason));
  }

  /// Tải lại hồ sơ/quyền (sau khi sửa hồ sơ, hoặc nút Thử lại). Chỉ có tác dụng khi đã đăng nhập.
  Future<void> reloadMe() async {
    final account = state.account;
    if (account == null) return;
    await _loadMeThenAuthenticate(account);
  }

  /// R4-4: làm mới phiên NGAY (xoay refresh token ⇒ claim mới) ⇒ `GET /api/account` ⇒ `loadMe`. Dùng sau `PUT /account`.
  /// Luôn mở LƯỢT LÀM MỚI MỚI (`force`) — lượt đang bay từ trước khi ghi có thể trả token mang claim cũ.
  ///
  /// Trả `true` khi hồ sơ + quyền đã tải lại xong; `false` khi `GET /account`/`loadMe` lỗi mạng (màn hình báo "đã
  /// lưu nhưng chưa tải lại được hồ sơ"). 401/403 khi làm mới ⇒ ném [ApiError] (phiên đã mất).
  Future<bool> refreshSession() async {
    await _session.refresh(force: true); // 401/403 ⇒ sự kiện lost ⇒ ẩn danh; ném lỗi lên người gọi
    var account = _session.account;
    var accountOk = true;
    try {
      account = await _deps.identity.getAccount();
    } on Object {
      // Không lấy được hồ sơ đầy đủ (mạng) ⇒ vẫn có claim mới trong token để hiển thị tên/múi giờ mới.
      accountOk = false;
    }
    if (account == null) return false;
    final meOk = await _loadMeThenAuthenticate(account);
    return accountOk && meOk;
  }

  bool hasPermission(String code) => switch (state) {
    AuthAuthenticated(:final me) => me.permissions.contains(code),
    _ => false,
  };

  Future<void> _signedOut(String? userId) async {
    try {
      await _deps.onSignedOut?.call(userId);
    } on Object catch (e) {
      afLog('onSignedOut lỗi (${e.runtimeType}) — bỏ qua');
    }
  }

  void _onSessionEvent(AuthSessionEvent event) {
    if (event is! AuthSessionLost) return;
    _gen++;
    if (state is AuthAnonymous) return;
    final userId = state.account?.id ?? _lastUserId;
    _set(const AuthAnonymous(reason: AuthLostReason.expired));
    unawaited(_signedOut(userId));
  }
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(AuthController.new);

/// Tài khoản hiện tại (null khi chưa đăng nhập).
final currentAccountProvider = Provider<Account?>((ref) => ref.watch(authControllerProvider).account);

/// Quyền hiện tại (rỗng khi chưa đăng nhập / chưa có hồ sơ).
final permissionsProvider = Provider<Set<String>>(
  (ref) => switch (ref.watch(authControllerProvider)) {
    AuthAuthenticated(:final me) => me.permissions,
    _ => const {},
  },
);
