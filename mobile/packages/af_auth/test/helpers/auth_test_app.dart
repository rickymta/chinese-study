import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'auth_fixtures.dart';
import 'fake_adapter.dart';

/// App tối giản cho widget test trang đăng nhập/đăng ký: router có `authRedirect`, `AuthDeps` với kho trong bộ nhớ
/// và identity giả. Trang `/` hiện "TRANG CHỦ" + email để assert đã vào app.
class AuthTestApp extends StatelessWidget {
  AuthTestApp({
    super.key,
    required this.identity,
    this.stored,
    this.loadMe,
    this.initialLocation = '/dang-nhap',
    this.timeZone = 'Asia/Ho_Chi_Minh',
    this.dark = false,
  });

  final Future<FakeResponse> Function(RequestOptions req, int i) identity;
  final StoredSession? stored;
  final Future<MeInfo> Function()? loadMe;
  final String initialLocation;
  final String timeZone;
  final bool dark;

  final InMemoryTokenStore store = InMemoryTokenStore();
  late final FakeHttpClientAdapter adapter = FakeHttpClientAdapter(identity);

  @override
  Widget build(BuildContext context) {
    store.session = stored;
    // Key theo instance: pumpWidget một AuthTestApp khác trong cùng test phải dựng container/router/state mới
    // (ProviderScope cùng vị trí cây sẽ bị Flutter tái dùng State cũ).
    return ProviderScope(
      key: ObjectKey(this),
      overrides: [
        // Session dựng trong provider để `ref.onDispose` huỷ Timer hẹn làm mới khi cây widget bị gỡ (flutter_test
        // báo lỗi nếu còn Timer treo sau test).
        authDepsProvider.overrideWith((ref) {
          late AuthSession session;
          final dio = createApiClient(
            baseUrl: 'http://test.local/identity/api',
            headers: const {kClientHeaderName: 'chinese-mobile/0.1.0+1 (web)'},
            getAccessToken: () => session.accessToken,
            refresh: () => session.refresh(),
            onAuthLost: () => session.handleAuthLost(),
          )..httpClientAdapter = adapter;
          final client = IdentityClient(dio);
          session = AuthSession(refreshCall: client.refresh, store: store, wait: (_) async {});
          ref.onDispose(session.dispose);
          return AuthDeps(
            identity: client,
            session: session,
            loadMe: loadMe ?? () async => meWithStudy,
            deviceName: 'Web dev',
          );
        }),
        deviceTimeZoneProvider.overrideWith((_) async => timeZone),
      ],
      retry: afNoRetry,
      child: _RouterApp(initialLocation: initialLocation, dark: dark),
    );
  }
}

class _RouterApp extends ConsumerStatefulWidget {
  const _RouterApp({required this.initialLocation, required this.dark});

  final String initialLocation;
  final bool dark;

  @override
  ConsumerState<_RouterApp> createState() => _RouterAppState();
}

class _RouterAppState extends ConsumerState<_RouterApp> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    final notifier = ValueNotifier(0);
    ref.listenManual(authControllerProvider, (_, _) => notifier.value++);
    _router = GoRouter(
      initialLocation: widget.initialLocation,
      refreshListenable: notifier,
      redirect: (context, state) => authRedirect(ref.read(authControllerProvider), state.uri),
      routes: [
        GoRoute(
          path: '/dang-nhap',
          builder: (_, _) => const LoginPage(brand: 'AntFarm · Test'),
        ),
        GoRoute(
          path: '/dang-ky',
          builder: (_, _) => const RegisterPage(brand: 'AntFarm · Test'),
        ),
        GoRoute(
          path: '/',
          builder: (_, _) => const AuthGate(child: _HomeStub()),
        ),
        GoRoute(
          path: '/on-tap',
          builder: (_, _) => const AuthGate(child: Scaffold(body: Text('ÔN TẬP'))),
        ),
        GoRoute(
          path: '/403',
          builder: (_, _) => const Scaffold(body: Text('CẤM')),
        ),
      ],
    );
  }

  @override
  void dispose() {
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp.router(
    theme: buildAfTheme(brightness: widget.dark ? Brightness.dark : Brightness.light),
    routerConfig: _router,
  );
}

class _HomeStub extends ConsumerWidget {
  const _HomeStub();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final account = ref.watch(currentAccountProvider);
    return Scaffold(body: Text('TRANG CHỦ ${account?.email ?? ''}'));
  }
}
