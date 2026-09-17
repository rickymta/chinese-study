import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/api/clients.dart';
import 'package:af_chinese/app.dart';
import 'package:af_chinese/config/app_config_provider.dart';
import 'package:af_chinese/features/auth/application/auth_providers.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Đọc JSON mẫu trong `test/fixtures/` (chạy từ thư mục app).
Map<String, Object?> loadFixture(String name) {
  final file = File('test/fixtures/$name');
  return jsonDecode(file.readAsStringSync()) as Map<String, Object?>;
}

/// Adapter HTTP giả cho widget test: trả theo [handler]; ném lỗi ⇒ dio bọc thành lỗi mạng.
class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.handler);

  final Future<(int, String)> Function(RequestOptions req) handler;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final (status, body) = await handler(options);
    return ResponseBody.fromString(
      body,
      status,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

const testConfig = AppConfig(
  env: AfEnv.dev,
  identityApiUrl: 'http://test.local/identity/api',
  chineseApiUrl: 'http://test.local/chinese/api',
  isWeb: false,
);

/// Tài khoản/phiên mẫu đã lưu trong kho (như đã đăng nhập ở lần chạy trước).
const testAccount = Account(id: 'u-1', email: 'ban@vidu.com', displayName: 'Quân', timeZone: 'Asia/Ho_Chi_Minh');

StoredSession testStoredSession() =>
    StoredSession(refreshToken: 'rt-0', refreshTokenExpiresAt: DateTime.utc(2099), account: testAccount);

/// JWT không ký cho claim tài khoản mẫu.
String testJwt({String sub = 'u-1', String email = 'ban@vidu.com', String name = 'Quân'}) {
  String enc(Object o) => base64Url.encode(utf8.encode(jsonEncode(o))).replaceAll('=', '');
  return '${enc({'alg': 'none'})}.${enc({'sub': sub, 'email': email, 'name': name, 'zoneinfo': 'Asia/Ho_Chi_Minh'})}.x';
}

/// Thân JSON phản hồi token (login/register/refresh) chuẩn §6.1.
String tokenBody({bool withAccount = false}) => jsonEncode({
  'accessToken': testJwt(),
  'accessTokenExpiresAt': '2099-01-01T00:00:00Z',
  'refreshToken': 'rt-1',
  'refreshTokenExpiresAt': '2099-02-01T00:00:00Z',
  if (withAccount) 'account': testAccount.toJson(),
});

/// Adapter identity: `/auth/mobile/*` trả theo [auth] (mặc định refresh/login/register OK, logout 204), còn lại
/// giao cho [rest] (vd `system/info`). Giữ số lời gọi `system/info` của test cũ không đổi.
FakeAdapter identityStub({required FakeAdapter rest, Future<(int, String)> Function(RequestOptions req)? auth}) =>
    FakeAdapter((req) {
      final path = req.uri.path;
      if (path.contains('/auth/mobile/')) {
        if (auth != null) return auth(req);
        if (path.endsWith('/logout')) return Future.value((204, ''));
        return Future.value((200, tokenBody(withAccount: !path.endsWith('/refresh'))));
      }
      return rest.handler(req);
    });

/// Adapter chinese: `/me` trả hồ sơ với [permissions]; còn lại giao cho [rest].
FakeAdapter chineseStub({required FakeAdapter rest, Set<String> permissions = const {'study.use'}}) =>
    FakeAdapter((req) {
      if (req.uri.path.endsWith('/me')) {
        return Future.value((
          200,
          jsonEncode({
            'id': 'u-1',
            'email': 'ban@vidu.com',
            'displayName': 'Quân',
            'timeZone': 'Asia/Ho_Chi_Minh',
            'roles': ['learner'],
            'permissions': permissions.toList(),
            'firstSeenAt': '2026-09-17T08:00:00Z',
          }),
        ));
      }
      return rest.handler(req);
    });

/// Dựng app đầy đủ (router + theme + provider + phiên) với client giả — dùng cho widget test shell/trang.
///
/// Mặc định [signedIn] ⇒ kho có phiên (như mở lại app) ⇒ refresh + `/me` OK ⇒ vào trang chủ. [permissions] để thử
/// `/403`; [authHandler] để thử lỗi đăng nhập; [tokenStore] để assert kho sau đăng xuất.
Widget buildTestApp({
  required FakeAdapter chineseAdapter,
  required FakeAdapter identityAdapter,
  KeyValueStore? store,
  bool signedIn = true,
  Set<String> permissions = const {'study.use'},
  Future<(int, String)> Function(RequestOptions req)? authHandler,
  InMemoryTokenStore? tokenStore,
}) {
  final tokens = tokenStore ?? InMemoryTokenStore();
  if (signedIn && tokens.session == null) tokens.session = testStoredSession();
  final prefs = store ?? InMemoryKeyValueStore({kInstallFlagKey: true});

  return ProviderScope(
    overrides: [
      appConfigProvider.overrideWithValue(testConfig),
      keyValueStoreProvider.overrideWithValue(prefs),
      tokenStoreProvider.overrideWithValue(tokens),
      chineseAdapterProvider.overrideWithValue(chineseStub(rest: chineseAdapter, permissions: permissions)),
      identityAdapterProvider.overrideWithValue(identityStub(rest: identityAdapter, auth: authHandler)),
      authDepsProvider.overrideWith(buildChineseAuthDeps),
      deviceTimeZoneProvider.overrideWith((_) async => 'Asia/Ho_Chi_Minh'),
    ],
    retry: afNoRetry,
    child: const ChineseApp(),
  );
}

/// Adapter luôn trả `system/info` thành công cho một service.
FakeAdapter okSystemInfo(String service) => FakeAdapter(
  (_) async => (
    200,
    jsonEncode({
      'service': service,
      'version': '0.1.0',
      'environment': 'Development',
      'serverTimeUtc': '2026-09-17T08:00:00Z',
    }),
  ),
);

/// Adapter giả lập service tắt (gateway 502).
FakeAdapter downSystemInfo() => FakeAdapter((_) async => (502, ''));
