import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('HanziText đặt locale zh-CN và phông dự phòng CJK', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: HanziText('你好', style: TextStyle(fontSize: 32))),
      ),
    );
    final text = tester.widget<Text>(find.byType(Text));
    expect(text.locale, const Locale('zh', 'CN'));
    expect(text.style!.locale, const Locale('zh', 'CN'));
    expect(text.style!.fontFamilyFallback, kCjkFontFallback);
    expect(text.style!.fontSize, 32);
    expect(find.text('你好'), findsOneWidget);
  });

  testWidgets('HanziText không style ⇒ kế thừa DefaultTextStyle', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DefaultTextStyle(
            style: TextStyle(fontSize: 20, color: Colors.red),
            child: HanziText('中'),
          ),
        ),
      ),
    );
    final text = tester.widget<Text>(find.byType(Text));
    expect(text.style!.fontSize, 20);
    expect(text.style!.color, Colors.red);
    expect(text.style!.locale, const Locale('zh', 'CN'));
  });

  testWidgets('LangText giữ locale truyền vào', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: LangText('xin chào', locale: Locale('vi'))),
      ),
    );
    final text = tester.widget<Text>(find.byType(Text));
    expect(text.locale, const Locale('vi'));
    expect(text.style!.fontFamilyFallback, isNull);
  });

  test('hanziStyle', () {
    final s = hanziStyle(const TextStyle(fontSize: 12));
    expect(s.locale, kZhCn);
    expect(s.fontFamilyFallback, kCjkFontFallback);
    expect(s.fontSize, 12);
  });
}
