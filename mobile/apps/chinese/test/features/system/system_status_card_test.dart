import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  testWidgets('cả hai service chạy ⇒ hai chip "đang chạy", không có khối cảnh báo', (tester) async {
    await tester.pumpWidget(
      buildTestApp(chineseAdapter: okSystemInfo('chinese-backend'), identityAdapter: okSystemInfo('identity-service')),
    );
    await tester.pumpAndSettle();
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
    await tester.pumpWidget(buildTestApp(chineseAdapter: chinese, identityAdapter: okSystemInfo('identity-service')));
    await tester.pumpAndSettle();
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
    await tester.pumpWidget(buildTestApp(chineseAdapter: down, identityAdapter: down));
    await tester.pumpAndSettle();
    expect(find.text('Không tới được: chinese-backend, identity-service'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
