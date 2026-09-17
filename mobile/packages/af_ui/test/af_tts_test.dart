import 'package:af_ui/af_ui.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_tts/flutter_tts.dart' show IosTextToSpeechAudioCategory;

import 'helpers/fake_flutter_tts.dart';

void main() {
  group('TtsVoice', () {
    test('fromMap đọc name/locale/identifier, thiếu name ⇒ null', () {
      expect(TtsVoice.fromMap({'name': 'A', 'locale': 'zh_CN'}), const TtsVoice(name: 'A', locale: 'zh_CN'));
      expect(TtsVoice.fromMap({'name': 'A', 'locale': 'zh-CN', 'identifier': 'id'})?.identifier, 'id');
      expect(TtsVoice.fromMap({'locale': 'zh-CN'}), isNull);
      expect(TtsVoice.fromMap('x'), isNull);
      expect(const TtsVoice(name: 'A', locale: 'zh_CN').normalizedLocale, 'zh-cn');
    });
  });

  group('AfTts.filterAndSort / isCantonese', () {
    test('lọc theo tiền tố zh, loại Quảng Đông, zh-CN đứng trước, Enhanced ưu tiên', () {
      final voices = sampleVoices.map(TtsVoice.fromMap).whereType<TtsVoice>().toList();
      final out = AfTts.filterAndSort(voices, 'zh');
      expect(out.map((v) => v.name), ['Tingting (Enhanced)', 'cmn-cn-x-ccc', 'Mei-Jia']);
    });

    test('isCantonese theo locale zh-HK / yue / tên Cantonese', () {
      expect(AfTts.isCantonese(const TtsVoice(name: 'x', locale: 'zh-HK')), isTrue);
      expect(AfTts.isCantonese(const TtsVoice(name: 'x', locale: 'yue-HK')), isTrue);
      expect(AfTts.isCantonese(const TtsVoice(name: 'x', locale: 'zh_yue')), isTrue);
      expect(AfTts.isCantonese(const TtsVoice(name: '粤語 女', locale: 'zh-CN')), isTrue);
      expect(AfTts.isCantonese(const TtsVoice(name: 'Tingting', locale: 'zh-CN')), isFalse);
    });
  });

  group('AfTts.platformRate — quy đổi theo nền tảng (đọc từ mã flutter_tts 4.2.5)', () {
    test('web giữ nguyên', () => expect(AfTts.platformRate(0.8, TtsPlatform.web), 0.8));
    test('Android nhân 0,5 (plugin nhân đôi)', () => expect(AfTts.platformRate(0.8, TtsPlatform.android), 0.4));
    test('iOS nhân 0,5 (AVSpeech 0,5 = bình thường)', () => expect(AfTts.platformRate(1.2, TtsPlatform.ios), 0.6));
    test('kẹp 0,1–1,0 ở native', () {
      expect(AfTts.platformRate(0.1, TtsPlatform.ios), 0.1);
      expect(AfTts.platformRate(5, TtsPlatform.android), 1.0);
    });
    test('NaN ⇒ tốc độ mặc định', () => expect(AfTts.platformRate(double.nan, TtsPlatform.web), kTtsRateDefault));
  });

  group('AfTts.init', () {
    test('có giọng zh ⇒ ready + danh sách đã sắp; cấu hình awaitSpeakCompletion một lần', () async {
      final fake = FakeFlutterTts(voices: sampleVoices);
      final tts = AfTts(tts: fake, platform: TtsPlatform.android);
      final probe = await tts.init();
      expect(probe.status, SpeechStatus.ready);
      expect(probe.voices.first.name, 'Tingting (Enhanced)');
      expect(fake.awaitCompletion, isTrue);
      expect(fake.sharedInstance, isFalse); // chỉ iOS
      await tts.init();
      expect(fake.calls.where((c) => c == 'awaitSpeakCompletion').length, 1);
    });

    test('iOS: setSharedInstance + audio category playback', () async {
      final fake = FakeFlutterTts(voices: sampleVoices);
      await AfTts(tts: fake, platform: TtsPlatform.ios).init();
      expect(fake.sharedInstance, isTrue);
      expect(fake.iosCategory, IosTextToSpeechAudioCategory.playback);
    });

    test('không giọng zh nhưng isLanguageAvailable(zh-CN) ⇒ ready với giọng mặc định', () async {
      final fake = FakeFlutterTts(voices: const [], languageAvailable: true);
      final probe = await AfTts(tts: fake, platform: TtsPlatform.android).init();
      expect(probe.status, SpeechStatus.ready);
      expect(probe.voices, isEmpty);
    });

    test('không giọng, ngôn ngữ không có ⇒ noVoice', () async {
      final fake = FakeFlutterTts(
        voices: const [
          {'name': 'en', 'locale': 'en-US'},
        ],
      );
      final tts = AfTts(tts: fake, platform: TtsPlatform.web, voiceRetryDelay: Duration.zero);
      expect((await tts.init()).status, SpeechStatus.noVoice);
    });

    test('web: getVoices lần đầu rỗng ⇒ chờ voiceRetryDelay rồi hỏi lại một lần', () async {
      final fake = _LateVoicesTts(sampleVoices);
      final tts = AfTts(tts: fake, platform: TtsPlatform.web, voiceRetryDelay: Duration.zero);
      final probe = await tts.init();
      expect(probe.status, SpeechStatus.ready);
      expect(fake.calls.where((c) => c == 'getVoices').length, 2);

      // Android: không dò lại (engine trả ngay).
      final fake2 = _LateVoicesTts(sampleVoices);
      final probe2 = await AfTts(tts: fake2, platform: TtsPlatform.android, voiceRetryDelay: Duration.zero).init();
      expect(probe2.status, SpeechStatus.noVoice);
      expect(fake2.calls.where((c) => c == 'getVoices').length, 1);
    });

    test('MissingPluginException ⇒ unsupported', () async {
      final fake = FakeFlutterTts(throwMissing: true);
      expect((await AfTts(tts: fake, platform: TtsPlatform.other).init()).status, SpeechStatus.unsupported);
    });

    test('web không có speechSynthesis (plugin trả null) ⇒ unsupported', () async {
      final fake = FakeFlutterTts(nullResults: true);
      final tts = AfTts(tts: fake, platform: TtsPlatform.web, voiceRetryDelay: Duration.zero);
      expect((await tts.init()).status, SpeechStatus.unsupported);
    });
  });

  group('AfTts.speak / stop', () {
    test('stop trước, đặt ngôn ngữ theo giọng, setVoice, tốc độ quy đổi, rồi speak', () async {
      final fake = FakeFlutterTts(voices: sampleVoices);
      final tts = AfTts(tts: fake, platform: TtsPlatform.ios);
      await tts.init();
      fake.calls.clear();
      const voice = TtsVoice(name: 'Tingting (Enhanced)', locale: 'zh-CN', identifier: 'id');
      await tts.speak('你好', rate: 0.8, voice: voice);
      expect(fake.calls, ['stop', 'setLanguage', 'setVoice', 'setSpeechRate', 'speak']);
      expect(fake.language, 'zh-CN');
      expect(fake.voice, {'name': 'Tingting (Enhanced)', 'locale': 'zh-CN', 'identifier': 'id'});
      expect(fake.rate, 0.4);
      expect(fake.spoken, ['你好']);
    });

    test('không giọng ⇒ không setVoice, ngôn ngữ mặc định zh-CN; chuỗi trống ⇒ không gọi gì', () async {
      final fake = FakeFlutterTts(languageAvailable: true);
      final tts = AfTts(tts: fake, platform: TtsPlatform.web, voiceRetryDelay: Duration.zero);
      await tts.speak('  ');
      expect(fake.spoken, isEmpty);
      await tts.speak('好');
      expect(fake.language, 'zh-CN');
      expect(fake.voice, isNull);
      expect(fake.rate, 0.8);
    });

    test('plugin ném khi phát ⇒ nuốt, không ném lên', () async {
      final fake = FakeFlutterTts(throwMissing: true);
      await expectLater(AfTts(tts: fake, platform: TtsPlatform.other).speak('好'), completes);
      await expectLater(AfTts(tts: fake, platform: TtsPlatform.other).stop(), completes);
    });
  });
}

/// Giống Chrome: lần `getVoices` đầu trả rỗng, lần sau mới có.
class _LateVoicesTts extends FakeFlutterTts {
  _LateVoicesTts(this.later);

  final List<Map<String, String>> later;
  int _n = 0;

  @override
  Future<dynamic> get getVoices async {
    calls.add('getVoices');
    return ++_n == 1 ? const <Map<String, String>>[] : later;
  }
}
