import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  testWidgets('ẩn danh ⇒ trang đăng nhập; đăng nhập đúng ⇒ trang chủ chào tên; kho được ghi', (tester) async {
    final tokens = InMemoryTokenStore();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        signedIn: false,
        tokenStore: tokens,
      ),
    );
    await tester.pumpAndSettle();
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
    expect(find.text('AntFarm · Tiếng Trung'), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'ban@vidu.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'matkhau-dai');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    expect(find.text('Xin chào, Quân!'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(tokens.session?.refreshToken, 'rt-1');
    expect(tokens.log, contains('write:rt-1'));
  });

  testWidgets('sai mật khẩu ⇒ lỗi tại chỗ, vẫn ở trang đăng nhập', (tester) async {
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        signedIn: false,
        authHandler: (_) async => (401, jsonEncode({'error': 'Sai.', 'code': 'INVALID_CREDENTIALS'})),
      ),
    );
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'ban@vidu.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'sai-roi');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();
    expect(find.text('Email hoặc mật khẩu không đúng.'), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
  });

  testWidgets('có phiên trong kho (mở lại app) ⇒ tự vào trang chủ; "Thêm" hiện tài khoản + Đăng xuất ⇒ về đăng nhập', (
    tester,
  ) async {
    final tokens = InMemoryTokenStore();
    final identity = okSystemInfo('identity-service');
    await tester.pumpWidget(
      buildTestApp(chineseAdapter: okSystemInfo('chinese-backend'), identityAdapter: identity, tokenStore: tokens),
    );
    await tester.pumpAndSettle();
    expect(find.text('Xin chào, Quân!'), findsOneWidget);
    expect(tokens.session?.refreshToken, 'rt-1'); // đã xoay khi làm mới lúc mở app

    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    expect(find.text('ban@vidu.com'), findsOneWidget);
    expect(find.text('Đăng xuất'), findsOneWidget);
    // Không có quyền quản trị ⇒ không hiện dòng dùng bản web.
    expect(find.textContaining('Quản trị nội dung và người dùng dùng bản web'), findsNothing);

    await tester.ensureVisible(find.text('Đăng xuất'));
    await tester.tap(find.text('Đăng xuất'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
    expect(tokens.session, isNull);
    expect(find.textContaining('hết hạn'), findsNothing); // đăng xuất chủ động: không banner
  });

  testWidgets('quyền quản trị ⇒ "Thêm" hiện dòng giải thích dùng bản web (RM-S6)', (tester) async {
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        permissions: const {'study.use', 'content.manage'},
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    expect(find.textContaining('Quản trị nội dung và người dùng dùng bản web:'), findsOneWidget);
    expect(find.textContaining('chinese.antfarms.xyz'), findsOneWidget);
  });

  testWidgets('thiếu study.use ⇒ /403 có lời giải thích + Đăng xuất ⇒ về đăng nhập', (tester) async {
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        permissions: const {},
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Không có quyền truy cập'), findsWidgets);
    expect(find.textContaining('chưa được cấp quyền học'), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);

    await tester.tap(find.widgetWithText(OutlinedButton, 'Đăng xuất'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
  });

  testWidgets('refresh bị từ chối lúc mở app ⇒ đăng nhập với banner "Phiên đăng nhập đã hết hạn"', (tester) async {
    final tokens = InMemoryTokenStore();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        tokenStore: tokens,
        authHandler: (_) async => (401, jsonEncode({'error': 'Phiên không hợp lệ.', 'code': 'REFRESH_INVALID'})),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại để tiếp tục.'), findsOneWidget);
    expect(tokens.session, isNull);
  });

  testWidgets('identity không tới được lúc mở app ⇒ màn "Không kết nối được máy chủ", phiên còn trong kho', (
    tester,
  ) async {
    final tokens = InMemoryTokenStore();
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        tokenStore: tokens,
        authHandler: (_) async => throw Exception('socket'),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Không kết nối được máy chủ'), findsWidgets);
    expect(find.text('Thử lại'), findsOneWidget);
    expect(find.text('Đăng xuất'), findsOneWidget);
    expect(tokens.session?.refreshToken, 'rt-0');
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsNothing);
  });
  testWidgets('chế độ tối + 360×740 + chữ 1.3×: trang /403 (thiếu study.use) không overflow', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: okSystemInfo('chinese-backend'),
        identityAdapter: okSystemInfo('identity-service'),
        permissions: const {},
        store: InMemoryKeyValueStore({kInstallFlagKey: true, kThemeModeKey: 'dark'}),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('chưa được cấp quyền học'), findsOneWidget);
    expect(Theme.of(tester.element(find.text('Đăng xuất'))).brightness, Brightness.dark);
    expect(tester.takeException(), isNull);
  });
}
