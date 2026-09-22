import 'package:af_chinese/features/profile/presentation/widgets/appearance_tab.dart';
import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/speech_helpers.dart';

/// Test giọng đọc chuyển từ `voice_settings_page_test.dart` (M3) — màn tạm `/giong-doc` đã gỡ ở M4.
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

  Widget host(FakeAfTts tts, {LearningSettings? settings}) => speechTestApp(
    const Scaffold(body: AppearanceTab()),
    tts: tts,
    settings: settings,
  );

  testWidgets('liệt kê giọng zh (tên qua HanziText, locale zh-CN), chọn giọng lưu af.speech.voice.zh, nghe thử', (
    tester,
  ) async {
    useSmallPhone(tester);
    final tts = FakeAfTts(voices: zhVoices);
    await tester.pumpWidget(host(tts, settings: LearningSettings.defaults));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    expect(find.text('你好'), findsOneWidget);
    expect(find.text('nǐ hǎo'), findsOneWidget);
    expect(find.text('Tingting'), findsOneWidget);
    expect(find.text('Mei-Jia'), findsOneWidget);
    final nameText = tester.widget<Text>(find.text('Tingting'));
    expect(nameText.locale, const Locale('zh', 'CN'));
    expect(find.text('Chưa có giọng tiếng Trung'), findsNothing);
    expect(find.text('0,80'), findsOneWidget);

    await tester.ensureVisible(find.text('Mei-Jia'));
    await tester.tap(find.text('Mei-Jia'));
    await tester.pumpAndSettle();
    final ctx = tester.element(find.byType(AppearanceTab));
    final store = ProviderScope.containerOf(ctx).read(keyValueStoreProvider);
    expect(await store.getString(speechVoiceStorageKey('zh')), 'Mei-Jia');

    await tester.ensureVisible(find.widgetWithText(OutlinedButton, 'Nghe thử'));
    await tester.tap(find.widgetWithText(OutlinedButton, 'Nghe thử'));
    await tester.pumpAndSettle();
    expect(tts.spoken, ['你好']);
    expect(tts.voice?.name, 'Mei-Jia');
    expect(tts.rate, 0.8);
  });

  testWidgets('không giọng ⇒ hướng dẫn cài giọng, nút nghe vô hiệu; Dò lại ⇒ ready; chữ 1.3× không overflow', (
    tester,
  ) async {
    useSmallPhone(tester, textScale: 1.3);
    final tts = FakeAfTts();
    await tester.pumpWidget(host(tts));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Chưa có giọng tiếng Trung'), findsOneWidget);
    expect(find.text('Chưa có giọng tiếng Trung trên thiết bị này.'), findsOneWidget);
    expect(tester.widget<OutlinedButton>(find.byType(OutlinedButton)).onPressed, isNull);

    tts.voices = zhVoices;
    await tester.ensureVisible(find.text('Dò lại giọng'));
    await tester.tap(find.text('Dò lại giọng'));
    await tester.pumpAndSettle();
    expect(find.text('Tingting'), findsOneWidget);
    expect(tester.widget<OutlinedButton>(find.byType(OutlinedButton)).onPressed, isNotNull);
  });

  testWidgets('thiết bị không hỗ trợ ⇒ thông báo unsupported, không crash', (tester) async {
    useSmallPhone(tester);
    await tester.pumpWidget(host(FakeAfTts(unsupported: true)));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Thiết bị không hỗ trợ đọc văn bản'), findsOneWidget);
  });
}
