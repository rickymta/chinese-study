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

  test('voiceInstallInstructions đổi tên ngôn ngữ', () {
    expect(voiceInstallInstructions(TtsPlatform.ios, languageName: 'tiếng Nhật'), contains('Tiếng Nhật'));
  });
}
