import 'package:af_chinese/core/speech/chinese_speech.dart';
import 'package:af_chinese/features/speech/presentation/pages/voice_settings_page.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';

void main() {
  void useSmallPhone(WidgetTester tester, {double textScale = 1.0}) {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = textScale;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
  }

  testWidgets('liệt kê giọng zh, chọn giọng lưu af.speech.voice.zh, kéo tốc độ lưu af.chinese.ttsRate', (tester) async {
    useSmallPhone(tester);
    final tts = FakeAfTts(voices: zhVoices);
    final store = InMemoryKeyValueStore();
    await tester.pumpWidget(speechTestApp(const VoiceSettingsPage(), tts: tts, store: store));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    expect(find.text('你好'), findsOneWidget);
    expect(find.text('nǐ hǎo'), findsOneWidget);
    expect(find.text('Tingting'), findsOneWidget);
    expect(find.text('Mei-Jia'), findsOneWidget);
    expect(find.byType(VoiceMissingNotice), findsOneWidget); // ready ⇒ vẽ rỗng
    expect(find.text('Chưa có giọng tiếng Trung'), findsNothing);

    await tester.ensureVisible(find.text('Mei-Jia'));
    await tester.tap(find.text('Mei-Jia'));
    await tester.pumpAndSettle();
    expect(store.snapshot[speechVoiceStorageKey('zh')], 'Mei-Jia');

    // Nghe thử đọc bằng giọng vừa chọn.
    final listenButton = find.widgetWithText(OutlinedButton, 'Nghe thử');
    await tester.ensureVisible(listenButton);
    await tester.tap(listenButton);
    await tester.pumpAndSettle();
    expect(tts.spoken, ['你好']);
    expect(tts.voice?.name, 'Mei-Jia');

    await tester.ensureVisible(find.byType(Slider));
    await tester.drag(find.byType(Slider), const Offset(300, 0));
    await tester.pumpAndSettle();
    expect(store.snapshot[kTtsRateKey], '1.2');
    expect(find.text('1,20'), findsOneWidget);
  });

  testWidgets('không giọng ⇒ hướng dẫn cài giọng, nút nghe vô hiệu; chữ 1.3× không overflow', (tester) async {
    useSmallPhone(tester, textScale: 1.3);
    final tts = FakeAfTts();
    await tester.pumpWidget(speechTestApp(const VoiceSettingsPage(), tts: tts));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Chưa có giọng tiếng Trung'), findsOneWidget);
    expect(find.text('Chưa có giọng tiếng Trung trên thiết bị này.'), findsOneWidget);
    expect(tester.widget<OutlinedButton>(find.byType(OutlinedButton)).onPressed, isNull);

    // Cài giọng xong ⇒ "Dò lại giọng" chuyển sang ready.
    tts.voices = zhVoices;
    await tester.ensureVisible(find.text('Dò lại giọng'));
    await tester.tap(find.text('Dò lại giọng'));
    await tester.pumpAndSettle();
    expect(find.text('Tingting'), findsOneWidget);
    expect(tester.widget<OutlinedButton>(find.byType(OutlinedButton)).onPressed, isNotNull);
  });

  testWidgets('thiết bị không hỗ trợ ⇒ thông báo unsupported, không crash', (tester) async {
    useSmallPhone(tester);
    await tester.pumpWidget(speechTestApp(const VoiceSettingsPage(), tts: FakeAfTts(unsupported: true)));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Thiết bị không hỗ trợ đọc văn bản'), findsOneWidget);
  });
}
