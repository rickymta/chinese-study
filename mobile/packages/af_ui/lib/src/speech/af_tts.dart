import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter_tts/flutter_tts.dart';

/// Nền tảng chạy TTS — quyết định cách quy đổi tốc độ và lời hướng dẫn cài giọng.
enum TtsPlatform {
  android,
  ios,
  web,
  other;

  /// Suy từ nền tảng đang chạy (`kIsWeb` trước, rồi `defaultTargetPlatform` — test đổi được bằng
  /// `debugDefaultTargetPlatformOverride`).
  static TtsPlatform current() {
    if (kIsWeb) return TtsPlatform.web;
    return switch (defaultTargetPlatform) {
      TargetPlatform.android => TtsPlatform.android,
      TargetPlatform.iOS => TtsPlatform.ios,
      _ => TtsPlatform.other,
    };
  }
}

/// Trạng thái bộ đọc (tương đương `SpeechStatus` của `@af/ui` web): `loading | ready | noVoice | unsupported`.
enum SpeechStatus { loading, ready, noVoice, unsupported }

/// Một giọng đọc của engine (map `name`/`locale` từ `flutter_tts`; iOS/macOS thêm `identifier`).
@immutable
class TtsVoice {
  const TtsVoice({required this.name, required this.locale, this.identifier});

  /// Đọc từ map của `getVoices` (khoá kiểu `dynamic` tuỳ nền tảng). Thiếu `name` ⇒ null.
  static TtsVoice? fromMap(Object? raw) {
    if (raw is! Map) return null;
    final name = raw['name']?.toString();
    if (name == null || name.isEmpty) return null;
    final identifier = raw['identifier']?.toString();
    return TtsVoice(
      name: name,
      locale: raw['locale']?.toString() ?? '',
      identifier: identifier == null || identifier.isEmpty ? null : identifier,
    );
  }

  final String name;
  final String locale;
  final String? identifier;

  /// `zh_CN` / `zh-cn` ⇒ `zh-cn`.
  String get normalizedLocale => normalizeLocale(locale);

  /// Map truyền cho `FlutterTts.setVoice` (iOS ưu tiên `identifier` — README flutter_tts 4.2.5).
  Map<String, String> toVoiceMap() => {'name': name, 'locale': locale, 'identifier': ?identifier};

  static String normalizeLocale(String locale) => locale.replaceAll('_', '-').toLowerCase();

  @override
  bool operator ==(Object other) =>
      other is TtsVoice && other.name == name && other.locale == locale && other.identifier == identifier;

  @override
  int get hashCode => Object.hash(name, locale, identifier);

  @override
  String toString() => 'TtsVoice($name, $locale)';
}

/// Kết quả dò engine lúc khởi tạo.
@immutable
class TtsProbe {
  const TtsProbe({required this.status, this.voices = const []});

  final SpeechStatus status;

  /// Giọng khớp tiền tố ngôn ngữ, đã lọc Quảng Đông và sắp ưu tiên. Rỗng khi `ready` nghĩa là dùng giọng mặc định engine.
  final List<TtsVoice> voices;
}

// Tên giọng Quảng Đông: cantonese | 粤語 | 粵語 | 廣東話 | 广东话 (viết mã thoát — đây là so khớp, không hiển thị).
final RegExp _cantoneseName = RegExp(
  'cantonese|\u7CA4\u8A9E|\u7CB5\u8A9E|\u5EE3\u6771\u8A71|\u5E7F\u4E1C\u8BDD',
  caseSensitive: false,
);
final RegExp _premiumName = RegExp('enhanced|premium|natural|online', caseSensitive: false);

/// Tốc độ đọc mặc định (chậm hơn tự nhiên để nghe rõ thanh — như web `TTS_RATE_DEFAULT`).
const double kTtsRateDefault = 0.8;

/// Bọc `flutter_tts` 4.2.5 (hợp đồng mobile §5.3.7). Một ngôn ngữ một thể hiện: [langPrefix] (`zh`) lọc giọng,
/// [language] (`zh-CN`) đặt cho engine khi đọc.
///
/// Mọi lời gọi plugin đều bắt lỗi: `MissingPluginException` (nền tảng không có plugin, chạy test) ⇒ `unsupported`;
/// lỗi phát ⇒ nuốt + `afLog` (không ném lên UI).
class AfTts {
  AfTts({
    FlutterTts? tts,
    TtsPlatform? platform,
    this.langPrefix = 'zh',
    this.language = 'zh-CN',
    this.voiceRetryDelay = const Duration(milliseconds: 1000),
  }) : _tts = tts ?? FlutterTts(),
       platform = platform ?? TtsPlatform.current();

  final FlutterTts _tts;
  final TtsPlatform platform;
  final String langPrefix;
  final String language;

  /// Web: Chrome nạp giọng bất đồng bộ — `getVoices()` lần đầu thường rỗng (sự kiện `voiceschanged` bắn sau). Nếu
  /// rỗng thì chờ khoảng này rồi hỏi lại MỘT lần (tương đương `listVoices(timeoutMs)` của `@af/ui` web).
  final Duration voiceRetryDelay;

  bool _configured = false;

  /// Dò engine: cấu hình một lần (chờ đọc xong, iOS phát cả khi gạt im lặng), lấy danh sách giọng, lọc + sắp.
  /// Rỗng ⇒ thử `isLanguageAvailable(language)` ⇒ `true` thì `ready` với giọng mặc định, ngược lại `noVoice`.
  Future<TtsProbe> init() async {
    try {
      await _configureOnce();
      var raw = await _tts.getVoices;
      var voices = filterAndSort(_parseVoices(raw), langPrefix);
      if (voices.isEmpty && platform == TtsPlatform.web && raw is List && raw.isEmpty) {
        await Future<void>.delayed(voiceRetryDelay);
        raw = await _tts.getVoices;
        voices = filterAndSort(_parseVoices(raw), langPrefix);
      }
      if (voices.isNotEmpty) return TtsProbe(status: SpeechStatus.ready, voices: voices);
      final available = await _tts.isLanguageAvailable(language);
      // Bản web của plugin trả null cho MỌI lời gọi khi trình duyệt không có `speechSynthesis` ⇒ unsupported.
      if (raw == null && available == null) return const TtsProbe(status: SpeechStatus.unsupported);
      return TtsProbe(status: available == true ? SpeechStatus.ready : SpeechStatus.noVoice);
    } on MissingPluginException catch (e) {
      afLog('AfTts.init: không có plugin TTS trên nền tảng này ($e)');
      return const TtsProbe(status: SpeechStatus.unsupported);
    } on Object catch (e) {
      afLog('AfTts.init lỗi: $e');
      return const TtsProbe(status: SpeechStatus.noVoice);
    }
  }

  Future<void> _configureOnce() async {
    if (_configured) return;
    await _tts.awaitSpeakCompletion(true);
    if (platform == TtsPlatform.ios) {
      // Phát cả khi gạt im lặng (BA-mặc định §5.3.7); trộn với nhạc nền thay vì cắt.
      await _tts.setSharedInstance(true);
      await _tts.setIosAudioCategory(IosTextToSpeechAudioCategory.playback, [
        IosTextToSpeechAudioCategoryOptions.mixWithOthers,
      ], IosTextToSpeechAudioMode.defaultMode);
    }
    _configured = true;
  }

  /// Đọc [text]: dừng lượt trước, đặt ngôn ngữ/giọng/tốc độ rồi phát, trả về khi đọc xong (hoặc bị huỷ).
  /// Có đồng hồ an toàn (8 s + 400 ms mỗi ký tự, chia tốc độ) phòng engine "kẹt" không báo xong.
  Future<void> speak(String text, {double rate = kTtsRateDefault, TtsVoice? voice}) async {
    if (text.trim().isEmpty) return;
    try {
      await _configureOnce();
      await _tts.stop();
      await _tts.setLanguage(voice?.locale.isNotEmpty == true ? voice!.locale : language);
      if (voice != null) await _tts.setVoice(voice.toVoiceMap());
      await _tts.setSpeechRate(platformRate(rate, platform));
      final guard = Duration(milliseconds: ((8000 + text.length * 400) / rate.clamp(0.1, 10)).round());
      await _tts.speak(text).timeout(guard, onTimeout: () => null);
    } on Object catch (e) {
      // Lỗi phát (engine bận, thiếu quyền âm thanh...) không làm hỏng màn hình — chỉ ghi log.
      afLog('AfTts.speak lỗi: $e');
    }
  }

  /// Huỷ lượt đang đọc (rời màn, đổi thẻ). Không ném.
  Future<void> stop() async {
    try {
      await _tts.stop();
    } on Object catch (e) {
      afLog('AfTts.stop lỗi: $e');
    }
  }

  /// Quy đổi tốc độ "người dùng" (1,0 = bình thường, 0,5–1,2) sang giá trị plugin theo nền tảng — đọc từ mã
  /// flutter_tts 4.2.5:
  /// - web: `utterance.rate = rate` (1,0 = bình thường) ⇒ giữ nguyên;
  /// - Android: plugin gọi `tts.setSpeechRate(rate * 2)` ⇒ 0,5 = bình thường ⇒ nhân 0,5;
  /// - iOS/macOS: gán thẳng `AVSpeechUtterance.rate` (0,5 = `AVSpeechUtteranceDefaultSpeechRate`) ⇒ nhân 0,5.
  /// Khác BA-mặc định §5.3.7 (Android giữ nguyên) vì mã plugin nhân đôi — kiểm trên máy thật (VERIFY-DEVICE #14).
  static double platformRate(double rate, TtsPlatform platform) {
    final r = rate.isFinite ? rate : kTtsRateDefault;
    return switch (platform) {
      TtsPlatform.web => r.clamp(0.1, 10.0),
      TtsPlatform.android || TtsPlatform.ios || TtsPlatform.other => (0.5 * r).clamp(0.1, 1.0),
    };
  }

  /// Giọng Quảng Đông (`zh-HK`, `zh-yue`, `yue-*`, tên có "Cantonese"/粤語) đọc chữ Hán khác hẳn phổ thông ⇒ loại
  /// khỏi danh sách (người học pinyin nghe nhầm mà không biết) — cùng luật với `@af/ui` web.
  static bool isCantonese(TtsVoice v) {
    final lang = v.normalizedLocale;
    return lang == 'zh-hk' || lang.startsWith('zh-yue') || lang.startsWith('yue') || _cantoneseName.hasMatch(v.name);
  }

  /// Lọc giọng có locale bắt đầu bằng [prefix] (bỏ Quảng Đông); sắp: khớp đúng `<prefix>-cn` trước, rồi tên
  /// `Enhanced|Premium|Natural|Online`, rồi còn lại theo tên.
  static List<TtsVoice> filterAndSort(List<TtsVoice> voices, String prefix) {
    final p = TtsVoice.normalizeLocale(prefix);
    final matched = voices.where((v) => v.normalizedLocale.startsWith(p) && !isCantonese(v)).toList();
    int score(TtsVoice v) {
      var s = 0;
      final lang = v.normalizedLocale;
      if (lang == '$p-cn' || lang == p) s += 100;
      if (_premiumName.hasMatch(v.name)) s += 10;
      return s;
    }

    matched.sort((a, b) {
      final d = score(b) - score(a);
      return d != 0 ? d : a.name.compareTo(b.name);
    });
    return matched;
  }

  static List<TtsVoice> _parseVoices(Object? raw) {
    if (raw is! List) return const [];
    return raw.map(TtsVoice.fromMap).whereType<TtsVoice>().toList();
  }
}
