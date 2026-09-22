import 'package:af_auth/af_auth.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';

void main() {
  const anonymous = AuthAnonymous();
  const expired = AuthAnonymous(reason: AuthLostReason.expired);
  const authed = AuthAuthenticated(account: testAccount, me: meWithStudy);
  const noStudy = AuthAuthenticated(account: testAccount, me: MeInfo.empty);

  String? go(AuthState s, String location) => authRedirect(s, Uri.parse(location));

  group('sanitizeReturnTo', () {
    test('chỉ nhận đường dẫn nội bộ', () {
      expect(sanitizeReturnTo('/on-tap?x=1'), '/on-tap?x=1');
      expect(sanitizeReturnTo('//evil.com'), isNull);
      expect(sanitizeReturnTo(r'/\evil.com'), isNull);
      expect(sanitizeReturnTo('http://evil.com'), isNull);
      expect(sanitizeReturnTo('on-tap'), isNull);
      expect(sanitizeReturnTo(''), isNull);
      expect(sanitizeReturnTo(null), isNull);
    });
  });

  group('buildLoginUrl', () {
    test('kèm returnTo (path + query) và reason; bỏ qua "/" và trang đăng nhập', () {
      expect(
        buildLoginUrl('/dang-nhap', Uri.parse('/on-tap?tab=luyen'), null),
        '/dang-nhap?returnTo=%2Fon-tap%3Ftab%3Dluyen',
      );
      expect(buildLoginUrl('/dang-nhap', Uri.parse('/'), null), '/dang-nhap');
      expect(buildLoginUrl('/dang-nhap', Uri.parse('/dang-nhap?x=1'), null), '/dang-nhap');
      expect(buildLoginUrl('/dang-nhap', Uri.parse('/'), AuthLostReason.expired), '/dang-nhap?reason=expired');
      expect(
        buildLoginUrl('/dang-nhap', Uri.parse('/bai-hoc'), AuthLostReason.passwordChanged),
        '/dang-nhap?returnTo=%2Fbai-hoc&reason=password-changed',
      );
    });
  });

  group('authRedirect', () {
    test('AuthLoading / AuthUnreachable ⇒ không điều hướng (trừ Unreachable ở trang đăng nhập)', () {
      expect(go(const AuthLoading(), '/on-tap'), isNull);
      expect(go(const AuthUnreachable(message: 'x'), '/on-tap'), isNull);
      expect(go(const AuthUnreachable(message: 'x'), '/403'), isNull);
      expect(go(const AuthLoading(), '/dang-nhap'), isNull);
    });

    test('AuthUnreachable ở /dang-nhap|/dang-ky (đăng nhập xong nhưng /me lỗi mạng) ⇒ về returnTo hoặc /', () {
      expect(go(const AuthUnreachable(message: 'x'), '/dang-nhap'), '/');
      expect(go(const AuthUnreachable(message: 'x'), '/dang-nhap?returnTo=%2Fon-tap'), '/on-tap');
      expect(go(const AuthUnreachable(message: 'x'), '/dang-ky?returnTo=//evil'), '/');
    });

    test('ẩn danh ⇒ /dang-nhap?returnTo=/on-tap; mất phiên ⇒ thêm reason=expired', () {
      expect(go(anonymous, '/on-tap'), '/dang-nhap?returnTo=%2Fon-tap');
      expect(go(expired, '/on-tap'), '/dang-nhap?returnTo=%2Fon-tap&reason=expired');
      expect(go(anonymous, '/'), '/dang-nhap');
    });

    test('ẩn danh ở trang đăng nhập/đăng ký/401/404 ⇒ ở lại', () {
      expect(go(anonymous, '/dang-nhap'), isNull);
      expect(go(anonymous, '/dang-ky?returnTo=%2Fon-tap'), isNull);
      expect(go(anonymous, '/401'), isNull);
      expect(go(anonymous, '/404'), isNull);
      expect(go(anonymous, '/403'), '/dang-nhap?returnTo=%2F403');
    });

    test('đã đăng nhập ở trang đăng nhập ⇒ returnTo hợp lệ hoặc /; returnTo=//evil ⇒ /', () {
      expect(go(authed, '/dang-nhap?returnTo=%2Fon-tap'), '/on-tap');
      expect(go(authed, '/dang-ky?returnTo=%2Fon-tap%3Ftab%3Dluyen'), '/on-tap?tab=luyen');
      expect(go(authed, '/dang-nhap'), '/');
      expect(go(authed, '/dang-nhap?returnTo=//evil.com'), '/');
      expect(go(authed, '/dang-nhap?returnTo=http://evil.com'), '/');
    });

    test('đã đăng nhập, có study.use ⇒ đi tiếp mọi trang', () {
      expect(go(authed, '/'), isNull);
      expect(go(authed, '/on-tap/phien'), isNull);
      expect(go(authed, '/403'), isNull);
    });

    test('thiếu study.use ⇒ /403 trừ /403, /them, /ho-so, /giay-phep', () {
      expect(go(noStudy, '/'), '/403');
      expect(go(noStudy, '/on-tap'), '/403');
      expect(go(noStudy, '/403'), isNull);
      expect(go(noStudy, '/them'), isNull);
      expect(go(noStudy, '/ho-so?tab=mat-khau'), isNull);
      expect(go(noStudy, '/giay-phep'), isNull);
      expect(go(noStudy, '/dang-nhap'), '/');
    });

    test('AuthLostReason.fromQuery', () {
      expect(AuthLostReason.fromQuery('expired'), AuthLostReason.expired);
      expect(AuthLostReason.fromQuery('password-changed'), AuthLostReason.passwordChanged);
      expect(AuthLostReason.fromQuery('la'), isNull);
      expect(AuthLostReason.fromQuery(null), isNull);
    });
  });
}
