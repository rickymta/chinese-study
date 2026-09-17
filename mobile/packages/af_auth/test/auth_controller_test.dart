import 'dart:async';

import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';
import 'helpers/fake_adapter.dart';

/// Chờ tới khi trạng thái thoả [test] (tối đa ~2 s vòng lặp microtask/timer ngắn).
Future<AuthState> waitFor(ProviderContainer c, bool Function(AuthState s) test) async {
  for (var i = 0; i < 200; i++) {
    final s = c.read(authControllerProvider);
    if (test(s)) return s;
    await Future<void>.delayed(const Duration(milliseconds: 5));
  }
  fail('Hết thời gian chờ; trạng thái hiện tại: ${c.read(authControllerProvider)}');
}

void main() {
  const base = 'http://localhost:5280/identity/api';

  ({ProviderContainer container, InMemoryTokenStore store, FakeHttpClientAdapter adapter, List<String?> signedOut})
  build({
    required Future<FakeResponse> Function(RequestOptions req, int i) identity,
    required Future<MeInfo> Function() loadMe,
    StoredSession? stored,
    KeyValueStore? prefs,
    Completer<void>? refreshGate,
  }) {
    final adapter = FakeHttpClientAdapter((req, i) async {
      if (refreshGate != null && req.uri.path.endsWith('/auth/mobile/refresh')) await refreshGate.future;
      return identity(req, i);
    });
    final store = InMemoryTokenStore(stored);
    final signedOut = <String?>[];
    late AuthSession session;
    final dio = createApiClient(
      baseUrl: base,
      getAccessToken: () => session.accessToken,
      refresh: () => session.refresh(),
      onAuthLost: () => session.handleAuthLost(),
    )..httpClientAdapter = adapter;
    final identityClient = IdentityClient(dio);
    session = AuthSession(refreshCall: identityClient.refresh, store: store, wait: (_) async {});
    final container = ProviderContainer(
      overrides: [
        authDepsProvider.overrideWithValue(
          AuthDeps(
            identity: identityClient,
            session: session,
            loadMe: loadMe,
            installGuard: prefs == null ? null : InstallGuard(prefs: prefs, tokenStore: store),
            deviceName: 'Web dev',
            onSignedOut: (id) async => signedOut.add(id),
          ),
        ),
      ],
    );
    addTearDown(() {
      container.dispose();
      session.dispose();
    });
    // Giữ provider sống trong suốt test.
    container.listen(authControllerProvider, (_, _) {});
    return (container: container, store: store, adapter: adapter, signedOut: signedOut);
  }

  Future<FakeResponse> refreshOk(RequestOptions req, int i) async {
    if (req.uri.path.endsWith('/auth/mobile/refresh')) {
      return FakeResponse.json(200, {
        'accessToken': fakeJwt(),
        'accessTokenExpiresAt': '2099-01-01T00:00:00Z',
        'refreshToken': 'rt-$i',
        'refreshTokenExpiresAt': '2099-02-01T00:00:00Z',
      });
    }
    if (req.uri.path.endsWith('/auth/mobile/logout')) return const FakeResponse(204, contentType: null);
    return FakeResponse.json(404, {'error': 'không có'});
  }

  test('không có phiên trong kho ⇒ AuthAnonymous (không reason), không gọi mạng', () async {
    final t = build(identity: refreshOk, loadMe: () async => meWithStudy);
    final s = await waitFor(t.container, (s) => s is! AuthLoading);
    expect(s, isA<AuthAnonymous>().having((a) => a.reason, 'reason', isNull));
    expect(t.adapter.requests, isEmpty);
  });

  test('có phiên ⇒ refresh ⇒ loadMe ⇒ AuthAuthenticated (tài khoản từ claim JWT)', () async {
    final t = build(identity: refreshOk, loadMe: () async => meWithStudy, stored: storedSession());
    final s = await waitFor(t.container, (s) => s is AuthAuthenticated);
    s as AuthAuthenticated;
    expect(s.account.displayName, 'Quân');
    expect(s.hasPermission('study.use'), isTrue);
    expect(t.store.session?.refreshToken, 'rt-0');
    expect(t.container.read(currentAccountProvider)?.id, 'u-1');
    expect(t.container.read(permissionsProvider), {'study.use'});
  });

  test('refresh 401 lúc khởi động ⇒ AuthAnonymous(expired), kho bị xoá, onSignedOut được gọi', () async {
    final t = build(
      identity: (_, _) async => FakeResponse.json(401, errorBody('REFRESH_INVALID', 'Phiên không hợp lệ.')),
      loadMe: () async => meWithStudy,
      stored: storedSession(),
    );
    final s = await waitFor(t.container, (s) => s is AuthAnonymous);
    expect((s as AuthAnonymous).reason, AuthLostReason.expired);
    expect(t.store.session, isNull);
    await Future<void>.delayed(Duration.zero);
    expect(t.signedOut, ['u-1']);
  });

  test('refresh lỗi mạng ⇒ AuthUnreachable (giữ phiên); bootstrap lại khi mạng về ⇒ authenticated', () async {
    var down = true;
    final t = build(
      identity: (req, i) async {
        if (down) throw StateError('mất mạng');
        return refreshOk(req, i);
      },
      loadMe: () async => meWithStudy,
      stored: storedSession(),
    );
    final s = await waitFor(t.container, (s) => s is AuthUnreachable);
    expect((s as AuthUnreachable).account?.email, 'ban@vidu.com');
    expect(s.message, contains('Không kết nối được máy chủ'));
    expect(t.store.session, isNotNull);

    down = false;
    unawaited(t.container.read(authControllerProvider.notifier).bootstrap());
    await waitFor(t.container, (s) => s is AuthAuthenticated);
  });

  test('loadMe 403 ⇒ vẫn AuthAuthenticated với 0 quyền (router đưa /403)', () async {
    final t = build(
      identity: refreshOk,
      loadMe: () async => throw ApiError('Không có quyền', status: 403),
      stored: storedSession(),
    );
    final s = await waitFor(t.container, (s) => s is AuthAuthenticated) as AuthAuthenticated;
    expect(s.permissions, isEmpty);
  });

  test('loadMe lỗi mạng ⇒ AuthUnreachable; reloadMe khi service về ⇒ authenticated', () async {
    var down = true;
    final t = build(
      identity: refreshOk,
      loadMe: () async {
        if (down) throw ApiError.network();
        return meWithStudy;
      },
      stored: storedSession(),
    );
    await waitFor(t.container, (s) => s is AuthUnreachable);
    down = false;
    await t.container.read(authControllerProvider.notifier).reloadMe();
    expect(t.container.read(authControllerProvider), isA<AuthAuthenticated>());
  });

  test(
    'refreshSession: xoay token + GET /account + loadMe ⇒ true; loadMe lỗi mạng ⇒ false + AuthUnreachable',
    () async {
      var meDown = false;
      final t = build(
        identity: (req, i) async {
          if (req.uri.path.endsWith('/account')) {
            return FakeResponse.json(200, {
              'id': 'u-1',
              'email': 'ban@vidu.com',
              'displayName': 'Tên mới',
              'timeZone': 'Asia/Tokyo',
              'createdAt': '2026-09-01T00:00:00Z',
            });
          }
          return refreshOk(req, i);
        },
        loadMe: () async {
          if (meDown) throw ApiError.network();
          return meWithStudy;
        },
        stored: storedSession(),
      );
      await waitFor(t.container, (s) => s is AuthAuthenticated);
      final refreshesBefore = t.adapter.requests.where((r) => r.uri.path.endsWith('/refresh')).length;

      expect(await t.container.read(authControllerProvider.notifier).refreshSession(), isTrue);
      expect(t.adapter.requests.where((r) => r.uri.path.endsWith('/refresh')).length, refreshesBefore + 1);
      expect(t.container.read(authControllerProvider).account?.displayName, 'Tên mới');
      expect(t.container.read(authControllerProvider).account?.timeZone, 'Asia/Tokyo');

      meDown = true;
      expect(await t.container.read(authControllerProvider.notifier).refreshSession(), isFalse);
      expect(t.container.read(authControllerProvider), isA<AuthUnreachable>());
      expect(t.store.session, isNotNull); // giữ phiên (RM-S3)
    },
  );

  test('login: ghi kho, gửi deviceName, loadMe ⇒ authenticated; sai mật khẩu ⇒ ném ApiError, vẫn ẩn danh', () async {
    final t = build(
      identity: (req, i) async {
        if (req.uri.path.endsWith('/auth/mobile/login')) {
          if (bodyOf(req)['password'] == 'sai') {
            return FakeResponse.json(401, errorBody('INVALID_CREDENTIALS', 'Sai.'));
          }
          return FakeResponse.json(200, loadFixture('mobile_auth_response.json'));
        }
        return refreshOk(req, i);
      },
      loadMe: () async => meWithStudy,
    );
    await waitFor(t.container, (s) => s is AuthAnonymous);
    final ctrl = t.container.read(authControllerProvider.notifier);

    await expectLater(ctrl.login(email: 'ban@vidu.com', password: 'sai'), throwsA(isA<ApiError>()));
    expect(t.container.read(authControllerProvider), isA<AuthAnonymous>());
    expect(t.store.session, isNull);

    await ctrl.login(email: 'ban@vidu.com', password: 'dung');
    expect(t.container.read(authControllerProvider), isA<AuthAuthenticated>());
    expect(t.store.log, contains(startsWith('write:9f2c')));
    expect(bodyOf(t.adapter.requests.last)['deviceName'], 'Web dev');
  });

  test('register ⇒ authenticated với tài khoản trả về', () async {
    final t = build(
      identity: (req, i) async => FakeResponse.json(201, loadFixture('mobile_auth_response.json')),
      loadMe: () async => meWithStudy,
    );
    await waitFor(t.container, (s) => s is AuthAnonymous);
    await t.container
        .read(authControllerProvider.notifier)
        .register(email: 'ban@vidu.com', password: 'matkhau-dai', displayName: 'Quân', timeZone: 'Asia/Ho_Chi_Minh');
    final s = t.container.read(authControllerProvider) as AuthAuthenticated;
    expect(s.account.id, '0192');
    expect(t.adapter.requests.single.uri.path, endsWith('/auth/mobile/register'));
  });

  test('logout: gọi /mobile/logout với refresh token, xoá kho, hook onSignedOut, AuthAnonymous không reason', () async {
    final t = build(identity: refreshOk, loadMe: () async => meWithStudy, stored: storedSession());
    await waitFor(t.container, (s) => s is AuthAuthenticated);
    await t.container.read(authControllerProvider.notifier).logout();
    final logoutReq = t.adapter.requests.last;
    expect(logoutReq.uri.path, endsWith('/auth/mobile/logout'));
    expect(bodyOf(logoutReq)['refreshToken'], 'rt-0');
    expect(t.store.session, isNull);
    expect(t.signedOut, ['u-1']);
    expect(t.container.read(authControllerProvider), isA<AuthAnonymous>().having((a) => a.reason, 'reason', isNull));
  });

  test('logout khi /mobile/logout lỗi mạng ⇒ vẫn đăng xuất cục bộ', () async {
    var loggedIn = false;
    final t = build(
      identity: (req, i) async {
        if (req.uri.path.endsWith('/auth/mobile/logout')) throw StateError('mạng');
        loggedIn = true;
        return refreshOk(req, i);
      },
      loadMe: () async => meWithStudy,
      stored: storedSession(),
    );
    await waitFor(t.container, (s) => s is AuthAuthenticated);
    expect(loggedIn, isTrue);
    await t.container.read(authControllerProvider.notifier).logout();
    expect(t.container.read(authControllerProvider), isA<AuthAnonymous>());
    expect(t.store.session, isNull);
  });

  test('request 401 ⇒ refresh ⇒ gửi lại; refresh bị từ chối ⇒ sự kiện lost ⇒ AuthAnonymous(expired)', () async {
    var refreshCount = 0;
    final t = build(
      identity: (req, i) async {
        if (req.uri.path.endsWith('/auth/mobile/refresh')) {
          refreshCount++;
          if (refreshCount == 1) return refreshOk(req, i);
          return FakeResponse.json(401, errorBody('REFRESH_INVALID', 'Phiên không hợp lệ.'));
        }
        return refreshOk(req, i);
      },
      loadMe: () async => meWithStudy,
      stored: storedSession(),
    );
    await waitFor(t.container, (s) => s is AuthAuthenticated);

    // Một Dio khác (service ngôn ngữ) nhận 401 ⇒ refresh (lần 2 bị từ chối) ⇒ onAuthLost.
    final session = t.container.read(authDepsProvider).session;
    final chinese = createApiClient(
      baseUrl: 'http://localhost:5280/chinese/api',
      getAccessToken: () => session.accessToken,
      refresh: session.refresh,
      onAuthLost: session.handleAuthLost,
    )..httpClientAdapter = FakeHttpClientAdapter((_, _) async => FakeResponse.json(401, {'error': 'Cần đăng nhập'}));
    await expectLater(chinese.get<Object?>('/me'), throwsA(isA<ApiError>()));
    final s = await waitFor(t.container, (s) => s is AuthAnonymous) as AuthAnonymous;
    expect(s.reason, AuthLostReason.expired);
    expect(t.store.session, isNull);
  });

  test('InstallGuard lần đầu: kho có phiên cũ (Keychain sau cài lại) ⇒ bị xoá ⇒ ẩn danh', () async {
    final t = build(
      identity: refreshOk,
      loadMe: () async => meWithStudy,
      stored: storedSession(),
      prefs: InMemoryKeyValueStore(),
    );
    final s = await waitFor(t.container, (s) => s is! AuthLoading);
    expect(s, isA<AuthAnonymous>());
    expect(t.adapter.requests, isEmpty);
  });

  test('signOutLocally(passwordChanged) ⇒ AuthAnonymous(password-changed), không gọi server', () async {
    final t = build(identity: refreshOk, loadMe: () async => meWithStudy, stored: storedSession());
    await waitFor(t.container, (s) => s is AuthAuthenticated);
    final before = t.adapter.requests.length;
    await t.container.read(authControllerProvider.notifier).signOutLocally(AuthLostReason.passwordChanged);
    expect(t.adapter.requests.length, before);
    expect(
      t.container.read(authControllerProvider),
      isA<AuthAnonymous>().having((a) => a.reason, 'reason', AuthLostReason.passwordChanged),
    );
  });
  test('loadMe 401 mà refresh chỉ lỗi mạng (interceptor trả 401 gốc) ⇒ AuthUnreachable, kho CÒN', () async {
    final t = build(
      identity: refreshOk,
      loadMe: () async => throw ApiError('Cần đăng nhập để tiếp tục.', status: 401),
      stored: storedSession(),
    );
    final s = await waitFor(t.container, (s) => s is AuthUnreachable);
    expect((s as AuthUnreachable).account?.id, 'u-1');
    expect(t.store.session, isNotNull);
    expect(t.signedOut, isEmpty);
  });

  test('logout trong lúc refresh treo ⇒ chờ xong, gửi refresh token MỚI NHẤT, kho rỗng, không "sống lại"', () async {
    final t = build(identity: refreshOk, loadMe: () async => meWithStudy, stored: storedSession());
    await waitFor(t.container, (s) => s is AuthAuthenticated);
    final session = t.container.read(authDepsProvider).session;
    // Giả lập refresh treo: chặn adapter bằng completer riêng cho lần refresh kế tiếp.
    final gate = Completer<void>();
    final t2 = build(identity: refreshOk, loadMe: () async => meWithStudy, stored: storedSession(), refreshGate: gate);
    await Future<void>.delayed(const Duration(milliseconds: 20)); // bootstrap của t2 đang treo ở refresh
    expect(t2.container.read(authControllerProvider), isA<AuthLoading>());
    final logoutFuture = t2.container.read(authControllerProvider.notifier).logout();
    await Future<void>.delayed(const Duration(milliseconds: 20));
    gate.complete(); // refresh về (đã xoay sang rt-0 mới của adapter)
    await logoutFuture;
    final logoutReq = t2.adapter.requests.lastWhere((r) => r.uri.path.endsWith('/auth/mobile/logout'));
    final refreshRes = t2.adapter.requests.where((r) => r.uri.path.endsWith('/auth/mobile/refresh'));
    expect(refreshRes, hasLength(1));
    expect(bodyOf(logoutReq)['refreshToken'], 'rt-0'); // token đã xoay do refreshOk trả 'rt-<index>' = rt-0
    expect(t2.store.session, isNull);
    expect(t2.container.read(authControllerProvider), isA<AuthAnonymous>());
    // Không có lần ghi kho nào SAU khi clear (đăng xuất không "sống lại").
    expect(t2.store.log.last, 'clear');
    await Future<void>.delayed(const Duration(milliseconds: 20));
    expect(t2.store.session, isNull);
    expect(session.hasStoredSession, isTrue); // container 1 không liên quan
  });
  test('logout trong lúc loadMe của login treo ⇒ kết quả muộn không đặt lại AuthAuthenticated', () async {
    final meGate = Completer<MeInfo>();
    var calls = 0;
    final t = build(
      identity: (req, i) async => req.uri.path.endsWith('/auth/mobile/login')
          ? FakeResponse.json(200, loadFixture('mobile_auth_response.json'))
          : refreshOk(req, i),
      loadMe: () {
        calls++;
        return meGate.future;
      },
    );
    await waitFor(t.container, (s) => s is AuthAnonymous);
    final ctrl = t.container.read(authControllerProvider.notifier);
    final loginFuture = ctrl.login(email: 'ban@vidu.com', password: 'dung');
    await Future<void>.delayed(const Duration(milliseconds: 20));
    expect(calls, 1);
    await ctrl.logout(); // trong lúc loadMe treo
    expect(t.container.read(authControllerProvider), isA<AuthAnonymous>());
    meGate.complete(meWithStudy);
    await loginFuture;
    await Future<void>.delayed(const Duration(milliseconds: 20));
    expect(t.container.read(authControllerProvider), isA<AuthAnonymous>());
    expect(t.store.session, isNull);
  });
}
