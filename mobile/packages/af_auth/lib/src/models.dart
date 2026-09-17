import 'package:af_core/af_core.dart';

/// Múi giờ mặc định của người học Việt (khớp identity-service `users.time_zone` mặc định).
const kDefaultTimeZone = 'Asia/Ho_Chi_Minh';

/// `account` trong phản hồi register/login mobile và `GET /api/account` (hợp đồng mobile §6.1).
///
/// [createdAt] chỉ có khi lấy từ API; tài khoản dựng từ claim JWT (sau làm mới) hoặc từ kho không có.
class Account {
  const Account({
    required this.id,
    required this.email,
    required this.displayName,
    required this.timeZone,
    this.createdAt,
  });

  /// Thiếu khoá ⇒ chuỗi rỗng (không ném); `timeZone` rỗng ⇒ [kDefaultTimeZone]. Không phải Map ⇒ null.
  static Account? fromJson(JsonMap? json) {
    if (json == null) return null;
    final id = readString(json, 'id');
    if (id == null || id.isEmpty) return null;
    final tz = readString(json, 'timeZone');
    return Account(
      id: id,
      email: readStringOr(json, 'email'),
      displayName: readStringOr(json, 'displayName'),
      timeZone: (tz == null || tz.isEmpty) ? kDefaultTimeZone : tz,
      createdAt: readDateTime(json, 'createdAt'),
    );
  }

  final String id;
  final String email;
  final String displayName;

  /// Múi giờ IANA, vd `Asia/Ho_Chi_Minh`.
  final String timeZone;
  final DateTime? createdAt;

  JsonMap toJson() => {
    'id': id,
    'email': email,
    'displayName': displayName,
    'timeZone': timeZone,
    if (createdAt != null) 'createdAt': createdAt!.toUtc().toIso8601String(),
  };

  Account copyWith({String? email, String? displayName, String? timeZone, DateTime? createdAt}) => Account(
    id: id,
    email: email ?? this.email,
    displayName: displayName ?? this.displayName,
    timeZone: timeZone ?? this.timeZone,
    createdAt: createdAt ?? this.createdAt,
  );

  @override
  bool operator ==(Object other) =>
      other is Account &&
      other.id == id &&
      other.email == email &&
      other.displayName == displayName &&
      other.timeZone == timeZone &&
      other.createdAt == createdAt;

  @override
  int get hashCode => Object.hash(id, email, displayName, timeZone, createdAt);

  @override
  String toString() => 'Account($id, $email)';
}

/// Ném khi phản hồi token của identity thiếu `accessToken`/`refreshToken` (URL cấu hình sai trả HTML/JSON lạ —
/// không được ghi token rác vào kho rồi nổ ở chỗ giải mã JWT, RK25 của web).
ApiError invalidTokenResponse([int? status]) =>
    ApiError('Phản hồi máy chủ không hợp lệ — kiểm tra cấu hình AF_IDENTITY_API_URL hoặc proxy dev.', status: status);

/// `POST /api/auth/mobile/refresh` → 200 (RM-A2: refresh token MỚI đã xoay, token cũ hết dùng sau ân hạn 30 s).
class MobileRefreshResponse {
  const MobileRefreshResponse({
    required this.accessToken,
    required this.accessTokenExpiresAt,
    required this.refreshToken,
    required this.refreshTokenExpiresAt,
  });

  /// Ném [ApiError] khi thiếu token; hạn thiếu/sai ⇒ giả định 15 phút (access) / 30 ngày (refresh) tính từ [now].
  factory MobileRefreshResponse.fromJson(JsonMap? json, {DateTime? now}) {
    final access = readString(json, 'accessToken');
    final refresh = readString(json, 'refreshToken');
    if (access == null || access.isEmpty || refresh == null || refresh.isEmpty) throw invalidTokenResponse();
    final base = now ?? DateTime.now().toUtc();
    return MobileRefreshResponse(
      accessToken: access,
      accessTokenExpiresAt: readDateTime(json, 'accessTokenExpiresAt') ?? base.add(const Duration(minutes: 15)),
      refreshToken: refresh,
      refreshTokenExpiresAt: readDateTime(json, 'refreshTokenExpiresAt') ?? base.add(const Duration(days: 30)),
    );
  }

  final String accessToken;
  final DateTime accessTokenExpiresAt;
  final String refreshToken;
  final DateTime refreshTokenExpiresAt;
}

/// `POST /api/auth/mobile/register` → 201 · `POST /api/auth/mobile/login` → 200 (token + tài khoản).
class MobileAuthResponse extends MobileRefreshResponse {
  const MobileAuthResponse({
    required super.accessToken,
    required super.accessTokenExpiresAt,
    required super.refreshToken,
    required super.refreshTokenExpiresAt,
    required this.account,
  });

  /// Ném [ApiError] khi thiếu token hoặc thiếu `account.id`.
  factory MobileAuthResponse.fromJson(JsonMap? json, {DateTime? now}) {
    final tokens = MobileRefreshResponse.fromJson(json, now: now);
    final account = Account.fromJson(readMap(json, 'account'));
    if (account == null) throw invalidTokenResponse();
    return MobileAuthResponse(
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: tokens.accessTokenExpiresAt,
      refreshToken: tokens.refreshToken,
      refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
      account: account,
    );
  }

  final Account account;
}

/// `POST /api/auth/mobile/password` → 200.
class ChangePasswordResult {
  const ChangePasswordResult({required this.otherSessionsRevoked, required this.currentSessionKept});

  factory ChangePasswordResult.fromJson(JsonMap? json) => ChangePasswordResult(
    otherSessionsRevoked: readIntOr(json, 'otherSessionsRevoked'),
    // Thiếu ⇒ coi như KHÔNG giữ được phiên (an toàn: app đăng xuất cục bộ).
    currentSessionKept: readBoolOr(json, 'currentSessionKept'),
  );

  /// Số họ refresh token KHÁC đã bị thu hồi.
  final int otherSessionsRevoked;

  /// `false` ⇒ mọi phiên (kể cả phiên này) bị thu hồi ⇒ app về `/dang-nhap?reason=password-changed`.
  final bool currentSessionKept;
}

/// Hồ sơ + quyền do SERVICE NGÔN NGỮ trả (`GET /<ngôn-ngữ>/api/me`, HĐG §6.3) — nguồn sự thật DUY NHẤT về quyền
/// (RM-S5). `af_auth` chỉ cần [permissions]; app đọc thêm các trường còn lại.
class MeInfo {
  const MeInfo({
    required this.id,
    required this.email,
    required this.displayName,
    required this.timeZone,
    required this.roles,
    required this.permissions,
    this.firstSeenAt,
  });

  /// Fail-closed như `loadMe.ts` web: thiếu/sai kiểu `permissions` ⇒ 0 quyền (router đưa `/403`), không ném.
  factory MeInfo.fromJson(JsonMap? json) {
    final tz = readString(json, 'timeZone');
    return MeInfo(
      id: readStringOr(json, 'id'),
      email: readStringOr(json, 'email'),
      displayName: readStringOr(json, 'displayName'),
      timeZone: (tz == null || tz.isEmpty) ? kDefaultTimeZone : tz,
      roles: List.unmodifiable(readPrimitiveList<String>(json, 'roles')),
      permissions: Set.unmodifiable(readPrimitiveList<String>(json, 'permissions')),
      firstSeenAt: readDateTime(json, 'firstSeenAt'),
    );
  }

  /// Hồ sơ rỗng, 0 quyền — dùng khi `GET /me` trả 403 (tài khoản bị gỡ hết vai trò).
  static const MeInfo empty = MeInfo(
    id: '',
    email: '',
    displayName: '',
    timeZone: kDefaultTimeZone,
    roles: [],
    permissions: {},
  );

  final String id;
  final String email;
  final String displayName;

  /// Múi giờ đã đồng bộ ở service ngôn ngữ — "hôm nay" của người học tính theo đây.
  final String timeZone;

  /// Mã vai trò cục bộ: `admin`, `learner`.
  final List<String> roles;

  /// Mã quyền cục bộ: `study.use`, `content.manage`, `users.manage`.
  final Set<String> permissions;
  final DateTime? firstSeenAt;

  bool has(String permission) => permissions.contains(permission);
}

/// Nội dung lưu trong `flutter_secure_storage` khoá `af.auth.session` (hợp đồng mobile §5.1.2).
///
/// Access token KHÔNG bao giờ nằm ở đây (RM-S1) — chỉ trong bộ nhớ của `AuthSession`.
class StoredSession {
  const StoredSession({required this.refreshToken, required this.refreshTokenExpiresAt, required this.account});

  /// Phiên bản định dạng; đổi cấu trúc ⇒ tăng số, bản cũ bị coi là không đọc được (xoá kho, đăng nhập lại).
  static const version = 1;

  /// JSON hỏng/sai phiên bản/thiếu token ⇒ null (người gọi xoá kho — RM-S4).
  static StoredSession? fromJson(JsonMap? json) {
    if (json == null || readInt(json, 'v') != version) return null;
    final token = readString(json, 'refreshToken');
    final account = Account.fromJson(readMap(json, 'account'));
    if (token == null || token.isEmpty || account == null) return null;
    return StoredSession(
      refreshToken: token,
      refreshTokenExpiresAt: readDateTime(json, 'refreshTokenExpiresAt') ?? DateTime.now().toUtc(),
      account: account,
    );
  }

  final String refreshToken;
  final DateTime refreshTokenExpiresAt;

  /// Tài khoản rút gọn để hiện tên ngay khi mở app (trước khi làm mới xong).
  final Account account;

  JsonMap toJson() => {
    'v': version,
    'refreshToken': refreshToken,
    'refreshTokenExpiresAt': refreshTokenExpiresAt.toUtc().toIso8601String(),
    'account': account.toJson(),
  };

  /// Kho được xem là hết hạn khi refresh token đã quá hạn theo đồng hồ máy — vẫn thử làm mới một lần (server là
  /// nguồn sự thật), chỉ dùng để hiển thị.
  bool isExpiredAt(DateTime now) => !refreshTokenExpiresAt.isAfter(now);
}
