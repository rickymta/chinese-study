import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

/// Cài đặt học tập của người học (`GET|PUT /me/learning-settings`, HĐ67 §6.2; port `LearningSettings` +
/// `LearningSettingsResponse` của `features/srs/types.ts`).
///
/// Luật server: `dailyNewCards` 0..50 · `dailyReviewLimit` 10..1000 · `desiredRetention` 0,80..0,97 · `ttsRate`
/// 0,5..1,2 (tối đa 2 chữ số thập phân) · `autoPlayAudio`. [isDefault] chỉ có khi GET: `true` ⇒ người học chưa lưu
/// cài đặt nào (đang dùng mặc định).
@immutable
class LearningSettings {
  const LearningSettings({
    required this.dailyNewCards,
    required this.dailyReviewLimit,
    required this.desiredRetention,
    required this.ttsRate,
    required this.autoPlayAudio,
    this.isDefault = false,
  });

  /// Thiếu khoá/sai kiểu ⇒ giá trị mặc định của server (`DEFAULT_LEARNING_SETTINGS` web), không ném.
  factory LearningSettings.fromJson(JsonMap? json) => LearningSettings(
    dailyNewCards: readIntOr(json, 'dailyNewCards', defaults.dailyNewCards),
    dailyReviewLimit: readIntOr(json, 'dailyReviewLimit', defaults.dailyReviewLimit),
    desiredRetention: readDoubleOr(json, 'desiredRetention', defaults.desiredRetention),
    ttsRate: readDoubleOr(json, 'ttsRate', defaults.ttsRate),
    autoPlayAudio: readBoolOr(json, 'autoPlayAudio', defaults.autoPlayAudio),
    isDefault: readBoolOr(json, 'isDefault'),
  );

  /// Mặc định của server (§6.2) — dùng khi chưa tải được để giao diện/TTS không trống.
  static const defaults = LearningSettings(
    dailyNewCards: 10,
    dailyReviewLimit: 200,
    desiredRetention: 0.9,
    ttsRate: 0.8,
    autoPlayAudio: true,
    isDefault: true,
  );

  final int dailyNewCards;
  final int dailyReviewLimit;
  final double desiredRetention;
  final double ttsRate;
  final bool autoPlayAudio;
  final bool isDefault;

  /// Thân `PUT` — đủ 5 trường, KHÔNG gửi `isDefault`; số thực làm tròn 2 chữ số (luật validator server).
  JsonMap toJson() => {
    'dailyNewCards': dailyNewCards,
    'dailyReviewLimit': dailyReviewLimit,
    'desiredRetention': round2(desiredRetention),
    'ttsRate': round2(ttsRate),
    'autoPlayAudio': autoPlayAudio,
  };

  static double round2(double v) => (v * 100).round() / 100;

  LearningSettings copyWith({
    int? dailyNewCards,
    int? dailyReviewLimit,
    double? desiredRetention,
    double? ttsRate,
    bool? autoPlayAudio,
    bool? isDefault,
  }) => LearningSettings(
    dailyNewCards: dailyNewCards ?? this.dailyNewCards,
    dailyReviewLimit: dailyReviewLimit ?? this.dailyReviewLimit,
    desiredRetention: desiredRetention ?? this.desiredRetention,
    ttsRate: ttsRate ?? this.ttsRate,
    autoPlayAudio: autoPlayAudio ?? this.autoPlayAudio,
    isDefault: isDefault ?? this.isDefault,
  );

  @override
  bool operator ==(Object other) =>
      other is LearningSettings &&
      other.dailyNewCards == dailyNewCards &&
      other.dailyReviewLimit == dailyReviewLimit &&
      other.desiredRetention == desiredRetention &&
      other.ttsRate == ttsRate &&
      other.autoPlayAudio == autoPlayAudio &&
      other.isDefault == isDefault;

  @override
  int get hashCode => Object.hash(dailyNewCards, dailyReviewLimit, desiredRetention, ttsRate, autoPlayAudio, isDefault);

  @override
  String toString() =>
      'LearningSettings(new=$dailyNewCards, limit=$dailyReviewLimit, retention=$desiredRetention, '
      'tts=$ttsRate, auto=$autoPlayAudio, default=$isDefault)';
}
