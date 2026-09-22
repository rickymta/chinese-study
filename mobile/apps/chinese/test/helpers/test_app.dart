import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/api/clients.dart';
import 'package:af_chinese/app.dart';
import 'package:af_chinese/config/app_config_provider.dart';
import 'package:af_chinese/features/auth/application/auth_providers.dart';
import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_chinese/router/router.dart';
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
String testJwt({
  String sub = 'u-1',
  String email = 'ban@vidu.com',
  String name = 'Quân',
  String timeZone = 'Asia/Ho_Chi_Minh',
}) {
  String enc(Object o) => base64Url.encode(utf8.encode(jsonEncode(o))).replaceAll('=', '');
  return '${enc({'alg': 'none'})}.${enc({'sub': sub, 'email': email, 'name': name, 'zoneinfo': timeZone})}.x';
}

/// Thân JSON phản hồi token (login/register/refresh) chuẩn §6.1. [name]/[timeZone] để giả token mang claim mới sau
/// khi sửa hồ sơ (R4-4); [sub]/[email] để giả người dùng KHÁC đăng nhập (test đổi tài khoản).
String tokenBody({
  bool withAccount = false,
  String name = 'Quân',
  String timeZone = 'Asia/Ho_Chi_Minh',
  String sub = 'u-1',
  String email = 'ban@vidu.com',
}) => jsonEncode({
  'accessToken': testJwt(sub: sub, email: email, name: name, timeZone: timeZone),
  'accessTokenExpiresAt': '2099-01-01T00:00:00Z',
  'refreshToken': 'rt-1',
  'refreshTokenExpiresAt': '2099-02-01T00:00:00Z',
  if (withAccount) 'account': Account(id: sub, email: email, displayName: name, timeZone: timeZone).toJson(),
});

/// Adapter identity: `/auth/mobile/*` trả theo [auth] (mặc định refresh/login/register OK, logout 204), còn lại
/// giao cho [rest] (vd `system/info`, `/account`). Giữ số lời gọi `system/info` của test cũ không đổi. Mọi request
/// (kể cả `/me`, refresh) được ghi vào [log] nếu có — để test đếm lời gọi.
FakeAdapter identityStub({
  required FakeAdapter rest,
  Future<(int, String)> Function(RequestOptions req)? auth,
  List<RequestOptions>? log,
}) => FakeAdapter((req) {
  log?.add(req);
  final path = req.uri.path;
  if (path.contains('/auth/mobile/')) {
    if (auth != null) return auth(req);
    if (path.endsWith('/logout')) return Future.value((204, ''));
    return Future.value((200, tokenBody(withAccount: !path.endsWith('/refresh'))));
  }
  return rest.handler(req);
});

/// Phản hồi mặc định của `/auth/mobile/*` (để handler tuỳ biến trong test uỷ quyền phần còn lại).
Future<(int, String)> defaultAuthResponse(RequestOptions req) {
  final path = req.uri.path;
  if (path.endsWith('/logout')) return Future.value((204, ''));
  return Future.value((200, tokenBody(withAccount: !path.endsWith('/refresh'))));
}

/// Thân JSON `/progress/overview` mặc định cho widget test: người mới (chuỗi 0, mọi khối có nhưng bằng 0).
String defaultOverviewBody() => File('test/fixtures/progress_overview_new_user.json').readAsStringSync();

/// Thân JSON `/progress/overview` từ fixture (`progress_overview_full.json`, `_minimal.json`, `_new_user.json`).
String overviewFixture(String name) => File('test/fixtures/$name').readAsStringSync();

/// Thân JSON `/srs/summary` suy từ khối `srs` + `localDate`/`timeZone` của một thân `/progress/overview` — để huy hiệu
/// "Ôn tập" trong test (nguồn: tóm tắt SRS từ M6) khớp số của fixture tổng quan như trước. Các trường còn lại mặc định.
String summaryFromOverview(String overviewBody) {
  final o = asJsonMap(jsonDecode(overviewBody)) ?? const {};
  final srs = readMap(o, 'srs') ?? const {};
  return jsonEncode({
    'localDate': readStringOr(o, 'localDate', '2026-09-17'),
    'timeZone': readStringOr(o, 'timeZone', 'Asia/Ho_Chi_Minh'),
    'dueToday': readIntOr(srs, 'dueToday'),
    'dueNow': readIntOr(srs, 'dueNow'),
    'reviewedToday': readIntOr(srs, 'reviewedToday'),
    'reviewsDoneToday': 0,
    'reviewLimitRemaining': 200,
    'dailyReviewLimit': 200,
    'newIntroducedToday': readIntOr(srs, 'newIntroducedToday'),
    'newAvailableToday': readIntOr(srs, 'newAvailableToday'),
    'dailyNewCards': 10,
    'totalCards': 0,
    'matureCards': 0,
  });
}

/// Adapter chinese: `/me` trả hồ sơ với [permissions]; `/progress/overview` trả theo [overview] (null ⇒ giao cho
/// [rest] — test lỗi tự trả 503/502); `/srs/*` trả theo [srs] (null ⇒ `/srs/summary` suy từ [overview] nếu có, còn lại
/// giao cho [rest]); còn lại giao cho [rest].
FakeAdapter chineseStub({
  required FakeAdapter rest,
  Set<String> permissions = const {'study.use'},
  List<RequestOptions>? log,
  String Function()? meId,
  Future<(int, String)> Function(RequestOptions req)? overview,
  Future<(int, String)> Function(RequestOptions req)? srs,
}) => FakeAdapter((req) async {
  log?.add(req);
  if (req.uri.path.endsWith('/progress/overview') && overview != null) return overview(req);
  if (req.uri.path.contains('/srs/')) {
    if (srs != null) return srs(req);
    if (req.uri.path.endsWith('/srs/summary') && overview != null) {
      final (status, body) = await overview(req);
      return status == 200 ? (200, summaryFromOverview(body)) : (status, body);
    }
  }
  if (req.uri.path.endsWith('/me')) {
    return Future.value((
      200,
      jsonEncode({
        'id': meId?.call() ?? 'u-1',
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

/// Thân JSON cài đặt học tập (`GET/PUT /me/learning-settings`) theo [s].
String learningSettingsBody(LearningSettings s) => jsonEncode({...s.toJson(), 'isDefault': s.isDefault});

/// Adapter chinese trả `system/info` OK và `GET/PUT /me/learning-settings` (PUT ⇒ lưu lại vào [state], trả
/// `isDefault=false`) — cho test hồ sơ/giọng đọc. [onPut] tuỳ biến phản hồi PUT (vd 400 details).
FakeAdapter chineseWithSettings({
  LearningSettings initial = LearningSettings.defaults,
  Future<(int, String)>? Function(RequestOptions req)? onPut,
  List<LearningSettings>? saved,
}) {
  var state = initial;
  return FakeAdapter((req) async {
    if (req.uri.path.endsWith('/me/learning-settings')) {
      if (req.method == 'PUT') {
        final custom = onPut?.call(req);
        if (custom != null) return custom;
        final body = asJsonMap(req.data is String ? jsonDecode(req.data as String) : req.data);
        state = LearningSettings.fromJson(body).copyWith(isDefault: false);
        saved?.add(state);
      }
      return (200, learningSettingsBody(state));
    }
    return okSystemInfo('chinese-backend').handler(req);
  });
}

/// Dựng app đầy đủ (router + theme + provider + phiên) với client giả — dùng cho widget test shell/trang.
///
/// Mặc định [signedIn] ⇒ kho có phiên (như mở lại app) ⇒ refresh + `/me` OK ⇒ vào trang chủ (tổng quan). [permissions]
/// để thử `/403`; [authHandler] để thử lỗi đăng nhập; [tokenStore] để assert kho sau đăng xuất.
///
/// Trang chủ M5 gọi `/progress/overview`: mặc định trả fixture người mới ([defaultOverviewBody]); [overviewBody] để
/// đổi số liệu (vd `overviewFixture('progress_overview_full.json')`); [overview] để trả theo từng lời gọi (lỗi rồi
/// thành công, đổi người). Cả hai null ⇒ giao cho [chineseAdapter] (test lỗi tải). Huy hiệu/trang Ôn tập (M6) gọi
/// `/srs/summary`: mặc định suy từ thân tổng quan ([summaryFromOverview]); [srs] để trả `/srs/*` tuỳ ý (hàng đợi,
/// chấm thẻ).
Widget buildTestApp({
  required FakeAdapter chineseAdapter,
  required FakeAdapter identityAdapter,
  KeyValueStore? store,
  bool signedIn = true,
  Set<String> permissions = const {'study.use'},
  Future<(int, String)> Function(RequestOptions req)? authHandler,
  InMemoryTokenStore? tokenStore,
  List<RequestOptions>? requestLog,
  String deviceTimeZone = 'Asia/Ho_Chi_Minh',
  AfTts? tts,
  String Function()? meId,
  String? overviewBody,
  Future<(int, String)> Function(RequestOptions req)? overview,
  bool useDefaultOverview = true,
  Future<(int, String)> Function(RequestOptions req)? srs,
}) {
  final tokens = tokenStore ?? InMemoryTokenStore();
  if (signedIn && tokens.session == null) tokens.session = testStoredSession();
  final prefs = store ?? InMemoryKeyValueStore({kInstallFlagKey: true});
  final overviewHandler =
      overview ??
      (overviewBody != null || useDefaultOverview
          ? (_) => Future.value((200, overviewBody ?? defaultOverviewBody()))
          : null);

  return ProviderScope(
    overrides: [
      appConfigProvider.overrideWithValue(testConfig),
      keyValueStoreProvider.overrideWithValue(prefs),
      tokenStoreProvider.overrideWithValue(tokens),
      chineseAdapterProvider.overrideWithValue(
        chineseStub(
          rest: chineseAdapter,
          permissions: permissions,
          log: requestLog,
          meId: meId,
          overview: overviewHandler,
          srs: srs,
        ),
      ),
      identityAdapterProvider.overrideWithValue(
        identityStub(rest: identityAdapter, auth: authHandler, log: requestLog),
      ),
      authDepsProvider.overrideWith(buildChineseAuthDeps),
      deviceTimeZoneProvider.overrideWith((_) async => deviceTimeZone),
      availableTimeZonesProvider.overrideWith((_) async => kFallbackTimeZones),
      if (tts != null) afTtsProvider.overrideWithValue(tts),
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

/// Router của app (để test điều hướng thẳng tới `/ho-so?tab=...` như deep link).
final routerProviderForTest = routerProvider;
