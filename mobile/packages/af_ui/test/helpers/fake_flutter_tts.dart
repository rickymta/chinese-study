import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_tts/flutter_tts.dart';

/// `FlutterTts` giả cho test `AfTts`/`SpeechController`: ghi lại lời gọi, trả danh sách giọng cấu hình sẵn.
///
/// [throwMissing] ⇒ mọi lời gọi ném `MissingPluginException` (nền tảng không có plugin). [nullResults] ⇒ trả null như
/// bản web khi trình duyệt không có `speechSynthesis`.
class FakeFlutterTts extends Fake implements FlutterTts {
  FakeFlutterTts({
    this.voices = const [],
    this.languageAvailable = false,
    this.throwMissing = false,
    this.nullResults = false,
  });

  List<Map<String, String>> voices;
  bool languageAvailable;
  bool throwMissing;
  bool nullResults;

  final calls = <String>[];
  final spoken = <String>[];
  String? language;
  Map<String, String>? voice;
  double? rate;
  bool? awaitCompletion;
  bool sharedInstance = false;
  IosTextToSpeechAudioCategory? iosCategory;

  /// Khi đặt, `speak` chờ tới khi completer hoàn thành (mô phỏng đang đọc).
  Completer<void>? speakGate;

  void _guard(String name) {
    calls.add(name);
    if (throwMissing) throw MissingPluginException('No implementation found for method $name on channel flutter_tts');
  }

  @override
  Future<dynamic> awaitSpeakCompletion(bool awaitCompletion) async {
    _guard('awaitSpeakCompletion');
    this.awaitCompletion = awaitCompletion;
    return nullResults ? null : 1;
  }

  @override
  Future<dynamic> setSharedInstance(bool sharedSession) async {
    _guard('setSharedInstance');
    sharedInstance = sharedSession;
    return 1;
  }

  @override
  Future<dynamic> setIosAudioCategory(
    IosTextToSpeechAudioCategory category,
    List<IosTextToSpeechAudioCategoryOptions> options, [
    IosTextToSpeechAudioMode mode = IosTextToSpeechAudioMode.defaultMode,
  ]) async {
    _guard('setIosAudioCategory');
    iosCategory = category;
    return 1;
  }

  @override
  Future<dynamic> get getVoices async {
    _guard('getVoices');
    return nullResults ? null : voices;
  }

  @override
  Future<dynamic> isLanguageAvailable(String language) async {
    _guard('isLanguageAvailable');
    return nullResults ? null : languageAvailable;
  }

  @override
  Future<dynamic> setLanguage(String language) async {
    _guard('setLanguage');
    this.language = language;
    return 1;
  }

  @override
  Future<dynamic> setVoice(Map<String, String> voice) async {
    _guard('setVoice');
    this.voice = voice;
    return 1;
  }

  @override
  Future<dynamic> setSpeechRate(double rate) async {
    _guard('setSpeechRate');
    this.rate = rate;
    return 1;
  }

  @override
  Future<dynamic> speak(String text, {bool focus = false}) async {
    _guard('speak');
    spoken.add(text);
    if (speakGate != null) await speakGate!.future;
    return 1;
  }

  @override
  Future<dynamic> stop() async {
    _guard('stop');
    return 1;
  }
}

/// Giọng mẫu như engine trả về (Android `zh_CN`, iOS `zh-CN`, Quảng Đông `zh-HK`, Đài Loan `zh-TW`, tiếng Anh).
const sampleVoices = <Map<String, String>>[
  {'name': 'en-us-x-sfg', 'locale': 'en-US'},
  {'name': 'Sin-ji', 'locale': 'zh-HK'},
  {'name': 'Mei-Jia', 'locale': 'zh-TW'},
  {'name': 'cmn-cn-x-ccc', 'locale': 'zh_CN'},
  {'name': 'Tingting (Enhanced)', 'locale': 'zh-CN', 'identifier': 'com.apple.voice.enhanced.zh-CN.Tingting'},
  {'name': 'Cantonese Female', 'locale': 'zh-CN'},
];
