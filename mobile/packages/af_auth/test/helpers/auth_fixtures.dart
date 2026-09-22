import 'dart:convert';
import 'dart:io';

import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

/// Đọc JSON mẫu trong `test/fixtures/` (chạy từ thư mục package).
JsonMap loadFixture(String name) => jsonDecode(File('test/fixtures/$name').readAsStringSync()) as JsonMap;

/// JWT KHÔNG ký (header.payload.sig giả) — chỉ để test giải mã claim; app không kiểm chữ ký.
String fakeJwt({
  String sub = 'u-1',
  String email = 'ban@vidu.com',
  String name = 'Quân',
  String zoneinfo = 'Asia/Ho_Chi_Minh',
  Map<String, Object?> extra = const {},
}) {
  String enc(Object o) => base64Url.encode(utf8.encode(jsonEncode(o))).replaceAll('=', '');
  final header = enc({'alg': 'RS256', 'typ': 'JWT'});
  final payload = enc({'sub': sub, 'email': email, 'name': name, 'zoneinfo': zoneinfo, ...extra});
  return '$header.$payload.sig';
}

const testAccount = Account(id: 'u-1', email: 'ban@vidu.com', displayName: 'Quân', timeZone: 'Asia/Ho_Chi_Minh');

StoredSession storedSession({String token = 'rt-0', Account account = testAccount}) =>
    StoredSession(refreshToken: token, refreshTokenExpiresAt: DateTime.utc(2026, 10, 17, 8), account: account);

MobileRefreshResponse refreshResponse({String access = 'at-1', String refresh = 'rt-1', DateTime? expiresAt}) =>
    MobileRefreshResponse(
      accessToken: access,
      accessTokenExpiresAt: expiresAt ?? DateTime.utc(2026, 9, 17, 8, 15),
      refreshToken: refresh,
      refreshTokenExpiresAt: DateTime.utc(2026, 10, 17, 8),
    );

MobileAuthResponse authResponse({String access = 'at-1', String refresh = 'rt-1', Account account = testAccount}) =>
    MobileAuthResponse(
      accessToken: access,
      accessTokenExpiresAt: DateTime.utc(2026, 9, 17, 8, 15),
      refreshToken: refresh,
      refreshTokenExpiresAt: DateTime.utc(2026, 10, 17, 8),
      account: account,
    );

/// Thân request JSON mà adapter giả nhìn thấy (dio giữ `options.data` là Map; chuỗi thì giải mã).
Map<String, Object?> bodyOf(RequestOptions req) {
  final d = req.data;
  if (d is String) return jsonDecode(d) as Map<String, Object?>;
  return (d as Map).map((k, v) => MapEntry(k.toString(), v));
}

/// JSON thân lỗi §6.0.
Map<String, Object?> errorBody(String code, String message, {Map<String, Object?>? details}) => {
  'error': message,
  'code': code,
  'details': ?details,
};

const meWithStudy = MeInfo(
  id: 'u-1',
  email: 'ban@vidu.com',
  displayName: 'Quân',
  timeZone: 'Asia/Ho_Chi_Minh',
  roles: ['learner'],
  permissions: {'study.use'},
);
