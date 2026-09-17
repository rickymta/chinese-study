import 'package:af_chinese/core/widgets/hanzi_big.dart';
import 'package:af_chinese/core/widgets/meaning_status_chip.dart';
import 'package:af_chinese/core/widgets/pinyin_text.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Widget wrap(Widget child) => MaterialApp(home: Scaffold(body: child));

  group('PinyinText', () {
    testWidgets('đổi số thanh sang dấu, không gợi ý khi showSandhi tắt', (tester) async {
      await tester.pumpWidget(wrap(const PinyinText('ni3 hao3')));
      expect(find.text('nǐ hǎo'), findsOneWidget);
      expect(find.textContaining('Biến điệu'), findsNothing);
    });

    testWidgets('showSandhi ⇒ chú thích biến điệu 3-3 và 不', (tester) async {
      await tester.pumpWidget(wrap(const PinyinText('bu4 shi4', hanzi: '不是', showSandhi: true)));
      expect(find.text('bù shì'), findsOneWidget);
      expect(find.text('Biến điệu: âm tiết 1 đọc bú (不 trước thanh 4)'), findsOneWidget);
    });

    testWidgets('join nối liền có dấu nháy', (tester) async {
      await tester.pumpWidget(wrap(const PinyinText('Xi1 an1', join: true)));
      expect(find.text("Xī'ān"), findsOneWidget);
    });
  });

  group('HanziBig', () {
    testWidgets('locale zh-CN, cỡ theo HanziSize, giữ màu từ style', (tester) async {
      await tester.pumpWidget(
        wrap(
          const HanziBig(
            '直骨角',
            size: HanziSize.xl,
            style: TextStyle(color: Colors.red),
          ),
        ),
      );
      final text = tester.widget<Text>(find.byType(Text));
      expect(text.locale, const Locale('zh', 'CN'));
      expect(text.style!.fontSize, 56);
      expect(text.style!.color, Colors.red);
      expect(text.style!.locale, const Locale('zh', 'CN'));
    });
  });

  group('MeaningStatusChip', () {
    testWidgets('machine ⇒ chip "Chưa duyệt" có tooltip; reviewed/null ⇒ không vẽ', (tester) async {
      await tester.pumpWidget(wrap(const MeaningStatusChip(status: 'machine')));
      expect(find.text('Chưa duyệt'), findsOneWidget);
      expect(tester.widget<Tooltip>(find.byType(Tooltip)).message, kMachineMeaningTooltip);

      await tester.pumpWidget(wrap(const MeaningStatusChip(status: 'reviewed')));
      expect(find.text('Chưa duyệt'), findsNothing);
      await tester.pumpWidget(wrap(const MeaningStatusChip(status: null)));
      expect(find.text('Chưa duyệt'), findsNothing);
    });
  });
}
