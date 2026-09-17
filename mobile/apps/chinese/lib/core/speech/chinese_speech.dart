import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Tốc độ đọc TTS của người học (0,5–1,2; mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh), cùng hằng số với web
/// `useTtsRate.ts`.
const double kTtsRateMin = 0.5;
const double kTtsRateMax = 1.2;

/// Khoá cache cục bộ (cùng tên với `localStorage` của web). M3: đây là nguồn duy nhất; M4 chuyển nguồn sự thật sang
/// `learner_settings.tts_rate` (`GET/PUT /me/learning-settings`), khoá này chỉ còn là cache.
const kTtsRateKey = 'af.chinese.ttsRate';

/// Kẹp 0,5–1,2 và làm tròn 2 chữ số (luật `PUT /api/me/learning-settings`).
double clampTtsRate(double v) {
  final x = v.isFinite ? v : kTtsRateDefault;
  return (x.clamp(kTtsRateMin, kTtsRateMax) * 100).round() / 100;
}

/// Tốc độ đọc, lưu bền trong `KeyValueStore` (tạm — xem [kTtsRateKey]).
class TtsRateController extends Notifier<double> {
  bool _userChanged = false;

  @override
  double build() {
    unawaited(_restore());
    return kTtsRateDefault;
  }

  Future<void> _restore() async {
    final raw = await ref.read(keyValueStoreProvider).getString(kTtsRateKey);
    if (!ref.mounted || _userChanged || raw == null) return;
    final parsed = double.tryParse(raw);
    if (parsed != null) state = clampTtsRate(parsed);
  }

  /// Áp dụng tức thì, ghi kho sau; ghi hỏng thì im lặng (chỉ mất ghi nhớ).
  Future<void> setRate(double rate) async {
    _userChanged = true;
    final v = clampTtsRate(rate);
    state = v;
    await ref.read(keyValueStoreProvider).setString(kTtsRateKey, v.toString());
  }
}

final ttsRateProvider = NotifierProvider<TtsRateController, double>(TtsRateController.new);

/// Đọc chữ Hán bằng giọng + tốc độ đã chọn (tương đương `speakZh` của `ChineseSpeechProvider` web). [rate] ghi đè
/// tốc độ đã lưu (nút "Nghe thử" đọc theo giá trị đang kéo). Gọi TRONG handler thao tác người dùng.
Future<void> speakZh(WidgetRef ref, String text, {double? rate}) =>
    ref.read(speechControllerProvider.notifier).speak(text, rate: rate ?? ref.read(ttsRateProvider));
