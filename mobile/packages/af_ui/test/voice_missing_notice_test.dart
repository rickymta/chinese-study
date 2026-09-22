import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Widget wrap(Widget child) => MaterialApp(home: Scaffold(body: child));

  testWidgets('noVoice trên Android ⇒ hướng dẫn Google TTS + nút Dò lại', (tester) async {
    var retried = 0;
    await tester.pumpWidget(
      wrap(VoiceMissingNotice(status: SpeechStatus.noVoice, platform: TtsPlatform.android, onRetry: () => retried++)),
    );
    expect(find.text('Chưa có giọng tiếng Trung'), findsOneWidget);
    expect(find.textContaining('Cài dữ liệu giọng nói'), findsOneWidget);
    await tester.tap(find.text('Dò lại giọng'));
    expect(retried, 1);
  });

  testWidgets('noVoice iOS / web ⇒ lời hướng dẫn tương ứng', (tester) async {
    await tester.pumpWidget(wrap(const VoiceMissingNotice(status: SpeechStatus.noVoice, platform: TtsPlatform.ios)));
    expect(find.textContaining('Trợ năng → Nội dung được đọc'), findsOneWidget);
    await tester.pumpWidget(wrap(const VoiceMissingNotice(status: SpeechStatus.noVoice, platform: TtsPlatform.web)));
    expect(find.textContaining('Chrome/Edge'), findsOneWidget);
  });

  testWidgets('unsupported ⇒ tiêu đề khác, không có nút Dò lại', (tester) async {
    await tester.pumpWidget(
      wrap(VoiceMissingNotice(status: SpeechStatus.unsupported, platform: TtsPlatform.other, onRetry: () {})),
    );
    expect(find.text('Thiết bị không hỗ trợ đọc văn bản'), findsOneWidget);
    expect(find.text('Dò lại giọng'), findsNothing);
  });

  testWidgets('ready / loading ⇒ không vẽ gì', (tester) async {
    await tester.pumpWidget(wrap(const VoiceMissingNotice(status: SpeechStatus.ready)));
    expect(find.byType(Material), findsNWidgets(1)); // chỉ Scaffold
    await tester.pumpWidget(wrap(const VoiceMissingNotice(status: SpeechStatus.loading)));
    expect(find.byIcon(Icons.volume_off_outlined), findsNothing);
  });

  testWidgets('chế độ tối: chữ dùng onTertiaryContainer trên nền tertiaryContainer, 360×740 chữ 1.3× không overflow', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
    final dark = buildAfTheme(brightness: Brightness.dark, accent: afAccentChinese);
    await tester.pumpWidget(
      MaterialApp(
        theme: dark,
        home: Scaffold(
          body: VoiceMissingNotice(status: SpeechStatus.noVoice, platform: TtsPlatform.android, onRetry: () {}),
        ),
      ),
    );
    expect(tester.takeException(), isNull);
    final title = tester.widget<Text>(find.text('Chưa có giọng tiếng Trung'));
    expect(title.style?.color, dark.colorScheme.onTertiaryContainer);
    final material = tester.widget<Material>(
      find.ancestor(of: find.text('Chưa có giọng tiếng Trung'), matching: find.byType(Material)).first,
    );
    expect(material.color, dark.colorScheme.tertiaryContainer);
    // Tương phản đủ đọc: nền tối, chữ sáng.
    expect(dark.colorScheme.tertiaryContainer.computeLuminance(), lessThan(0.3));
    expect(dark.colorScheme.onTertiaryContainer.computeLuminance(), greaterThan(0.5));
  });

  test('voiceInstallInstructions đổi tên ngôn ngữ', () {
    expect(voiceInstallInstructions(TtsPlatform.ios, languageName: 'tiếng Nhật'), contains('Tiếng Nhật'));
  });
}
