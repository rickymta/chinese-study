import 'dart:async';

import 'package:af_chinese/core/speech/chinese_speech.dart';
import 'package:af_chinese/core/widgets/speak_button.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';

void main() {
  group('SpeakButton', () {
    testWidgets('có giọng ⇒ bấm đọc đúng chữ với tốc độ đã lưu; onDone gọi sau khi xong', (tester) async {
      final tts = FakeAfTts(voices: zhVoices);
      final store = InMemoryKeyValueStore({kTtsRateKey: '1.1'});
      var done = 0;
      await tester.pumpWidget(
        speechTestApp(
          Scaffold(
            body: SpeakButton(text: '你好', onDone: () => done++),
          ),
          tts: tts,
          store: store,
        ),
      );
      await tester.pumpAndSettle();
      final button = tester.widget<IconButton>(find.byType(IconButton));
      expect(button.onPressed, isNotNull);

      await tester.tap(find.byType(IconButton));
      await tester.pumpAndSettle();
      expect(tts.spoken, ['你好']);
      expect(tts.rate, 1.1);
      expect(tts.voice?.name, 'Tingting');
      expect(done, 1);
    });

    testWidgets('rate ghi đè thắng tốc độ đã lưu', (tester) async {
      final tts = FakeAfTts(voices: zhVoices);
      await tester.pumpWidget(
        speechTestApp(
          const Scaffold(
            body: SpeakButton(text: '好', variant: SpeakButtonVariant.button, label: 'Nghe thử', rate: 0.6),
          ),
          tts: tts,
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Nghe thử'));
      await tester.pumpAndSettle();
      expect(tts.rate, 0.6);
    });

    testWidgets('không giọng ⇒ nút vô hiệu + tooltip "Chưa có giọng tiếng Trung"', (tester) async {
      final tts = FakeAfTts();
      await tester.pumpWidget(
        speechTestApp(
          const Scaffold(body: SpeakButton(text: '你好')),
          tts: tts,
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.widget<IconButton>(find.byType(IconButton)).onPressed, isNull);
      expect(tester.widget<Tooltip>(find.byType(Tooltip).first).message, kNoVoiceTooltip);
      await tester.tap(find.byType(IconButton), warnIfMissed: false);
      await tester.pump();
      expect(tts.spoken, isEmpty);
    });

    testWidgets('plugin thiếu (MissingPluginException) ⇒ vô hiệu, không ném', (tester) async {
      final tts = FakeAfTts(unsupported: true);
      await tester.pumpWidget(
        speechTestApp(
          const Scaffold(body: SpeakButton(text: '你好')),
          tts: tts,
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(tester.widget<IconButton>(find.byType(IconButton)).onPressed, isNull);
    });

    testWidgets('rời màn khi đang đọc ⇒ huỷ (stop được gọi)', (tester) async {
      final tts = FakeAfTts(voices: zhVoices)..speakGate = Completer<void>();
      await tester.pumpWidget(
        speechTestApp(
          const Scaffold(body: SpeakButton(text: '你好')),
          tts: tts,
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byType(IconButton));
      await tester.pump();
      final stopsBefore = tts.stops;
      // Thay widget ⇒ SpeakButton dispose.
      await tester.pumpWidget(speechTestApp(const Scaffold(body: SizedBox()), tts: tts));
      await tester.pump();
      expect(tts.stops, stopsBefore + 1);
      tts.speakGate!.complete();
      await tester.pumpAndSettle();
    });
  });

  group('TtsRateController', () {
    test('mặc định 0,8; nạp giá trị đã lưu; setRate kẹp và lưu', () async {
      expect(clampTtsRate(2), 1.2);
      expect(clampTtsRate(0.1), 0.5);
      expect(clampTtsRate(0.856), 0.86);
      expect(clampTtsRate(double.nan), kTtsRateDefault);
    });
  });
}
