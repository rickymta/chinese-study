import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';
import 'helpers/fake_adapter.dart';

void main() {
  const base = 'http://localhost:5280/identity/api';

  IdentityClient client(FakeHttpClientAdapter adapter, {String? Function()? getAccessToken}) {
    final dio = createApiClient(
      baseUrl: base,
      headers: {kClientHeaderName: 'chinese-mobile/0.1.0+1 (web)'},
      getAccessToken: getAccessToken,
    );
    dio.httpClientAdapter = adapter;
    return IdentityClient(dio);
  }

  test('register: POST /auth/mobile/register với đủ trường + X-AF-Client, parse 201', () async {
    final adapter = FakeHttpClientAdapter(
      (_, _) async => FakeResponse.json(201, loadFixture('mobile_auth_response.json')),
    );
    final res = await client(adapter).register(
      const RegisterRequest(
        email: 'ban@vidu.com',
        password: 'matkhau-dai',
        displayName: 'Quân',
        timeZone: 'Asia/Ho_Chi_Minh',
        deviceName: 'Web dev',
      ),
    );
    final req = adapter.requests.single;
    expect(req.uri.toString(), '$base/auth/mobile/register');
    expect(req.method, 'POST');
    expect(req.headers[kClientHeaderName], 'chinese-mobile/0.1.0+1 (web)');
    expect(bodyOf(req), {
      'email': 'ban@vidu.com',
      'password': 'matkhau-dai',
      'displayName': 'Quân',
      'timeZone': 'Asia/Ho_Chi_Minh',
      'deviceName': 'Web dev',
    });
    expect(res.account.id, '0192');
    expect(res.refreshToken, hasLength(64));
  });

  test('login: 401 INVALID_CREDENTIALS ⇒ ApiError có code, KHÔNG gọi refresh', () async {
    final adapter = FakeHttpClientAdapter(
      (_, _) async => FakeResponse.json(401, errorBody('INVALID_CREDENTIALS', 'Email hoặc mật khẩu không đúng.')),
    );
    await expectLater(
      client(adapter).login(const LoginRequest(email: 'a@b.vn', password: 'x')),
      throwsA(isA<ApiError>().having((e) => e.code, 'code', 'INVALID_CREDENTIALS').having((e) => e.status, 's', 401)),
    );
    expect(adapter.requests, hasLength(1));
  });

  test('refresh: gửi refreshToken, không Bearer, parse token mới; body HTML ⇒ ApiError (JSON guard)', () async {
    final adapter = FakeHttpClientAdapter(
      (_, _) async => FakeResponse.json(200, loadFixture('mobile_refresh_response.json')),
    );
    final res = await client(adapter, getAccessToken: () => 'old').refresh('rt-cu');
    final req = adapter.requests.single;
    expect(req.uri.path, endsWith('/auth/mobile/refresh'));
    expect(bodyOf(req), {'refreshToken': 'rt-cu'});
    expect(req.headers.containsKey('Authorization'), isFalse);
    expect(res.refreshToken, startsWith('41ab'));

    final html = FakeHttpClientAdapter(
      (_, _) async => const FakeResponse(200, body: '<html>', contentType: 'text/html'),
    );
    await expectLater(client(html).refresh('rt'), throwsA(isA<ApiError>()));
  });

  test('refresh 401 REFRESH_INVALID ⇒ ném ApiError 401 (không lặp làm mới)', () async {
    final adapter = FakeHttpClientAdapter(
      (_, _) async => FakeResponse.json(401, errorBody('REFRESH_INVALID', 'Phiên không hợp lệ.')),
    );
    await expectLater(client(adapter).refresh('rt'), throwsA(isA<ApiError>().having((e) => e.status, 's', 401)));
    expect(adapter.requests, hasLength(1));
  });

  test('logout: POST /auth/mobile/logout 204 không ném', () async {
    final adapter = FakeHttpClientAdapter((_, _) async => const FakeResponse(204, contentType: null));
    await client(adapter).logout('rt');
    expect(bodyOf(adapter.requests.single), {'refreshToken': 'rt'});
  });

  test('changePassword: Bearer + refreshToken tuỳ chọn; parse kết quả', () async {
    final adapter = FakeHttpClientAdapter(
      (_, _) async => FakeResponse.json(200, loadFixture('change_password_result.json')),
    );
    final r = await client(
      adapter,
      getAccessToken: () => 'tok',
    ).changePassword(currentPassword: 'cu', newPassword: 'moi', refreshToken: 'rt');
    expect(r.otherSessionsRevoked, 2);
    expect(r.currentSessionKept, isTrue);
    final req = adapter.requests.single;
    expect(req.headers['Authorization'], 'Bearer tok');
    expect(bodyOf(req), {'currentPassword': 'cu', 'newPassword': 'moi', 'refreshToken': 'rt'});
  });

  test('getAccount / updateAccount parse Account; thân lạ ⇒ ApiError', () async {
    final adapter = FakeHttpClientAdapter(
      (req, _) async => FakeResponse.json(200, {'id': '1', 'email': 'a@b.vn', 'displayName': 'A', 'timeZone': 'UTC'}),
    );
    final c = client(adapter, getAccessToken: () => 'tok');
    expect((await c.getAccount()).email, 'a@b.vn');
    expect((await c.updateAccount(displayName: 'B', timeZone: 'UTC')).displayName, 'A');
    expect(adapter.requests[1].method, 'PUT');

    final bad = FakeHttpClientAdapter((_, _) async => FakeResponse.json(200, {'la': true}));
    await expectLater(client(bad).getAccount(), throwsA(isA<ApiError>()));
  });
}
