import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';
import 'helpers/auth_test_app.dart';
import 'helpers/fake_adapter.dart';

void main() {
  Future<void> fill(WidgetTester tester, {String email = 'ban@vidu.com', String password = 'matkhau-dai'}) async {
    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), email);
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), password);
  }

  testWidgets('ẩn danh ⇒ hiện form; sai mật khẩu (401) ⇒ "Email hoặc mật khẩu không đúng." tại chỗ', (tester) async {
    final app = AuthTestApp(identity: (_, _) async => FakeResponse.json(401, errorBody('INVALID_CREDENTIALS', 'Sai.')));
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    expect(find.text('Đăng nhập'), findsWidgets);
    expect(find.text('AntFarm · Test'), findsOneWidget);

    await fill(tester, password: 'sai-roi');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Email hoặc mật khẩu không đúng.'), findsOneWidget);
    expect(app.store.session, isNull);
    expect(app.adapter.requests.single.uri.path, endsWith('/auth/mobile/login'));
  });

  testWidgets('423 ACCOUNT_LOCKED ⇒ hiện giờ mở khoá theo giờ địa phương', (tester) async {
    final until = DateTime.now().toUtc().add(const Duration(minutes: 15));
    final local = until.toLocal();
    final hhmm = '${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(
        423,
        errorBody('ACCOUNT_LOCKED', 'Khoá.', details: {'lockedUntil': until.toIso8601String()}),
      ),
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau $hhmm.'), findsOneWidget);
  });

  testWidgets('kiểm tra tại chỗ: email sai dạng / mật khẩu trống ⇒ không gọi mạng', (tester) async {
    final app = AuthTestApp(identity: (_, _) async => FakeResponse.json(200, {}));
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester, email: 'khong-hop-le', password: '');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Email không hợp lệ'), findsOneWidget);
    expect(find.text('Vui lòng nhập mật khẩu'), findsOneWidget);
    expect(app.adapter.requests, isEmpty);
  });

  testWidgets('đăng nhập thành công ⇒ về returnTo (/on-tap); kho được ghi trước', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(200, loadFixture('mobile_auth_response.json')),
      initialLocation: '/dang-nhap?returnTo=%2Fon-tap',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('ÔN TẬP'), findsOneWidget);
    expect(app.store.session?.refreshToken, startsWith('9f2c'));
  });

  testWidgets('reason=expired ⇒ banner phiên hết hạn; password-changed ⇒ banner đổi mật khẩu', (tester) async {
    await tester.pumpWidget(
      AuthTestApp(identity: (_, _) async => FakeResponse.json(200, {}), initialLocation: '/dang-nhap?reason=expired'),
    );
    await tester.pumpAndSettle();
    expect(find.text('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại để tiếp tục.'), findsOneWidget);

    await tester.pumpWidget(
      AuthTestApp(
        identity: (_, _) async => FakeResponse.json(200, {}),
        initialLocation: '/dang-nhap?reason=password-changed',
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Bạn vừa đổi mật khẩu. Vui lòng đăng nhập lại bằng mật khẩu mới.'), findsOneWidget);
  });

  testWidgets('có phiên trong kho ⇒ tự đăng nhập, ẩn danh vào /on-tap ⇒ bị đưa về /dang-nhap?returnTo', (tester) async {
    final app = AuthTestApp(
      identity: (req, i) async => FakeResponse.json(200, {
        'accessToken': fakeJwt(),
        'accessTokenExpiresAt': '2099-01-01T00:00:00Z',
        'refreshToken': 'rt-moi',
        'refreshTokenExpiresAt': '2099-02-01T00:00:00Z',
      }),
      stored: storedSession(),
      initialLocation: '/on-tap',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    expect(find.text('ÔN TẬP'), findsOneWidget);
    expect(app.store.session?.refreshToken, 'rt-moi');

    final anon = AuthTestApp(identity: (_, _) async => FakeResponse.json(200, {}), initialLocation: '/on-tap');
    await tester.pumpWidget(anon);
    await tester.pumpAndSettle();
    expect(find.text('ÔN TẬP'), findsNothing);
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
  });

  testWidgets('thiếu study.use ⇒ /403', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(200, loadFixture('mobile_auth_response.json')),
      loadMe: () async => MeInfo.empty,
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('CẤM'), findsOneWidget);
  });

  testWidgets('360×740, chữ 1.3× không overflow', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
    await tester.pumpWidget(
      AuthTestApp(identity: (_, _) async => FakeResponse.json(200, {}), initialLocation: '/dang-nhap?reason=expired'),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    final button = tester.getSize(find.widgetWithText(FilledButton, 'Đăng nhập'));
    expect(button.height, greaterThanOrEqualTo(48));
  });
  testWidgets('đăng nhập OK nhưng /me lỗi mạng ⇒ rời form, hiện màn "Không kết nối được máy chủ"', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(200, loadFixture('mobile_auth_response.json')),
      loadMe: () async => throw ApiError.network(),
      initialLocation: '/dang-nhap?returnTo=%2Fon-tap',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Không kết nối được máy chủ'), findsWidgets);
    expect(find.text('Thử lại'), findsOneWidget);
    expect(find.text('Đăng xuất'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsNothing);
    // Không bấm lại được ⇒ chỉ MỘT lần login ⇒ không tạo họ token mồ côi.
    expect(app.adapter.requests.where((r) => r.uri.path.endsWith('/auth/mobile/login')), hasLength(1));
    expect(app.store.session, isNotNull);
  });

  testWidgets('chế độ tối: đăng nhập (banner + lỗi) không overflow', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    await tester.pumpWidget(
      AuthTestApp(
        identity: (_, _) async => FakeResponse.json(401, errorBody('INVALID_CREDENTIALS', 'Sai.')),
        initialLocation: '/dang-nhap?reason=expired',
        dark: true,
      ),
    );
    await tester.pumpAndSettle();
    expect(Theme.of(tester.element(find.text('Đăng nhập').first)).brightness, Brightness.dark);
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Email hoặc mật khẩu không đúng.'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
