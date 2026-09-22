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

// ─────────────────────────────────────────────────────────────────────────────
// SRS (M6) — port `features/srs/types.ts` web (hợp đồng F6/F7 §6.2). Backend serialize enum `snake_case` (`new`,
// `relearning`) và lược trường null (`WhenWritingNull`) ⇒ mọi trường có thể null đọc "dễ tính" qua `json_read`.
// ─────────────────────────────────────────────────────────────────────────────

/// Mức chấm thẻ (FSRS-6): 1 Quên · 2 Khó · 3 Được · 4 Dễ. [apiValue] là chuỗi gửi/nhận với server.
enum SrsRating {
  again('again'),
  hard('hard'),
  good('good'),
  easy('easy');

  const SrsRating(this.apiValue);

  final String apiValue;

  static SrsRating? fromApi(String? value) {
    for (final r in values) {
      if (r.apiValue == value) return r;
    }
    return null;
  }
}

/// Trạng thái thẻ theo FSRS (`new` là từ khoá Dart ⇒ đặt tên [fresh]).
enum SrsCardState {
  fresh('new'),
  learning('learning'),
  review('review'),
  relearning('relearning');

  const SrsCardState(this.apiValue);

  final String apiValue;

  /// Chuỗi lạ/thiếu ⇒ [review] (không làm thẻ biến mất — thẻ vẫn ôn được).
  static SrsCardState fromApi(String? value) {
    for (final s in values) {
      if (s.apiValue == value) return s;
    }
    return review;
  }
}

/// Tóm tắt SRS của người học (`GET /srs/summary`, cũng nằm trong hàng đợi và phản hồi chấm).
@immutable
class SrsSummary {
  const SrsSummary({
    required this.localDate,
    required this.timeZone,
    required this.dueToday,
    required this.dueNow,
    required this.reviewedToday,
    required this.reviewsDoneToday,
    required this.reviewLimitRemaining,
    required this.dailyReviewLimit,
    required this.newIntroducedToday,
    required this.newAvailableToday,
    required this.dailyNewCards,
    required this.totalCards,
    required this.matureCards,
    this.nextDueAt,
  });

  factory SrsSummary.fromJson(JsonMap? json) => SrsSummary(
    localDate: readStringOr(json, 'localDate'),
    timeZone: readStringOr(json, 'timeZone'),
    dueToday: readIntOr(json, 'dueToday'),
    dueNow: readIntOr(json, 'dueNow'),
    reviewedToday: readIntOr(json, 'reviewedToday'),
    reviewsDoneToday: readIntOr(json, 'reviewsDoneToday'),
    reviewLimitRemaining: readIntOr(json, 'reviewLimitRemaining'),
    dailyReviewLimit: readIntOr(json, 'dailyReviewLimit'),
    newIntroducedToday: readIntOr(json, 'newIntroducedToday'),
    newAvailableToday: readIntOr(json, 'newAvailableToday'),
    dailyNewCards: readIntOr(json, 'dailyNewCards'),
    totalCards: readIntOr(json, 'totalCards'),
    matureCards: readIntOr(json, 'matureCards'),
    nextDueAt: readDateTime(json, 'nextDueAt'),
  );

  /// Ngày địa phương (`yyyy-MM-dd`) theo [timeZone] — RM-L4: KHÔNG tự tính bằng đồng hồ máy.
  final String localDate;
  final String timeZone;
  final int dueToday;
  final int dueNow;

  /// Mọi lượt chấm hôm nay.
  final int reviewedToday;

  /// Lượt chấm hôm nay có `state_before = review` (tính vào giới hạn ôn/ngày).
  final int reviewsDoneToday;
  final int reviewLimitRemaining;
  final int dailyReviewLimit;
  final int newIntroducedToday;
  final int newAvailableToday;
  final int dailyNewCards;

  /// Thẻ khác `new`.
  final int totalCards;

  /// Thẻ `review` có `stability >= 21`.
  final int matureCards;

  /// `min(due_at)` của thẻ chưa đến hạn (UTC) — null khi không có.
  final DateTime? nextDueAt;

  /// Số thẻ có thể bắt đầu ôn ngay = huy hiệu nhánh "Ôn tập" (hợp đồng §5.3.8).
  int get toStart => dueNow + newAvailableToday;

  /// Đã chạm giới hạn lượt ôn/ngày mà vẫn còn thẻ đến hạn ⇒ banner như web.
  bool get reviewLimitReached => reviewLimitRemaining == 0 && dueToday > 0;

  @override
  bool operator ==(Object other) =>
      other is SrsSummary &&
      other.localDate == localDate &&
      other.timeZone == timeZone &&
      other.dueToday == dueToday &&
      other.dueNow == dueNow &&
      other.reviewedToday == reviewedToday &&
      other.reviewsDoneToday == reviewsDoneToday &&
      other.reviewLimitRemaining == reviewLimitRemaining &&
      other.dailyReviewLimit == dailyReviewLimit &&
      other.newIntroducedToday == newIntroducedToday &&
      other.newAvailableToday == newAvailableToday &&
      other.dailyNewCards == dailyNewCards &&
      other.totalCards == totalCards &&
      other.matureCards == matureCards &&
      other.nextDueAt == nextDueAt;

  @override
  int get hashCode => Object.hash(
    localDate,
    timeZone,
    dueToday,
    dueNow,
    reviewedToday,
    reviewsDoneToday,
    reviewLimitRemaining,
    dailyReviewLimit,
    newIntroducedToday,
    newAvailableToday,
    dailyNewCards,
    totalCards,
    matureCards,
    nextDueAt,
  );
}

/// Từ rút gọn kèm thẻ trong hàng đợi (`meaningsVi` tối đa 3).
@immutable
class SrsQueueWord {
  const SrsQueueWord({
    required this.id,
    required this.simplified,
    required this.pinyin,
    this.traditional,
    this.hanViet,
    this.meaningsVi = const [],
    this.meaningViStatus,
  });

  factory SrsQueueWord.fromJson(JsonMap? json) => SrsQueueWord(
    id: readStringOr(json, 'id'),
    simplified: readStringOr(json, 'simplified'),
    pinyin: readStringOr(json, 'pinyin'),
    traditional: readString(json, 'traditional'),
    hanViet: readString(json, 'hanViet'),
    meaningsVi: readPrimitiveList<String>(json, 'meaningsVi'),
    meaningViStatus: readString(json, 'meaningViStatus'),
  );

  final String id;
  final String simplified;

  /// Pinyin dạng SỐ (`ni3 hao3`) — hiển thị dấu qua `PinyinText`.
  final String pinyin;
  final String? traditional;
  final String? hanViet;
  final List<String> meaningsVi;

  /// `machine` | `reviewed` | null.
  final String? meaningViStatus;
}

/// Một thẻ trong hàng đợi (`GET /srs/queue`). [intervals] là ISO-8601 duration theo mức chấm (`PT10M`, `P8D`) —
/// định dạng qua `domain/format_interval.dart`; thiếu ⇒ null ⇒ nút hiện "—".
@immutable
class SrsQueueCard {
  const SrsQueueCard({
    required this.cardId,
    required this.state,
    required this.queue,
    required this.word,
    this.dueAt,
    this.intervals = const {},
  });

  factory SrsQueueCard.fromJson(JsonMap? json) {
    final rawIntervals = readMap(json, 'intervals');
    return SrsQueueCard(
      cardId: readStringOr(json, 'cardId'),
      state: SrsCardState.fromApi(readString(json, 'state')),
      queue: readStringOr(json, 'queue'),
      dueAt: readDateTime(json, 'dueAt'),
      word: SrsQueueWord.fromJson(readMap(json, 'word')),
      intervals: {for (final r in SrsRating.values) r: readString(rawIntervals, r.apiValue)},
    );
  }

  final String cardId;
  final SrsCardState state;

  /// Nhóm hàng đợi (R7-7): `learning` · `review` · `new` · `ahead`.
  final String queue;
  final DateTime? dueAt;
  final SrsQueueWord word;
  final Map<SrsRating, String?> intervals;

  /// Thẻ hợp lệ để ôn: phải có id thẻ và chữ.
  bool get isValid => cardId.isNotEmpty && word.simplified.isNotEmpty;
}

@immutable
class SrsQueueResponse {
  const SrsQueueResponse({required this.cards, required this.summary, this.generatedAt});

  /// Thẻ thiếu id/chữ bị bỏ (không làm phiên ôn vỡ vì một dòng lỗi).
  factory SrsQueueResponse.fromJson(JsonMap? json) => SrsQueueResponse(
    generatedAt: readDateTime(json, 'generatedAt'),
    cards: readList(json, 'cards', SrsQueueCard.fromJson).where((c) => c.isValid).toList(),
    summary: SrsSummary.fromJson(readMap(json, 'summary')),
  );

  final DateTime? generatedAt;
  final List<SrsQueueCard> cards;
  final SrsSummary summary;
}

/// Thẻ trần sau khi chấm / đổi tạm dừng (`SrsCardDto`).
@immutable
class SrsCardStatus {
  const SrsCardStatus({
    required this.cardId,
    required this.state,
    required this.isSuspended,
    this.dueAt,
    this.reps = 0,
    this.lapses = 0,
  });

  factory SrsCardStatus.fromJson(JsonMap? json) => SrsCardStatus(
    cardId: readStringOr(json, 'cardId'),
    state: SrsCardState.fromApi(readString(json, 'state')),
    dueAt: readDateTime(json, 'dueAt'),
    reps: readIntOr(json, 'reps'),
    lapses: readIntOr(json, 'lapses'),
    isSuspended: readBoolOr(json, 'isSuspended'),
  );

  final String cardId;
  final SrsCardState state;
  final DateTime? dueAt;
  final int reps;
  final int lapses;
  final bool isSuspended;
}

/// Phản hồi `POST /srs/cards/{id}/reviews`. [duplicate] `true` ⇒ trùng `clientReviewId` cùng thẻ: server trả kết
/// quả cũ, KHÔNG tạo log thứ hai (idempotent — R7-8).
@immutable
class ReviewResponse {
  const ReviewResponse({required this.reviewId, required this.duplicate, required this.card, required this.summary});

  factory ReviewResponse.fromJson(JsonMap? json) => ReviewResponse(
    reviewId: readStringOr(json, 'reviewId'),
    duplicate: readBoolOr(json, 'duplicate'),
    card: SrsCardStatus.fromJson(readMap(json, 'card')),
    summary: SrsSummary.fromJson(readMap(json, 'summary')),
  );

  final String reviewId;
  final bool duplicate;
  final SrsCardStatus card;
  final SrsSummary summary;
}

/// Phản hồi `POST /srs/cards` (201).
@immutable
class AddCardsResponse {
  const AddCardsResponse({required this.added, required this.skipped, this.cards = const []});

  factory AddCardsResponse.fromJson(JsonMap? json) => AddCardsResponse(
    added: readIntOr(json, 'added'),
    skipped: readIntOr(json, 'skipped'),
    cards: readList(
      json,
      'cards',
      (m) => AddedCard(
        wordId: readStringOr(m, 'wordId'),
        cardId: readStringOr(m, 'cardId'),
        created: readBoolOr(m, 'created'),
      ),
    ),
  );

  final int added;
  final int skipped;
  final List<AddedCard> cards;

  /// Thẻ của [wordId] vừa được TẠO (không phải đã có sẵn) — quyết định lời toast.
  bool createdFor(String wordId) {
    for (final c in cards) {
      if (c.wordId == wordId) return c.created;
    }
    return added > 0;
  }
}

@immutable
class AddedCard {
  const AddedCard({required this.wordId, required this.cardId, required this.created});

  final String wordId;
  final String cardId;
  final bool created;
}
