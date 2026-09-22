import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/fake_flutter_tts.dart';

void main() {
  ProviderContainer makeContainer(
    FakeFlutterTts fake, {
    TtsPlatform platform = TtsPlatform.android,
    KeyValueStore? store,
  }) {
    return ProviderContainer.test(
      overrides: [
        keyValueStoreProvider.overrideWithValue(store ?? InMemoryKeyValueStore()),
        afTtsProvider.overrideWithValue(AfTts(tts: fake, platform: platform)),
      ],
    );
  }

  Future<void> settle() => Future<void>.delayed(Duration.zero);

  group('SpeechController', () {
    test('ban đầu loading; không giọng ⇒ noVoice, voice null, canSpeak false', () async {
      final c = makeContainer(FakeFlutterTts());
      expect(c.read(speechControllerProvider).status, SpeechStatus.loading);
      await settle();
      final s = c.read(speechControllerProvider);
      expect(s.status, SpeechStatus.noVoice);
      expect(s.voice, isNull);
      expect(s.canSpeak, isFalse);
    });

    test('có zh_CN ⇒ ready, chọn giọng zh-CN ưu tiên, loại Quảng Đông', () async {
      final c = makeContainer(FakeFlutterTts(voices: sampleVoices));
      c.read(speechControllerProvider);
      await settle();
      final s = c.read(speechControllerProvider);
      expect(s.status, SpeechStatus.ready);
      expect(s.voice?.name, 'Tingting (Enhanced)');
      expect(s.voices.map((v) => v.name), isNot(contains('Sin-ji')));
      expect(s.voices.map((v) => v.name), isNot(contains('Cantonese Female')));
    });

    test('MissingPluginException ⇒ unsupported', () async {
      final c = makeContainer(FakeFlutterTts(throwMissing: true));
      c.read(speechControllerProvider);
      await settle();
      expect(c.read(speechControllerProvider).status, SpeechStatus.unsupported);
    });

    test('giọng đã lưu af.speech.voice.zh được chọn lại; tên không còn ⇒ giọng đầu', () async {
      final store = InMemoryKeyValueStore({speechVoiceStorageKey('zh'): 'Mei-Jia'});
      final c = makeContainer(FakeFlutterTts(voices: sampleVoices), store: store);
      c.read(speechControllerProvider);
      await settle();
      expect(c.read(speechControllerProvider).voice?.name, 'Mei-Jia');

      final store2 = InMemoryKeyValueStore({speechVoiceStorageKey('zh'): 'da-xoa'});
      final c2 = makeContainer(FakeFlutterTts(voices: sampleVoices), store: store2);
      c2.read(speechControllerProvider);
      await settle();
      expect(c2.read(speechControllerProvider).voice?.name, 'Tingting (Enhanced)');
    });

    test('setVoice đổi state và lưu bền; chọn trước khi dò xong không bị đè', () async {
      final store = InMemoryKeyValueStore();
      final c = makeContainer(FakeFlutterTts(voices: sampleVoices), store: store);
      final n = c.read(speechControllerProvider.notifier);
      const mei = TtsVoice(name: 'Mei-Jia', locale: 'zh-TW');
      await n.setVoice(mei); // trước khi _init hoàn tất
      await settle();
      expect(c.read(speechControllerProvider).voice, mei);
      expect(store.snapshot[speechVoiceStorageKey('zh')], 'Mei-Jia');
    });

    test('speak: cờ speaking bật khi đang đọc, tắt khi xong; tốc độ quy đổi iOS', () async {
      final fake = FakeFlutterTts(voices: sampleVoices)..speakGate = Completer<void>();
      final c = makeContainer(fake, platform: TtsPlatform.ios);
      c.read(speechControllerProvider);
      await settle();
      final n = c.read(speechControllerProvider.notifier);
      final done = n.speak('你好', rate: 1.0);
      await settle();
      expect(c.read(speechControllerProvider).speaking, isTrue);
      expect(fake.rate, 0.5);
      expect(fake.language, 'zh-CN');
      fake.speakGate!.complete();
      await done;
      expect(c.read(speechControllerProvider).speaking, isFalse);
      expect(fake.spoken, ['你好']);
    });

    test('speak khi noVoice ⇒ không gọi plugin', () async {
      final fake = FakeFlutterTts();
      final c = makeContainer(fake);
      c.read(speechControllerProvider);
      await settle();
      await c.read(speechControllerProvider.notifier).speak('你好');
      expect(fake.spoken, isEmpty);
      expect(c.read(speechControllerProvider).speaking, isFalse);
    });

    test('cancel ⇒ stop + speaking false; lượt cũ kết thúc sau không đè cờ lượt mới', () async {
      final gate1 = Completer<void>();
      final fake = FakeFlutterTts(voices: sampleVoices)..speakGate = gate1;
      final c = makeContainer(fake);
      c.read(speechControllerProvider);
      await settle();
      final n = c.read(speechControllerProvider.notifier);
      final first = n.speak('一');
      await settle();
      expect(c.read(speechControllerProvider).speaking, isTrue);
      await n.cancel();
      expect(c.read(speechControllerProvider).speaking, isFalse);
      expect(fake.calls.last, 'stop');

      // Lượt mới bắt đầu, lượt cũ mới xong ⇒ cờ vẫn true.
      final gate2 = Completer<void>();
      fake.speakGate = gate2;
      final second = n.speak('二');
      await settle();
      expect(c.read(speechControllerProvider).speaking, isTrue);
      // Lượt đầu (đã bị huỷ) kết thúc muộn ⇒ không được tắt cờ của lượt hai.
      gate1.complete();
      await first;
      expect(c.read(speechControllerProvider).speaking, isTrue);
      gate2.complete();
      await second;
      expect(c.read(speechControllerProvider).speaking, isFalse);
    });

    test('refreshVoices sau khi cài giọng ⇒ từ noVoice sang ready', () async {
      final fake = FakeFlutterTts();
      final c = makeContainer(fake);
      c.read(speechControllerProvider);
      await settle();
      expect(c.read(speechControllerProvider).status, SpeechStatus.noVoice);
      fake.voices = sampleVoices;
      await c.read(speechControllerProvider.notifier).refreshVoices();
      expect(c.read(speechControllerProvider).status, SpeechStatus.ready);
      expect(c.read(speechControllerProvider).voice, isNotNull);
    });
  });
}
