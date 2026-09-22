import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/key_value_store_provider.dart';
import 'af_tts.dart';

/// Tiền tố ngôn ngữ của giọng đọc (`zh` cho app tiếng Trung). App ngôn ngữ khác override (`ja`, `en`...).
final speechLangPrefixProvider = Provider<String>((ref) => 'zh');

/// Ngôn ngữ BCP-47 đặt cho engine khi đọc (`zh-CN`). App khác override cùng với [speechLangPrefixProvider].
final speechLanguageProvider = Provider<String>((ref) => 'zh-CN');

/// Bộ đọc — test override bằng `AfTts(tts: FakeFlutterTts(), platform: ...)`.
final afTtsProvider = Provider<AfTts>(
  (ref) => AfTts(langPrefix: ref.watch(speechLangPrefixProvider), language: ref.watch(speechLanguageProvider)),
);

/// Khoá `shared_preferences` lưu tên giọng đã chọn: `af.speech.voice.<prefix>` (hợp đồng §5.1.2).
String speechVoiceStorageKey(String langPrefix) => 'af.speech.voice.$langPrefix';

/// Trạng thái giọng đọc (tương đương `UseSpeechResult` web).
@immutable
class SpeechState {
  const SpeechState({this.status = SpeechStatus.loading, this.voices = const [], this.voice, this.speaking = false});

  final SpeechStatus status;

  /// Giọng khớp ngôn ngữ (đã lọc/sắp). Có thể rỗng khi `ready` (engine tự chọn giọng mặc định).
  final List<TtsVoice> voices;

  /// Giọng đang dùng (`null` ⇒ giọng mặc định engine / chưa có).
  final TtsVoice? voice;
  final bool speaking;

  /// Phát được (status `ready`).
  bool get canSpeak => status == SpeechStatus.ready;

  SpeechState copyWith({SpeechStatus? status, List<TtsVoice>? voices, TtsVoice? voice, bool? speaking}) => SpeechState(
    status: status ?? this.status,
    voices: voices ?? this.voices,
    voice: voice ?? this.voice,
    speaking: speaking ?? this.speaking,
  );

  @override
  bool operator ==(Object other) =>
      other is SpeechState &&
      other.status == status &&
      listEquals(other.voices, voices) &&
      other.voice == voice &&
      other.speaking == speaking;

  @override
  int get hashCode => Object.hash(status, Object.hashAll(voices), voice, speaking);
}

/// Giọng đọc dùng chung cho cả app (hợp đồng §5.3.7): dò giọng lúc khởi tạo, nhớ giọng đã chọn, đọc/huỷ, báo
/// `noVoice`/`unsupported` để màn hình hiện [VoiceMissingNotice].
///
/// Riverpod 3: `build()` đồng bộ ⇒ trả `loading` rồi dò engine ở nền. Tốc độ do app truyền vào mỗi lần `speak`
/// (M3 tạm lưu KeyValueStore; M4 lấy từ `learning-settings`).
class SpeechController extends Notifier<SpeechState> {
  int _speakSeq = 0;

  /// Người dùng đã chọn giọng trong phiên ⇒ kết quả nạp từ kho (về sau) không đè lên.
  bool _userPicked = false;

  @override
  SpeechState build() {
    unawaited(_init());
    return const SpeechState();
  }

  String get _storageKey => speechVoiceStorageKey(ref.read(speechLangPrefixProvider));

  Future<void> _init() async {
    final store = ref.read(keyValueStoreProvider);
    final tts = ref.read(afTtsProvider);
    final stored = await store.getString(_storageKey);
    final probe = await tts.init();
    if (!ref.mounted) return;
    final current = _userPicked ? state.voice : null;
    // Dựng mới (không copyWith) để `voice` về null được khi danh sách giọng trống.
    state = SpeechState(
      status: probe.status,
      voices: probe.voices,
      voice: pickVoice(probe.voices, current?.name ?? stored),
      speaking: state.speaking,
    );
  }

  /// Dò lại giọng (người dùng vừa cài gói giọng, Chrome nạp giọng muộn).
  Future<void> refreshVoices() async {
    state = state.copyWith(status: SpeechStatus.loading);
    await _init();
  }

  /// Chọn giọng theo tên người dùng đã lưu; không có/không còn ⇒ giọng đầu danh sách (đã sắp ưu tiên).
  static TtsVoice? pickVoice(List<TtsVoice> voices, String? preferredName) {
    if (voices.isEmpty) return null;
    if (preferredName != null) {
      for (final v in voices) {
        if (v.name == preferredName) return v;
      }
    }
    return voices.first;
  }

  /// Đặt giọng và lưu bền. Lưu lỗi ⇒ vẫn đổi trong phiên.
  Future<void> setVoice(TtsVoice voice) async {
    if (!ref.mounted) return;
    _userPicked = true;
    state = state.copyWith(voice: voice);
    await ref.read(keyValueStoreProvider).setString(_storageKey, voice.name);
  }

  /// Đọc [text] với giọng đang chọn và tốc độ [rate]; chưa `ready` ⇒ bỏ qua. Lỗi phát đã được [AfTts] nuốt.
  Future<void> speak(String text, {double rate = kTtsRateDefault}) async {
    if (!ref.mounted || !state.canSpeak) return;
    final seq = ++_speakSeq;
    state = state.copyWith(speaking: true);
    try {
      await ref.read(afTtsProvider).speak(text, rate: rate, voice: state.voice);
    } finally {
      // Lượt mới đã bắt đầu trong lúc chờ ⇒ không tắt cờ của lượt mới.
      if (ref.mounted && seq == _speakSeq) state = state.copyWith(speaking: false);
    }
  }

  /// Huỷ lượt đang đọc (rời màn, đổi thẻ).
  Future<void> cancel() async {
    // Gọi từ dispose của widget (hoãn microtask) ⇒ container có thể đã bị huỷ (tắt app, test) ⇒ bỏ qua.
    if (!ref.mounted) return;
    _speakSeq++;
    if (state.speaking) state = state.copyWith(speaking: false);
    await ref.read(afTtsProvider).stop();
  }
}

final speechControllerProvider = NotifierProvider<SpeechController, SpeechState>(SpeechController.new);
