import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';
import 'helpers/auth_test_app.dart';
import 'helpers/fake_adapter.dart';

void main() {
  Future<void> fill(
    WidgetTester tester, {
    String name = 'Quân',
    String email = 'ban@vidu.com',
    String password = 'matkhau-dai',
    String? confirm,
  }) async {
    await tester.enterText(find.widgetWithText(TextFormField, 'Tên hiển thị'), name);
    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), email);
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu (8–128 ký tự)'), password);
    await tester.enterText(find.widgetWithText(TextFormField, 'Nhập lại mật khẩu'), confirm ?? password);
  }

  testWidgets('hiện dòng múi giờ theo máy; đăng ký gửi timeZone + deviceName rồi vào trang chủ', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(201, loadFixture('mobile_auth_response.json')),
      initialLocation: '/dang-ky',
      timeZone: 'Asia/Bangkok',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    expect(find.textContaining('Múi giờ: Asia/Bangkok (theo máy)'), findsOneWidget);

    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.text('TRANG CHỦ ban@vidu.com'), findsOneWidget);

    final body = bodyOf(app.adapter.requests.single);
    expect(body['timeZone'], 'Asia/Bangkok');
    expect(body['deviceName'], 'Web dev');
    expect(body['displayName'], 'Quân');
    expect(body.containsKey('confirmPassword'), isFalse);
  });

  testWidgets('409 EMAIL_TAKEN ⇒ lỗi dưới ô email + banner', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(409, errorBody('EMAIL_TAKEN', 'Trùng.')),
      initialLocation: '/dang-ky',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.text('Email này đã được đăng ký.'), findsOneWidget);
    expect(find.text('Email này đã được đăng ký. Bạn có thể đăng nhập hoặc dùng email khác.'), findsOneWidget);
  });

  testWidgets('403 REGISTRATION_CLOSED ⇒ banner; mật khẩu nhập lại không khớp ⇒ không gọi mạng', (tester) async {
    final app = AuthTestApp(
      identity: (_, _) async => FakeResponse.json(403, errorBody('REGISTRATION_CLOSED', 'Đóng.')),
      initialLocation: '/dang-ky',
    );
    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
    await fill(tester, confirm: 'khac-nhau');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.text('Mật khẩu nhập lại không khớp'), findsOneWidget);
    expect(app.adapter.requests, isEmpty);

    await fill(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.text('Hệ thống hiện chưa mở đăng ký tài khoản mới.'), findsOneWidget);
  });

  testWidgets('liên kết "Đăng nhập" giữ query returnTo', (tester) async {
    await tester.pumpWidget(
      AuthTestApp(identity: (_, _) async => FakeResponse.json(200, {}), initialLocation: '/dang-ky?returnTo=%2Fon-tap'),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(TextButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
    await tester.tap(find.widgetWithText(TextButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(FilledButton, 'Đăng ký'), findsOneWidget);
  });
  testWidgets('360×740, chữ 1.3×, chế độ tối: trang đăng ký không overflow, nút ≥ 48', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
    await tester.pumpWidget(
      AuthTestApp(
        identity: (_, _) async => FakeResponse.json(409, errorBody('EMAIL_TAKEN', 'Trùng.')),
        initialLocation: '/dang-ky',
        dark: true,
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await fill(tester);
    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();
    expect(find.text('Email này đã được đăng ký.'), findsOneWidget);
    expect(tester.takeException(), isNull);
    expect(tester.getSize(find.widgetWithText(FilledButton, 'Đăng ký')).height, greaterThanOrEqualTo(48));
  });
}
