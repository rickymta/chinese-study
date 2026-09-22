import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

/// Từ M5 thẻ "Trạng thái hệ thống" nằm trong khối thu gọn cuối trang chủ, chỉ hiện với `users.manage`
/// (`dashboard_page_test` kiểm điều kiện hiện/ẩn); ở đây kiểm hành vi của thẻ sau khi mở khối.
void main() {
  Future<void> pumpAndExpand(WidgetTester tester, {required FakeAdapter chinese, required FakeAdapter identity}) async {
    await tester.pumpWidget(
      buildTestApp(
        chineseAdapter: chinese,
        identityAdapter: identity,
        permissions: const {'study.use', 'users.manage'},
      ),
    );
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.byKey(const ValueKey('system-status-section')),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Trạng thái hệ thống (qua gateway)'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Trạng thái hệ thống'), 200, scrollable: find.byType(Scrollable).first);
    await tester.pumpAndSettle();
  }

  testWidgets('cả hai service chạy ⇒ hai chip "đang chạy", không có khối cảnh báo', (tester) async {
    await pumpAndExpand(tester, chinese: okSystemInfo('chinese-backend'), identity: okSystemInfo('identity-service'));
    expect(find.text('Tiếng Trung: đang chạy'), findsOneWidget);
    expect(find.text('Tài khoản: đang chạy'), findsOneWidget);
    expect(find.textContaining('Không tới được'), findsNothing);
  });

  testWidgets('chinese-backend tắt ⇒ chip lỗi + cảnh báo, app không trắng; bấm chip thử lại', (tester) async {
    var calls = 0;
    final chinese = FakeAdapter((_) async {
      calls++;
      if (calls == 1) return (502, '');
      return (200, '{"service":"chinese-backend","version":"0.1.0","environment":"Development"}');
    });
    await pumpAndExpand(tester, chinese: chinese, identity: okSystemInfo('identity-service'));
    expect(find.text('Tiếng Trung: lỗi'), findsOneWidget);
    expect(find.text('Tài khoản: đang chạy'), findsOneWidget);
    expect(find.text('Không tới được: chinese-backend'), findsOneWidget);
    expect(find.byType(Scaffold), findsWidgets);
    expect(tester.takeException(), isNull);

    await tester.tap(find.text('Tiếng Trung: lỗi'));
    await tester.pumpAndSettle();
    expect(find.text('Tiếng Trung: đang chạy'), findsOneWidget);
    expect(find.textContaining('Không tới được'), findsNothing);
    expect(calls, 2);
  });

  testWidgets('mất mạng hoàn toàn ⇒ cả hai chip lỗi, liệt kê cả hai service', (tester) async {
    final down = FakeAdapter((_) async => throw Exception('socket'));
    // Tổng quan vẫn trả được (stub mặc định) — chỉ /system/info lỗi.
    await pumpAndExpand(tester, chinese: down, identity: down);
    expect(find.text('Không tới được: chinese-backend, identity-service'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
