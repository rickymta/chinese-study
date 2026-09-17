import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/srs/application/providers.dart';
import '../../features/srs/data/models.dart';
import '../session_scope.dart';

/// Tốc độ đọc TTS của người học (0,5–1,2; mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh), cùng hằng số với web
/// `useTtsRate.ts`.
const double kTtsRateMin = 0.5;
const double kTtsRateMax = 1.2;

/// Câu nghe thử (你好 — "xin chào") cho nút "Nghe thử" ở Hồ sơ (Học tập, Giao diện). Hiển thị qua `HanziBig`/`HanziText`.
const kSampleHanzi = '\u4F60\u597D';
const kSamplePinyin = 'ni3 hao3';

/// Tiền tố khoá CACHE cục bộ (web dùng `af.chinese.ttsRate` trong localStorage). Từ M4 nguồn sự thật là
/// `learner_settings.tts_rate` (`GET/PUT /me/learning-settings`); cache chỉ để lần mở sau có ngay giá trị trước khi
/// server trả lời. Khoá THEO NGƯỜI DÙNG (`af.chinese.ttsRate.<userId>`) để người B không nhận tốc độ của người A
/// trên cùng máy (review M4 C1).
const kTtsRateKey = 'af.chinese.ttsRate';

/// Khoá cache tốc độ đọc của một người dùng; chưa đăng nhập ⇒ khoá chung.
String ttsRateCacheKey(String? userId) => userId == null ? kTtsRateKey : '$kTtsRateKey.$userId';

/// Kẹp 0,5–1,2 và làm tròn 2 chữ số (luật `PUT /api/me/learning-settings`).
double clampTtsRate(double v) {
  final x = v.isFinite ? v : kTtsRateDefault;
  return (x.clamp(kTtsRateMin, kTtsRateMax) * 100).round() / 100;
}

/// Tốc độ đọc đang dùng (port `useTtsRate` web):
/// - server (`learningSettingsProvider`) có dữ liệu ⇒ dùng `ttsRate` của server và ghi cache;
/// - chưa/không tải được ⇒ cache `af.chinese.ttsRate`, rồi mặc định 0,8;
/// - [setRate] áp dụng tức thì + ghi cache; [persist] ghi máy chủ (gọi khi THẢ thanh trượt — không cần debounce như
///   web vì `onChangeEnd` chỉ bắn một lần); ghi hỏng thì im lặng — lần "Lưu" ở tab Học tập sẽ đồng bộ lại.
///
/// Riverpod 3 dựng lại Notifier mỗi khi `learningSettingsProvider` đổi ⇒ `build()` luôn phản ánh server mới nhất.
/// `unwrapPrevious()` bắt buộc: khi đổi người dùng, provider ở `AsyncLoading` vẫn giữ `.value` của người TRƯỚC
/// (Riverpod 3) — không được đọc/ghi giá trị đó cho người sau (review M4 C1).
class TtsRateController extends Notifier<double> {
  bool _userChanged = false;

  @override
  double build() {
    final settings = ref.watch(learningSettingsProvider).unwrapPrevious().value;
    final pending = ref.watch(_pendingTtsRateProvider);
    if (settings != null) {
      // Người dùng đã chọn trong lúc server chưa trả lời ⇒ giá trị đó thắng và được đẩy lên server (như `pendingRef`
      // web); ghi ở microtask vì đang trong build.
      final p = pending.value;
      if (p != null) {
        pending.value = null;
        final notifier = ref.read(learningSettingsProvider.notifier);
        unawaited(
          Future<void>.microtask(() => notifier.save(settings.copyWith(ttsRate: p))).then<void>(
            (_) {},
            onError: (Object e) => afLog('ttsRate: không ghi được giá trị chờ (${ApiError.from(e).message})'),
          ),
        );
        unawaited(_writeCache(p));
        return p;
      }
      final v = clampTtsRate(settings.ttsRate);
      unawaited(_writeCache(v));
      return v;
    }
    unawaited(_restoreCache());
    return kTtsRateDefault;
  }

  String get _cacheKey => ttsRateCacheKey(ref.read(userScopeProvider));

  Future<void> _restoreCache() async {
    final key = _cacheKey;
    final raw = await ref.read(keyValueStoreProvider).getString(key);
    if (!ref.mounted || _userChanged || raw == null) return;
    final parsed = double.tryParse(raw);
    if (parsed != null) state = clampTtsRate(parsed);
  }

  Future<void> _writeCache(double v) async {
    await ref.read(keyValueStoreProvider).setString(_cacheKey, v.toString());
  }

  /// Áp dụng tức thì + ghi cache (kéo thanh trượt). Chưa ghi máy chủ.
  Future<void> setRate(double rate) async {
    _userChanged = true;
    final v = clampTtsRate(rate);
    state = v;
    await _writeCache(v);
  }

  /// Ghi máy chủ giá trị [rate] (đã [setRate]). Cài đặt chưa tải được ⇒ ghi nhớ chờ (đẩy lên khi server trả lời);
  /// lỗi ghi ⇒ chỉ log (giữ cache).
  Future<void> persist(double rate) async {
    await setRate(rate);
    final v = clampTtsRate(rate);
    final current = ref.read(learningSettingsProvider).unwrapPrevious().value;
    if (current == null) {
      ref.read(_pendingTtsRateProvider).value = v;
      return;
    }
    if (current.ttsRate == v && !current.isDefault) return;
    try {
      await ref.read(learningSettingsProvider.notifier).save(current.copyWith(ttsRate: v));
    } on Object catch (e) {
      afLog('ttsRate: không ghi được máy chủ (${ApiError.from(e).message}) — giữ cache, tab Học tập sẽ đồng bộ');
    }
  }
}

final ttsRateProvider = NotifierProvider<TtsRateController, double>(TtsRateController.new);

/// Hộp giữ giá trị người dùng chọn khi server chưa trả lời — sống cùng container (Notifier bị dựng lại mỗi lần
/// `learningSettingsProvider` đổi nên không giữ được trong Notifier). Theo người dùng: đổi người ⇒ hộp mới (giá trị
/// chờ của A không bị đẩy vào tài khoản B).
class _PendingRate {
  double? value;
}

final _pendingTtsRateProvider = Provider<_PendingRate>((ref) {
  ref.watch(userScopeProvider);
  return _PendingRate();
});

/// Tự đọc khi thẻ hiện (cài đặt học `autoPlayAudio`, mặc định bật) — M6 dùng. `unwrapPrevious` để không lấy cài
/// đặt của người trước lúc đổi tài khoản.
final autoPlayAudioProvider = Provider<bool>(
  (ref) =>
      ref.watch(learningSettingsProvider).unwrapPrevious().value?.autoPlayAudio ??
      LearningSettings.defaults.autoPlayAudio,
);

/// Đọc chữ Hán bằng giọng + tốc độ đã chọn (tương đương `speakZh` của `ChineseSpeechProvider` web). [rate] ghi đè
/// tốc độ hiện tại (nút "Nghe thử" đọc theo giá trị đang kéo). Gọi TRONG handler thao tác người dùng.
Future<void> speakZh(WidgetRef ref, String text, {double? rate}) =>
    ref.read(speechControllerProvider.notifier).speak(text, rate: rate ?? ref.read(ttsRateProvider));
