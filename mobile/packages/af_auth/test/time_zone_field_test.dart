import 'package:af_auth/af_auth.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Widget host({required String value, required ValueChanged<String> onChanged, String? device, String? error}) {
    return ProviderScope(
      overrides: [availableTimeZonesProvider.overrideWith((_) async => kFallbackTimeZones)],
      child: MaterialApp(
        theme: buildAfTheme(brightness: Brightness.light, accent: afAccentChinese),
        home: Scaffold(
          body: Padding(
            padding: const EdgeInsets.all(16),
            child: TimeZoneField(value: value, onChanged: onChanged, deviceTimeZone: device, errorText: error),
          ),
        ),
      ),
    );
  }

  testWidgets('chạm ô ⇒ sheet có ô tìm; gõ "ho chi" ⇒ Asia/Ho_Chi_Minh; chọn ⇒ onChanged', (tester) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    String? picked;
    await tester.pumpWidget(host(value: 'Asia/Tokyo', onChanged: (v) => picked = v, device: 'Asia/Tokyo'));
    await tester.pumpAndSettle();
    expect(find.text('Asia/Tokyo'), findsOneWidget);

    await tester.tap(find.byType(TextFormField));
    await tester.pumpAndSettle();
    expect(find.text('Chọn múi giờ'), findsOneWidget);
    expect(find.text('Múi giờ của máy'), findsOneWidget); // ghim đầu

    await tester.enterText(find.byType(TextField).last, 'ho chi');
    await tester.pumpAndSettle();
    expect(find.text('Asia/Ho_Chi_Minh'), findsOneWidget);
    expect(find.text('Asia/Bangkok'), findsNothing);
    expect(find.text('Múi giờ của máy'), findsNothing); // máy (Tokyo) không khớp câu hỏi ⇒ không ghim

    await tester.tap(find.text('Asia/Ho_Chi_Minh'));
    await tester.pumpAndSettle();
    expect(picked, 'Asia/Ho_Chi_Minh');
    expect(find.text('Chọn múi giờ'), findsNothing);
  });

  testWidgets('không khớp ⇒ trạng thái rỗng; lỗi máy chủ hiện dưới ô', (tester) async {
    await tester.pumpWidget(host(value: 'UTC', onChanged: (_) {}, error: 'Múi giờ không hợp lệ.'));
    await tester.pumpAndSettle();
    expect(find.text('Múi giờ không hợp lệ.'), findsOneWidget);

    await tester.tap(find.byType(TextFormField));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).last, 'xyzxyz');
    await tester.pumpAndSettle();
    expect(find.text('Không có múi giờ khớp'), findsOneWidget);
  });
}
