import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';

void main() {
  test('MobileAuthResponse.fromJson đọc đủ trường từ fixture register/login', () {
    final res = MobileAuthResponse.fromJson(loadFixture('mobile_auth_response.json'));
    expect(res.accessToken, startsWith('eyJ'));
    expect(res.accessTokenExpiresAt, DateTime.utc(2026, 9, 17, 8, 15));
    expect(res.refreshToken, hasLength(64));
    expect(res.refreshTokenExpiresAt, DateTime.utc(2026, 10, 17, 8));
    expect(res.account.id, '0192');
    expect(res.account.email, 'ban@vidu.com');
    expect(res.account.displayName, 'Quân');
    expect(res.account.timeZone, 'Asia/Ho_Chi_Minh');
    expect(res.account.createdAt, DateTime.utc(2026, 9, 17, 8));
  });

  test('MobileRefreshResponse.fromJson từ fixture; thiếu hạn ⇒ giả định 15 phút / 30 ngày', () {
    final res = MobileRefreshResponse.fromJson(loadFixture('mobile_refresh_response.json'));
    expect(res.accessTokenExpiresAt, DateTime.utc(2026, 9, 17, 8, 30));

    final now = DateTime.utc(2026, 9, 17, 8);
    final noDates = MobileRefreshResponse.fromJson({'accessToken': 'a', 'refreshToken': 'r'}, now: now);
    expect(noDates.accessTokenExpiresAt, now.add(const Duration(minutes: 15)));
    expect(noDates.refreshTokenExpiresAt, now.add(const Duration(days: 30)));
  });

  test('phản hồi token thiếu accessToken/refreshToken/account ⇒ ném ApiError (không ghi token rác)', () {
    expect(() => MobileRefreshResponse.fromJson({'accessToken': 'a'}), throwsA(isA<ApiError>()));
    expect(() => MobileRefreshResponse.fromJson(null), throwsA(isA<ApiError>()));
    expect(() => MobileAuthResponse.fromJson({'accessToken': 'a', 'refreshToken': 'r'}), throwsA(isA<ApiError>()));
  });

  test('MeInfo.fromJson từ fixture; thiếu permissions ⇒ rỗng (fail-closed), không ném', () {
    final me = MeInfo.fromJson(loadFixture('me.json'));
    expect(me.id, '0192');
    expect(me.roles, ['learner']);
    expect(me.permissions, {'study.use'});
    expect(me.has('study.use'), isTrue);
    expect(me.firstSeenAt, DateTime.utc(2026, 9, 17, 8, 0, 5));

    final empty = MeInfo.fromJson({'id': 'x', 'permissions': 'không phải mảng'});
    expect(empty.permissions, isEmpty);
    expect(empty.timeZone, kDefaultTimeZone);
    expect(MeInfo.fromJson(null).permissions, isEmpty);
  });

  test('ChangePasswordResult.fromJson; thiếu currentSessionKept ⇒ false (an toàn)', () {
    final r = ChangePasswordResult.fromJson(loadFixture('change_password_result.json'));
    expect(r.otherSessionsRevoked, 2);
    expect(r.currentSessionKept, isTrue);
    expect(ChangePasswordResult.fromJson({}).currentSessionKept, isFalse);
  });

  test('StoredSession toJson/fromJson vòng tròn; sai phiên bản/thiếu token ⇒ null', () {
    final s = storedSession();
    final back = StoredSession.fromJson(s.toJson());
    expect(back, isNotNull);
    expect(back!.refreshToken, 'rt-0');
    expect(back.account, testAccount);
    expect(s.toJson()['v'], 1);
    expect(s.toJson().containsKey('accessToken'), isFalse);

    expect(StoredSession.fromJson({...s.toJson(), 'v': 2}), isNull);
    expect(StoredSession.fromJson({...s.toJson(), 'refreshToken': ''}), isNull);
    expect(StoredSession.fromJson({...s.toJson(), 'account': null}), isNull);
  });

  test('Account.fromJson thiếu id ⇒ null; timeZone rỗng ⇒ mặc định', () {
    expect(Account.fromJson({'email': 'a@b.vn'}), isNull);
    expect(Account.fromJson({'id': '1', 'timeZone': ''})!.timeZone, kDefaultTimeZone);
  });

  test('decodeJwtPayload / accountFromToken: đọc claim, token hỏng ⇒ null', () {
    final token = fakeJwt(sub: 'abc', name: 'Lan', zoneinfo: 'Asia/Bangkok');
    expect(decodeJwtPayload(token)?['sub'], 'abc');
    final acc = accountFromToken(token)!;
    expect(acc.id, 'abc');
    expect(acc.displayName, 'Lan');
    expect(acc.timeZone, 'Asia/Bangkok');
    expect(acc.email, 'ban@vidu.com');

    expect(accountFromToken('không.phải.jwt'), isNull);
    expect(accountFromToken(''), isNull);
    expect(accountFromToken(fakeJwt(sub: '')), isNull);
  });
}
