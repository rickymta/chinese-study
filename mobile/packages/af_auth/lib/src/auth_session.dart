import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

import 'jwt.dart';
import 'models.dart';
import 'token_store.dart';

/// Sự kiện của [AuthSession] — `AuthController` nghe để đổi trạng thái.
sealed class AuthSessionEvent {
  const AuthSessionEvent();
}

/// Access token đổi (đăng nhập/làm mới) hoặc bị xoá (`null` — đăng xuất/mất phiên).
final class AuthTokenChanged extends AuthSessionEvent {
  const AuthTokenChanged(this.accessToken);

  final String? accessToken;
}

/// Mất phiên: refresh bị từ chối (401/403) hoặc gửi lại vẫn 401 ⇒ về đăng nhập với `reason=expired`.
final class AuthSessionLost extends AuthSessionEvent {
  const AuthSessionLost();
}

/// Ném khi kết quả làm mới về SAU khi phiên đã đổi (đăng xuất / đăng nhập người khác trong lúc refresh đang bay):
/// kết quả bị bỏ, không ghi kho, không coi là mất phiên. KHÔNG phải [ApiError] nên interceptor không gọi `onAuthLost`.
class AuthSessionChanged implements Exception {
  const AuthSessionChanged();

  @override
  String toString() => 'AuthSessionChanged: phiên đăng nhập đã thay đổi trong lúc làm mới.';
}

/// Lời gọi `POST /auth/mobile/refresh` — tách khỏi `IdentityClient` để test không cần Dio và để app nối lười
/// (tránh vòng phụ thuộc provider: Dio identity cần `session.refresh`, session cần Dio identity).
typedef RefreshCall = Future<MobileRefreshResponse> Function(String refreshToken);

/// Phiên xác thực: nguồn sự thật của access token cho MỌI Dio client trong app (hợp đồng mobile §5.3.6, RM-S1..S3).
///
/// - Access token CHỈ trong bộ nhớ; refresh token + tài khoản rút gọn trong [TokenStore] (secure storage).
/// - [refresh] single-flight; nhận token mới ⇒ **ghi kho trước** rồi mới phát access token (RM-S2, RK-M1).
/// - Lỗi mạng/5xx khi làm mới ⇒ thử lại theo [retryDelays] (1 s, 3 s — nằm trong ân hạn 30 s), vẫn lỗi ⇒ GIỮ phiên;
///   chỉ 401/403 mới là mất phiên (xoá kho, phát [AuthSessionLost]).
/// - Chủ động làm mới [refreshLead] trước hạn (Timer) và khi app trở lại foreground ([onAppResumed]).
class AuthSession {
  AuthSession({
    required this._refreshCall,
    required this._store,
    this.refreshLead = const Duration(seconds: 60),
    this.retryDelays = const [Duration(seconds: 1), Duration(seconds: 3)],
    DateTime Function()? now,
    Future<void> Function(Duration delay)? wait,
  }) : _now = now ?? (() => DateTime.now().toUtc()),
       _wait = wait ?? ((d) => Future<void>.delayed(d));

  final RefreshCall _refreshCall;
  final TokenStore _store;
  final DateTime Function() _now;
  final Future<void> Function(Duration delay) _wait;

  /// Làm mới chủ động trước hạn bao lâu (mặc định 60 s).
  final Duration refreshLead;

  /// Khoảng chờ giữa các lần thử lại khi làm mới gặp lỗi mạng/5xx.
  final List<Duration> retryDelays;

  /// Hẹn giờ tối thiểu — tránh xoay liên tục khi đồng hồ máy lệch.
  static const minScheduleDelay = Duration(seconds: 5);

  String? _accessToken;
  DateTime? _accessExpiresAt;
  StoredSession? _stored;
  Future<String>? _inflight;
  int _inflightEpoch = -1;

  /// Thế hệ phiên: tăng ở [clear] và [setFromAuth]. Refresh bắt đầu ở thế hệ cũ mà kết quả về khi thế hệ đã đổi ⇒
  /// bỏ kết quả (chống: đăng xuất "sống lại", phiên A ghi đè phiên B, 401 muộn của A xoá phiên B).
  int _epoch = 0;

  /// Chuỗi tuần tự hoá MỌI thao tác kho (write/clear): trạng thái trên đĩa sau cùng luôn khớp thao tác gọi sau cùng
  /// (clear() gọi sau một write đang treo ⇒ kho rỗng; setFromAuth(B) sau write của A ⇒ kho = B).
  Future<void> _storeOps = Future<void>.value();
  Timer? _timer;
  bool _disposed = false;
  // `sync`: người nghe (AuthController) nhận ngay trong cùng lượt — trạng thái đổi trước khi Future refresh hoàn tất.
  final _events = StreamController<AuthSessionEvent>.broadcast(sync: true);

  /// Access token trong bộ nhớ (null khi chưa đăng nhập / chưa làm mới sau khi mở app).
  String? get accessToken => _accessToken;
  DateTime? get accessExpiresAt => _accessExpiresAt;

  /// Phiên đã lưu (refresh token + tài khoản rút gọn) — có sau [restore]/[setFromAuth].
  StoredSession? get stored => _stored;
  bool get hasStoredSession => _stored != null;

  /// Refresh token hiện tại (để gọi `/mobile/logout`, `/mobile/password`). KHÔNG log, KHÔNG đưa lên URL.
  String? get refreshToken => _stored?.refreshToken;

  /// Tài khoản rút gọn đang biết (từ kho hoặc claim JWT sau khi làm mới).
  Account? get account => _stored?.account;

  Stream<AuthSessionEvent> get events => _events.stream;

  /// Đọc kho lúc khởi động (kho hỏng ⇒ null, kho đã tự xoá — RM-S4).
  Future<StoredSession?> restore() async {
    _stored = await _store.read();
    return _stored;
  }

  /// Sau đăng nhập/đăng ký: ghi kho rồi mới giữ access token (RM-S2). Mở thế hệ phiên mới — refresh đang bay của
  /// phiên trước (nếu có) sẽ bị bỏ khi về.
  Future<void> setFromAuth(MobileAuthResponse res) {
    _epoch++;
    return _apply(tokens: res, account: res.account, epoch: _epoch);
  }

  /// Xếp [op] vào chuỗi thao tác kho; lỗi của [op] ném cho người gọi nhưng không làm gãy chuỗi.
  Future<void> _storeOp(Future<void> Function() op) {
    final next = _storeOps.then((_) => op());
    _storeOps = next.then<void>((_) {}, onError: (Object _) {});
    return next;
  }

  /// Chờ lời gọi làm mới đang bay (nếu có) kết thúc — nuốt lỗi, tối đa [timeout]. Dùng trước khi đăng xuất để gửi
  /// đúng refresh token MỚI NHẤT tới `/mobile/logout`.
  Future<void> waitForInflight({Duration timeout = const Duration(seconds: 5)}) async {
    final f = _inflight;
    if (f == null) return;
    await f.then<void>((_) {}, onError: (Object _) {}).timeout(timeout, onTimeout: () {});
  }

  /// Có Timer làm mới chủ động đang chờ (để test kiểm sau khi đăng xuất).
  @visibleForTesting
  bool get hasRefreshTimer => _timer != null;

  /// Ghi kho rồi (nếu thế hệ [epoch] vẫn hiện hành) giữ token trong bộ nhớ. Trả `false` khi phiên đã đổi trong lúc
  /// đang ghi (clear()/setFromAuth() chen giữa) ⇒ KHÔNG đụng bộ nhớ, hẹn giờ hay sự kiện (đăng xuất không "sống lại").
  Future<bool> _apply({required MobileRefreshResponse tokens, required Account account, required int epoch}) async {
    final session = StoredSession(
      refreshToken: tokens.refreshToken,
      refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
      account: account,
    );
    // RM-S2 — ghi kho TRƯỚC khi phát access token cho request đang chờ (giảm cửa sổ mất token khi app bị tắt).
    // Kho ghi lỗi ⇒ vẫn dùng trong bộ nhớ tới khi app tắt (không chặn người học), ghi log để chẩn đoán.
    try {
      await _storeOp(() => _store.write(session));
    } on Object catch (e) {
      afLog('AuthSession: không ghi được kho (${e.runtimeType}) — tiếp tục trong bộ nhớ');
    }
    if (_disposed || epoch != _epoch) return false;
    _stored = session;
    _accessToken = tokens.accessToken;
    _accessExpiresAt = tokens.accessTokenExpiresAt;
    _schedule();
    _emit(AuthTokenChanged(tokens.accessToken));
    return true;
  }

  /// Làm mới access token bằng refresh token trong kho — single-flight: mọi lời gọi đồng thời dùng chung một Future.
  ///
  /// [force] ⇒ KHÔNG dùng chung lượt đang bay (lượt đó bắt đầu TRƯỚC, token trả về có thể mang claim cũ — vd vừa
  /// `PUT /account`): chờ lượt đó xong rồi mở lượt mới, để token chắc chắn phản ánh dữ liệu sau khi ghi.
  ///
  /// Trả access token mới. Ném [ApiError]: 401/403 ⇒ phiên đã bị xoá + phát [AuthSessionLost]; lỗi mạng/5xx sau
  /// khi thử lại ⇒ giữ nguyên phiên; không có phiên ⇒ 401 `NO_SESSION` (không phát sự kiện).
  Future<String> refresh({bool force = false}) {
    final existing = _inflight;
    // Lời gọi đang bay thuộc thế hệ hiện tại thì dùng chung; thuộc thế hệ cũ (đã đăng xuất/đăng nhập lại) thì mở mới.
    if (existing != null && _inflightEpoch == _epoch) {
      if (!force) return existing;
      // Lượt mới xếp SAU lượt đang bay (không chạy song song — hai lần xoay cùng token cha sẽ đụng ân hạn).
      final epoch = _epoch;
      late final Future<String> future;
      future = existing
          .then<void>((_) {}, onError: (Object _) {})
          .then((_) => epoch == _epoch ? _doRefresh(epoch) : throw const AuthSessionChanged())
          .whenComplete(() {
            if (identical(_inflight, future)) _inflight = null;
          });
      _inflight = future;
      _inflightEpoch = epoch;
      return future;
    }
    final epoch = _epoch;
    late final Future<String> future;
    future = _doRefresh(epoch).whenComplete(() {
      if (identical(_inflight, future)) _inflight = null;
    });
    _inflight = future;
    _inflightEpoch = epoch;
    return future;
  }

  Future<String> _doRefresh(int epoch) async {
    final current = _stored;
    if (current == null) throw ApiError('Chưa đăng nhập.', status: 401, code: 'NO_SESSION');
    var attempt = 0;
    while (true) {
      try {
        final res = await _refreshCall(current.refreshToken);
        if (epoch != _epoch) throw const AuthSessionChanged();
        // Claim JWT mới (`name`, `zoneinfo`) là bản mới nhất sau khi sửa hồ sơ (R4-4); giữ `createdAt` đã biết.
        final fromToken = accountFromToken(res.accessToken);
        final account = fromToken == null
            ? current.account
            : current.account.copyWith(
                email: fromToken.email.isEmpty ? null : fromToken.email,
                displayName: fromToken.displayName.isEmpty ? null : fromToken.displayName,
                timeZone: fromToken.timeZone,
              );
        if (!await _apply(tokens: res, account: account, epoch: epoch)) throw const AuthSessionChanged();
        return res.accessToken;
      } on ApiError catch (e) {
        // Lỗi về khi phiên đã đổi (đăng xuất/đăng nhập khác) ⇒ không được xoá phiên MỚI, cũng không thử lại.
        if (epoch != _epoch) throw const AuthSessionChanged();
        if (e.status == 401 || e.status == 403) {
          // REFRESH_INVALID / ACCOUNT_DISABLED: phiên thật sự mất.
          await _lose();
          rethrow;
        }
        final retryable = e.isNetwork || (e.status != null && e.status! >= 500);
        if (retryable && attempt < retryDelays.length) {
          afLog('AuthSession: làm mới lỗi (${e.status ?? 'mạng'}) — thử lại sau ${retryDelays[attempt].inSeconds}s');
          await _wait(retryDelays[attempt]);
          if (epoch != _epoch) throw const AuthSessionChanged();
          attempt++;
          continue;
        }
        rethrow; // lỗi mạng dai dẳng ⇒ giữ phiên, người gọi báo "Không kết nối được máy chủ"
      }
    }
  }

  /// Xoá token + huỷ hẹn giờ (đăng xuất, mất phiên). KHÔNG phát [AuthSessionLost].
  Future<void> clear() async {
    _epoch++; // refresh đang bay (nếu có) sẽ bị bỏ khi về — đăng xuất không "sống lại"
    _cancelTimer();
    final had = _accessToken != null;
    _accessToken = null;
    _accessExpiresAt = null;
    _stored = null;
    await _storeOp(_store.clear); // xếp sau write đang treo (nếu có) ⇒ đĩa sau cùng là rỗng
    if (had) _emit(const AuthTokenChanged(null));
  }

  Future<void> _lose() async {
    final had = _stored != null || _accessToken != null;
    await clear();
    if (had) _emit(const AuthSessionLost());
  }

  /// Gọi từ `createApiClient(onAuthLost:)`: refresh bị từ chối hoặc gửi lại vẫn 401.
  void handleAuthLost() {
    unawaited(_lose());
  }

  /// App trở lại foreground: token còn ≤ [refreshLead] (hoặc đã hết hạn) ⇒ làm mới ngay (Timer có thể bị hệ điều
  /// hành hãm khi app ở nền).
  void onAppResumed() {
    final exp = _accessExpiresAt;
    if (_accessToken == null || exp == null) return;
    if (exp.difference(_now()) <= refreshLead) _refreshSilently();
  }

  void _schedule() {
    _cancelTimer();
    final exp = _accessExpiresAt;
    if (exp == null) return;
    var delay = exp.difference(_now()) - refreshLead;
    if (delay < minScheduleDelay) delay = minScheduleDelay;
    _timer = Timer(delay, _refreshSilently);
  }

  void _refreshSilently() {
    // Lỗi ở đây được nuốt: token cũ vẫn dùng được tới hạn, request 401 kế tiếp sẽ thử làm mới lần nữa.
    unawaited(refresh().then<void>((_) {}, onError: (Object _) {}));
  }

  void _cancelTimer() {
    _timer?.cancel();
    _timer = null;
  }

  void _emit(AuthSessionEvent e) {
    if (!_events.isClosed) _events.add(e);
  }

  void dispose() {
    _disposed = true;
    _cancelTimer();
    unawaited(_events.close());
  }
}
