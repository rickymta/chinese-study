import 'package:af_chinese/shell/app_shell.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../helpers/test_app.dart';

void main() {
  Future<void> pumpApp(WidgetTester tester) async {
    await tester.pumpWidget(
      buildTestApp(chineseAdapter: okSystemInfo('chinese-backend'), identityAdapter: okSystemInfo('identity-service')),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('shell hiện 5 nhãn nav đúng thứ tự; chuyển nhánh đổi trang', (tester) async {
    await pumpApp(tester);
    final labels = tester.widgetList<NavigationDestination>(find.byType(NavigationDestination)).map((d) => d.label);
    expect(labels, ['Trang chủ', 'Ôn tập', 'Bài học', 'Luyện viết', 'Thêm']);
    expect(kShellDestinations.map((d) => d.label), labels);
    expect(find.text('AntFarm · Tiếng Trung'), findsOneWidget);

    await tester.tap(find.text('Bài học'));
    await tester.pumpAndSettle();
    expect(find.text('Tính năng này sắp có trên ứng dụng'), findsOneWidget);

    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    expect(find.text('Pinyin & luyện thanh'), findsOneWidget);
    expect(find.text('Giấy phép & nguồn'), findsOneWidget);

    // Push trang con trong nhánh "Thêm" ⇒ bottom nav vẫn còn.
    await tester.tap(find.text('Tra từ'));
    await tester.pumpAndSettle();
    expect(find.text('Tính năng này sắp có trên ứng dụng'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
  });

  testWidgets('"Thêm" không còn khối Giao diện/Giọng đọc (M4 chuyển vào Hồ sơ)', (tester) async {
    await pumpApp(tester);
    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    expect(find.text('Giao diện'), findsNothing);
    expect(find.text('Giọng đọc'), findsNothing);
    expect(find.textContaining('giao diện, giọng đọc'), findsOneWidget);
  });

  testWidgets('màn 360×740 và chữ 1.3× không overflow ở trang chủ / Thêm', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });

    await pumpApp(tester);
    expect(tester.takeException(), isNull);
    expect(find.text('Tiếng Trung: đang chạy'), findsOneWidget);

    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Hồ sơ'), findsOneWidget);
  });
}
