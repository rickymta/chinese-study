import 'dart:async';

import 'package:af_chinese/core/session_scope.dart';
import 'package:af_chinese/features/srs/application/providers.dart';
import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// `AfTts` giả cho widget test của app — ghi đè ba lời gọi công khai, không đụng plugin `flutter_tts` (app không khai
/// dependency đó; bản giả cấp plugin nằm ở `packages/af_ui/test/helpers`).
class FakeAfTts extends AfTts {
  FakeAfTts({this.voices = const [], this.unsupported = false}) : super(platform: TtsPlatform.web);

  List<TtsVoice> voices;
  bool unsupported;
  final spoken = <String>[];
  double? rate;
  TtsVoice? voice;
  int stops = 0;
  Completer<void>? speakGate;

  /// Khi đặt, `init` chờ tới khi completer hoàn thành (mô phỏng đang dò giọng — trạng thái `loading`).
  Completer<void>? initGate;

  @override
  Future<TtsProbe> init() async {
    if (initGate != null) await initGate!.future;
    if (unsupported) return const TtsProbe(status: SpeechStatus.unsupported);
    final list = AfTts.filterAndSort(voices, 'zh');
    return TtsProbe(status: list.isEmpty ? SpeechStatus.noVoice : SpeechStatus.ready, voices: list);
  }

  @override
  Future<void> speak(String text, {double rate = kTtsRateDefault, TtsVoice? voice}) async {
    spoken.add(text);
    this.rate = rate;
    this.voice = voice;
    if (speakGate != null) await speakGate!.future;
  }

  @override
  Future<void> stop() async {
    stops++;
  }
}

const zhVoices = <TtsVoice>[TtsVoice(name: 'Tingting', locale: 'zh-CN'), TtsVoice(name: 'Mei-Jia', locale: 'zh-TW')];

/// Bọc widget trong `ProviderScope` với TTS giả + kho trong bộ nhớ. [settings] = cài đặt học tập server trả
/// (nguồn `ttsRate`); null ⇒ server "chưa trả lời" (Future treo) ⇒ dùng cache/mặc định.
Widget speechTestApp(Widget child, {required FakeAfTts tts, KeyValueStore? store, LearningSettings? settings}) {
  return ProviderScope(
    overrides: [
      keyValueStoreProvider.overrideWithValue(store ?? InMemoryKeyValueStore()),
      afTtsProvider.overrideWithValue(tts),
      // Không có phiên đăng nhập trong test này ⇒ khoá cache/hộp chờ dùng nhánh "chưa đăng nhập".
      userScopeProvider.overrideWithValue(null),
      learningSettingsProvider.overrideWithBuild(
        (ref, notifier) => settings == null ? Completer<LearningSettings>().future : Future.value(settings),
      ),
    ],
    child: MaterialApp(
      theme: buildAfTheme(brightness: Brightness.light, accent: afAccentChinese),
      home: child,
    ),
  );
}
