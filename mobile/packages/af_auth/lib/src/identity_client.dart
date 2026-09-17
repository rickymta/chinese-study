import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

/// Yêu cầu `POST /api/auth/mobile/register` (hợp đồng mobile §6.1).
class RegisterRequest {
  const RegisterRequest({
    required this.email,
    required this.password,
    required this.displayName,
    required this.timeZone,
    this.deviceName,
  });

  final String email;
  final String password;
  final String displayName;
  final String timeZone;

  /// Tuỳ chọn, ≤ 100 ký tự (RM-A6) — [BA-mặc định] `"<platform> app"`.
  final String? deviceName;

  JsonMap toJson() => {
    'email': email,
    'password': password,
    'displayName': displayName,
    'timeZone': timeZone,
    if (deviceName != null) 'deviceName': deviceName,
  };
}

/// Yêu cầu `POST /api/auth/mobile/login`.
class LoginRequest {
  const LoginRequest({required this.email, required this.password, this.deviceName});

  final String email;
  final String password;
  final String? deviceName;

  JsonMap toJson() => {'email': email, 'password': password, if (deviceName != null) 'deviceName': deviceName};
}

/// Bộ lời gọi identity-service cho client mobile (hợp đồng mobile §6.1). Mọi app ngôn ngữ dùng chung, chỉ khác
/// `Dio` truyền vào (đã có `X-AF-Client` — RM-A5).
///
/// Bốn lời gọi phiên (`/auth/mobile/{register,login,refresh,logout}`) KHÔNG đi qua bước làm mới token khi 401
/// (`af_core` loại trừ theo URL); `/auth/mobile/password` và `/account*` cần Bearer nên vẫn được làm mới + gửi lại.
class IdentityClient {
  IdentityClient(this._dio);

  final Dio _dio;

  /// 201 · `403 REGISTRATION_CLOSED` · `409 EMAIL_TAKEN` · `422 INVALID_TIME_ZONE`.
  Future<MobileAuthResponse> register(RegisterRequest body) async {
    final res = await _dio.post<Object?>('/auth/mobile/register', data: body.toJson());
    return MobileAuthResponse.fromJson(asJsonMap(res.data));
  }

  /// 200 · `401 INVALID_CREDENTIALS` · `403 ACCOUNT_DISABLED` · `423 ACCOUNT_LOCKED` (`details.lockedUntil`).
  Future<MobileAuthResponse> login(LoginRequest body) async {
    final res = await _dio.post<Object?>('/auth/mobile/login', data: body.toJson());
    return MobileAuthResponse.fromJson(asJsonMap(res.data));
  }

  /// 200 (token mới đã xoay) · `401 REFRESH_INVALID` · `403 ACCOUNT_DISABLED` (cả hai ⇒ mất phiên).
  Future<MobileRefreshResponse> refresh(String refreshToken) async {
    final res = await _dio.post<Object?>(
      '/auth/mobile/refresh',
      data: {'refreshToken': refreshToken},
      // Lời gọi ẩn danh: không gắn Bearer cũ (không cần, và token cũ có thể đã hết hạn), không làm mới khi 401.
      options: afOptions(skipAuthRefresh: true, skipAuthHeader: true),
    );
    return MobileRefreshResponse.fromJson(asJsonMap(res.data));
  }

  /// 204 luôn (RM-A7) — không cần Bearer.
  Future<void> logout(String refreshToken) async {
    await _dio.post<Object?>(
      '/auth/mobile/logout',
      data: {'refreshToken': refreshToken},
      options: afOptions(skipAuthRefresh: true, skipErrorRedirect: true),
    );
  }

  /// Bearer; [refreshToken] tuỳ chọn để server giữ họ hiện tại (RM-A8). `422 WRONG_PASSWORD | PASSWORD_UNCHANGED`.
  Future<ChangePasswordResult> changePassword({
    required String currentPassword,
    required String newPassword,
    String? refreshToken,
  }) async {
    final res = await _dio.post<Object?>(
      '/auth/mobile/password',
      data: {'currentPassword': currentPassword, 'newPassword': newPassword, 'refreshToken': ?refreshToken},
    );
    return ChangePasswordResult.fromJson(asJsonMap(res.data));
  }

  /// `GET /api/account` (Bearer, dùng chung với web — RM-A13).
  Future<Account> getAccount() async {
    final res = await _dio.get<Object?>('/account', options: afOptions(skipErrorRedirect: true));
    final account = Account.fromJson(asJsonMap(res.data));
    if (account == null) throw invalidTokenResponse(res.statusCode);
    return account;
  }

  /// `PUT /api/account` `{ displayName, timeZone }` → account · `422 INVALID_TIME_ZONE`.
  Future<Account> updateAccount({required String displayName, required String timeZone}) async {
    final res = await _dio.put<Object?>('/account', data: {'displayName': displayName, 'timeZone': timeZone});
    final account = Account.fromJson(asJsonMap(res.data));
    if (account == null) throw invalidTokenResponse(res.statusCode);
    return account;
  }
}
