import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

// Kiểu dữ liệu bài học + quiz theo hợp đồng F8–F11 §6.1 (port `features/lessons/types.ts` web). Serializer backend bật
// `WhenWritingNull` ⇒ trường null bị LƯỢC khỏi JSON ⇒ đọc "dễ tính" (thiếu = null/mặc định, RK41). Pinyin luôn dạng SỐ
// THANH (`ni3 hao3`) — hiển thị dạng dấu qua `core/pinyin`.

/// Ngưỡng hoàn thành bài (R-LS3) — server là nguồn sự thật (`passThresholdPercent`), đây chỉ để hiện trước khi nộp.
const kPassThresholdPercent = 80;

/// `machine`: bài do agent soạn/nhập, chưa người duyệt (R-LS2) ⇒ chip "Nội dung chưa được duyệt".
const kReviewStatusMachine = 'machine';

/// Tiến độ của người học trên một bài — VẮNG khi chưa bắt đầu (R-LS12).
@immutable
class LessonProgress {
  const LessonProgress({
    required this.status,
    this.bestScorePercent,
    this.attemptsCount = 0,
    this.startedAt,
    this.lastAttemptAt,
    this.completedAt,
  });

  factory LessonProgress.fromJson(JsonMap json) => LessonProgress(
    status: readStringOr(json, 'status', 'in_progress'),
    bestScorePercent: readInt(json, 'bestScorePercent'),
    attemptsCount: readIntOr(json, 'attemptsCount'),
    startedAt: readDateTime(json, 'startedAt'),
    lastAttemptAt: readDateTime(json, 'lastAttemptAt'),
    completedAt: readDateTime(json, 'completedAt'),
  );

  /// `in_progress` | `completed`.
  final String status;

  /// Điểm cao nhất (%) trong các lần nộp; null khi chưa nộp lần nào.
  final int? bestScorePercent;
  final int attemptsCount;
  final DateTime? startedAt;
  final DateTime? lastAttemptAt;
  final DateTime? completedAt;

  bool get isCompleted => status == 'completed';

  @override
  bool operator ==(Object other) =>
      other is LessonProgress &&
      other.status == status &&
      other.bestScorePercent == bestScorePercent &&
      other.attemptsCount == attemptsCount &&
      other.startedAt == startedAt &&
      other.lastAttemptAt == lastAttemptAt &&
      other.completedAt == completedAt;

  @override
  int get hashCode => Object.hash(status, bestScorePercent, attemptsCount, startedAt, lastAttemptAt, completedAt);
}

/// Một dòng `GET /lessons`.
@immutable
class LessonSummary {
  const LessonSummary({
    required this.id,
    required this.slug,
    required this.title,
    this.topic = '',
    this.orderIndex = 0,
    this.summary,
    this.estimatedMinutes = 0,
    this.wordCount = 0,
    this.questionCount = 0,
    this.reviewStatus = 'reviewed',
    this.progress,
  });

  /// Dòng thiếu `id`/`slug` bị bỏ (không mở được).
  static LessonSummary? fromJson(JsonMap json) {
    final id = readString(json, 'id');
    final slug = readString(json, 'slug');
    if (id == null || id.isEmpty || slug == null || slug.isEmpty) return null;
    final progress = readMap(json, 'progress');
    return LessonSummary(
      id: id,
      slug: slug,
      title: readStringOr(json, 'title'),
      topic: readStringOr(json, 'topic'),
      orderIndex: readIntOr(json, 'orderIndex'),
      summary: readString(json, 'summary'),
      estimatedMinutes: readIntOr(json, 'estimatedMinutes'),
      wordCount: readIntOr(json, 'wordCount'),
      questionCount: readIntOr(json, 'questionCount'),
      reviewStatus: readStringOr(json, 'reviewStatus', 'reviewed'),
      progress: progress == null ? null : LessonProgress.fromJson(progress),
    );
  }

  final String id;
  final String slug;
  final String title;
  final String topic;
  final int orderIndex;
  final String? summary;
  final int estimatedMinutes;
  final int wordCount;
  final int questionCount;
  final String reviewStatus;
  final LessonProgress? progress;

  bool get isUnreviewed => reviewStatus == kReviewStatusMachine;
}

@immutable
class LessonListResponse {
  const LessonListResponse({this.items = const [], this.nextLessonSlug});

  factory LessonListResponse.fromJson(JsonMap? json) => LessonListResponse(
    items: readList(json, 'items', LessonSummary.fromJson),
    nextLessonSlug: readString(json, 'nextLessonSlug'),
  );

  final List<LessonSummary> items;

  /// Bài `published` có `orderIndex` nhỏ nhất chưa `completed` (R-LS4); null khi đã xong hết.
  final String? nextLessonSlug;

  /// Sắp theo `orderIndex` rồi tiêu đề (như web).
  List<LessonSummary> get sorted {
    final out = [...items];
    out.sort((a, b) {
      final c = a.orderIndex.compareTo(b.orderIndex);
      return c != 0 ? c : a.title.compareTo(b.title);
    });
    return out;
  }
}

// ─── Khối nội dung (§5.4.3) ───

/// Một dòng hội thoại / ví dụ ngữ pháp: chữ Hán + pinyin số thanh + nghĩa Việt.
@immutable
class ZhLine {
  const ZhLine({this.speaker, required this.hanzi, this.pinyin = '', this.vi = '', this.note});

  /// Dòng thiếu `hanzi` bị bỏ.
  static ZhLine? fromJson(JsonMap json) {
    final hanzi = readString(json, 'hanzi');
    if (hanzi == null || hanzi.isEmpty) return null;
    return ZhLine(
      speaker: readString(json, 'speaker'),
      hanzi: hanzi,
      pinyin: readStringOr(json, 'pinyin'),
      vi: readStringOr(json, 'vi'),
      note: readString(json, 'note'),
    );
  }

  final String? speaker;
  final String hanzi;
  final String pinyin;
  final String vi;

  /// Chỉ ở ví dụ ngữ pháp.
  final String? note;
}

/// Khối nội dung bài — sealed: kiểu lạ (backend thêm sau) thành [UnknownBlock], không crash.
sealed class LessonBlock {
  const LessonBlock({required this.id, required this.type});

  final String id;
  final String type;

  static LessonBlock fromJson(JsonMap json) {
    final id = readStringOr(json, 'id');
    final type = readStringOr(json, 'type');
    final payload = readMap(json, 'payload');
    return switch (type) {
      'text' => TextBlock(id: id, paragraphs: readPrimitiveList<String>(payload, 'paragraphs')),
      'dialogue' => DialogueBlock(
        id: id,
        title: readString(payload, 'title'),
        lines: readList(payload, 'lines', ZhLine.fromJson),
      ),
      'grammar' => GrammarBlock(
        id: id,
        title: readStringOr(payload, 'title'),
        pattern: readString(payload, 'pattern'),
        explanation: readStringOr(payload, 'explanation'),
        examples: readList(payload, 'examples', ZhLine.fromJson),
      ),
      'tip' => TipBlock(id: id, variant: readString(payload, 'variant'), text: readStringOr(payload, 'text')),
      _ => UnknownBlock(id: id, type: type),
    };
  }
}

/// Mỗi đoạn có thể chứa token nội dòng `[[chữ Hán|pinyin]]` (parse bằng `domain/inline_zh.dart`).
class TextBlock extends LessonBlock {
  const TextBlock({required super.id, required this.paragraphs}) : super(type: 'text');

  final List<String> paragraphs;
}

class DialogueBlock extends LessonBlock {
  const DialogueBlock({required super.id, this.title, required this.lines}) : super(type: 'dialogue');

  final String? title;
  final List<ZhLine> lines;
}

class GrammarBlock extends LessonBlock {
  const GrammarBlock({
    required super.id,
    required this.title,
    this.pattern,
    required this.explanation,
    required this.examples,
  }) : super(type: 'grammar');

  final String title;
  final String? pattern;
  final String explanation;
  final List<ZhLine> examples;
}

/// `variant`: `pronunciation` | `culture` | `memory` | `grammar` | null.
class TipBlock extends LessonBlock {
  const TipBlock({required super.id, this.variant, required this.text}) : super(type: 'tip');

  final String? variant;
  final String text;
}

class UnknownBlock extends LessonBlock {
  const UnknownBlock({required super.id, required super.type});
}

/// Từ bổ sung của bài (tên riêng, địa danh...) — KHÔNG vào ôn tập.
@immutable
class GlossaryEntry {
  const GlossaryEntry({required this.hanzi, this.pinyin = '', this.vi = ''});

  static GlossaryEntry? fromJson(JsonMap json) {
    final hanzi = readString(json, 'hanzi');
    if (hanzi == null || hanzi.isEmpty) return null;
    return GlossaryEntry(hanzi: hanzi, pinyin: readStringOr(json, 'pinyin'), vi: readStringOr(json, 'vi'));
  }

  final String hanzi;
  final String pinyin;
  final String vi;
}

/// Từ của bài (thuộc `content.words`) — hoàn thành bài ⇒ thành thẻ SRS (R-LS5).
@immutable
class LessonWord {
  const LessonWord({
    required this.id,
    required this.simplified,
    this.traditional,
    this.pinyin = '',
    this.hanViet,
    this.meaningsVi = const [],
    this.meaningViStatus,
    this.inSrs = false,
  });

  static LessonWord? fromJson(JsonMap json) {
    final id = readString(json, 'id');
    final simplified = readString(json, 'simplified');
    if (id == null || id.isEmpty || simplified == null || simplified.isEmpty) return null;
    return LessonWord(
      id: id,
      simplified: simplified,
      traditional: readString(json, 'traditional'),
      pinyin: readStringOr(json, 'pinyin'),
      hanViet: readString(json, 'hanViet'),
      meaningsVi: readPrimitiveList<String>(json, 'meaningsVi'),
      meaningViStatus: readString(json, 'meaningViStatus'),
      inSrs: readBoolOr(json, 'inSrs'),
    );
  }

  final String id;
  final String simplified;
  final String? traditional;
  final String pinyin;
  final String? hanViet;
  final List<String> meaningsVi;

  /// `machine` | `reviewed` | null.
  final String? meaningViStatus;

  /// `true` ⇒ người học đã có thẻ SRS cho từ này (bất kể nguồn).
  final bool inSrs;
}

// ─── Quiz ───

/// `lang`: `vi` | `zh` | `pinyin`.
@immutable
class QuizOption {
  const QuizOption({required this.id, required this.text, this.lang = 'vi'});

  static QuizOption? fromJson(JsonMap json) {
    final id = readString(json, 'id');
    if (id == null || id.isEmpty) return null;
    return QuizOption(id: id, text: readStringOr(json, 'text'), lang: readStringOr(json, 'lang', 'vi'));
  }

  final String id;
  final String text;
  final String lang;

  @override
  bool operator ==(Object other) => other is QuizOption && other.id == id && other.text == text && other.lang == lang;

  @override
  int get hashCode => Object.hash(id, text, lang);
}

/// Câu hỏi cho học viên — KHÔNG có `correctOptionId`/`explanation` (R-LS10).
@immutable
class QuizQuestion {
  const QuizQuestion({
    required this.id,
    this.type = 'single_choice',
    this.prompt = '',
    this.promptLang = 'vi',
    this.promptPinyin,
    this.audioText,
    this.audioPinyin,
    this.options = const [],
  });

  static QuizQuestion? fromJson(JsonMap json) {
    final id = readString(json, 'id');
    if (id == null || id.isEmpty) return null;
    return QuizQuestion(
      id: id,
      type: readStringOr(json, 'type', 'single_choice'),
      prompt: readStringOr(json, 'prompt'),
      promptLang: readStringOr(json, 'promptLang', 'vi'),
      promptPinyin: readString(json, 'promptPinyin'),
      audioText: readString(json, 'audioText'),
      audioPinyin: readString(json, 'audioPinyin'),
      options: readList(json, 'options', QuizOption.fromJson),
    );
  }

  final String id;

  /// `listen_choice` | `single_choice`.
  final String type;
  final String prompt;

  /// `vi` | `zh`.
  final String promptLang;

  /// Chỉ khi `promptLang = 'zh'`.
  final String? promptPinyin;

  /// Chỉ ở `listen_choice`: chữ Hán để đọc TTS.
  final String? audioText;

  /// Không có trong hợp đồng §6.1 — khai phòng backend bổ sung; có thì hiện kèm khi "Hiện chữ".
  final String? audioPinyin;
  final List<QuizOption> options;

  bool get isListen => type == 'listen_choice' && (audioText?.isNotEmpty ?? false);

  QuizOption? optionById(String id) {
    for (final o in options) {
      if (o.id == id) return o;
    }
    return null;
  }
}

/// `GET /lessons/{slug}` — chi tiết bài (quiz KHÔNG có đáp án).
@immutable
class LessonDetail {
  const LessonDetail({
    required this.id,
    required this.slug,
    required this.title,
    this.topic = '',
    this.orderIndex = 0,
    this.summary,
    this.objectives = const [],
    this.estimatedMinutes = 0,
    this.reviewStatus = 'reviewed',
    this.glossary = const [],
    this.blocks = const [],
    this.words = const [],
    this.quiz = const [],
    this.progress,
  });

  factory LessonDetail.fromJson(JsonMap? json) {
    final progress = readMap(json, 'progress');
    return LessonDetail(
      id: readStringOr(json, 'id'),
      slug: readStringOr(json, 'slug'),
      title: readStringOr(json, 'title'),
      topic: readStringOr(json, 'topic'),
      orderIndex: readIntOr(json, 'orderIndex'),
      summary: readString(json, 'summary'),
      objectives: readPrimitiveList<String>(json, 'objectives'),
      estimatedMinutes: readIntOr(json, 'estimatedMinutes'),
      reviewStatus: readStringOr(json, 'reviewStatus', 'reviewed'),
      glossary: readList(json, 'glossary', GlossaryEntry.fromJson),
      blocks: readList(json, 'blocks', LessonBlock.fromJson),
      words: readList(json, 'words', LessonWord.fromJson),
      quiz: readList(json, 'quiz', QuizQuestion.fromJson),
      progress: progress == null ? null : LessonProgress.fromJson(progress),
    );
  }

  final String id;
  final String slug;
  final String title;
  final String topic;
  final int orderIndex;
  final String? summary;
  final List<String> objectives;
  final int estimatedMinutes;
  final String reviewStatus;
  final List<GlossaryEntry> glossary;
  final List<LessonBlock> blocks;
  final List<LessonWord> words;
  final List<QuizQuestion> quiz;
  final LessonProgress? progress;

  bool get isUnreviewed => reviewStatus == kReviewStatusMachine;

  LessonDetail copyWith({LessonProgress? progress}) => LessonDetail(
    id: id,
    slug: slug,
    title: title,
    topic: topic,
    orderIndex: orderIndex,
    summary: summary,
    objectives: objectives,
    estimatedMinutes: estimatedMinutes,
    reviewStatus: reviewStatus,
    glossary: glossary,
    blocks: blocks,
    words: words,
    quiz: quiz,
    progress: progress ?? this.progress,
  );
}

/// Một câu trả lời gửi lên.
@immutable
class QuizAnswer {
  const QuizAnswer({required this.questionId, required this.optionId});

  final String questionId;
  final String optionId;

  Map<String, Object?> toJson() => {'questionId': questionId, 'optionId': optionId};

  @override
  bool operator ==(Object other) => other is QuizAnswer && other.questionId == questionId && other.optionId == optionId;

  @override
  int get hashCode => Object.hash(questionId, optionId);

  @override
  String toString() => 'QuizAnswer($questionId → $optionId)';
}

/// `POST /lessons/{id}/quiz-attempts` — `clientAttemptId` sinh bằng `uuidV4()` LÚC BẮT ĐẦU lượt; gửi lại cùng id ⇒
/// server trả kết quả cũ (R-LS9). `startedAt` ISO-8601 UTC (có `Z` — D38).
@immutable
class SubmitQuizRequest {
  const SubmitQuizRequest({required this.clientAttemptId, required this.startedAt, required this.answers});

  final String clientAttemptId;
  final DateTime startedAt;
  final List<QuizAnswer> answers;

  Map<String, Object?> toJson() => {
    'clientAttemptId': clientAttemptId,
    'startedAt': startedAt.toUtc().toIso8601String(),
    'answers': [for (final a in answers) a.toJson()],
  };
}

/// Kết quả một câu sau khi chấm — `explanation` có thể chứa token `[[chữ Hán|pinyin]]`.
@immutable
class QuizQuestionResult {
  const QuizQuestionResult({
    required this.questionId,
    required this.optionId,
    required this.correct,
    required this.correctOptionId,
    this.explanation,
  });

  static QuizQuestionResult? fromJson(JsonMap json) {
    final questionId = readString(json, 'questionId');
    if (questionId == null || questionId.isEmpty) return null;
    return QuizQuestionResult(
      questionId: questionId,
      optionId: readStringOr(json, 'optionId'),
      correct: readBoolOr(json, 'correct'),
      correctOptionId: readStringOr(json, 'correctOptionId'),
      explanation: readString(json, 'explanation'),
    );
  }

  final String questionId;
  final String optionId;
  final bool correct;
  final String correctOptionId;
  final String? explanation;
}

/// Phản hồi nộp quiz — 201 lần đầu, 200 khi phát lại cùng `clientAttemptId`.
@immutable
class QuizResult {
  const QuizResult({
    required this.attemptId,
    this.submittedAt,
    required this.total,
    required this.correct,
    required this.scorePercent,
    required this.passed,
    this.passThresholdPercent = kPassThresholdPercent,
    this.firstCompletion = false,
    this.srsCardsAdded = 0,
    this.results = const [],
    this.progress,
  });

  factory QuizResult.fromJson(JsonMap? json) {
    final progress = readMap(json, 'progress');
    return QuizResult(
      attemptId: readStringOr(json, 'attemptId'),
      submittedAt: readDateTime(json, 'submittedAt'),
      total: readIntOr(json, 'total'),
      correct: readIntOr(json, 'correct'),
      scorePercent: readIntOr(json, 'scorePercent'),
      passed: readBoolOr(json, 'passed'),
      passThresholdPercent: readIntOr(json, 'passThresholdPercent', kPassThresholdPercent),
      firstCompletion: readBoolOr(json, 'firstCompletion'),
      srsCardsAdded: readIntOr(json, 'srsCardsAdded'),
      results: readList(json, 'results', QuizQuestionResult.fromJson),
      progress: progress == null ? null : LessonProgress.fromJson(progress),
    );
  }

  final String attemptId;
  final DateTime? submittedAt;
  final int total;
  final int correct;

  /// `floor(correct * 100 / total)` (R-LS3).
  final int scorePercent;
  final bool passed;
  final int passThresholdPercent;

  /// `true` chỉ ở lần nộp đầu tiên đạt ngưỡng (kể cả khi phát lại cùng `clientAttemptId`).
  final bool firstCompletion;

  /// Số thẻ SRS mới thêm — phát lại ⇒ 0; chỉ hiện "Đã thêm N từ" khi > 0.
  final int srsCardsAdded;
  final List<QuizQuestionResult> results;
  final LessonProgress? progress;
}

/// Một dòng lịch sử lần làm (`GET /lessons/{id}/quiz-attempts`) — không kèm chi tiết câu.
@immutable
class QuizAttemptSummary {
  const QuizAttemptSummary({
    required this.attemptId,
    this.submittedAt,
    required this.total,
    required this.correct,
    required this.scorePercent,
    required this.passed,
    this.durationMs,
  });

  static QuizAttemptSummary? fromJson(JsonMap json) {
    final attemptId = readString(json, 'attemptId');
    if (attemptId == null || attemptId.isEmpty) return null;
    return QuizAttemptSummary(
      attemptId: attemptId,
      submittedAt: readDateTime(json, 'submittedAt'),
      total: readIntOr(json, 'total'),
      correct: readIntOr(json, 'correct'),
      scorePercent: readIntOr(json, 'scorePercent'),
      passed: readBoolOr(json, 'passed'),
      durationMs: readInt(json, 'durationMs'),
    );
  }

  final String attemptId;
  final DateTime? submittedAt;
  final int total;
  final int correct;
  final int scorePercent;
  final bool passed;
  final int? durationMs;
}

@immutable
class QuizAttemptsResponse {
  const QuizAttemptsResponse({this.items = const []});

  factory QuizAttemptsResponse.fromJson(JsonMap? json) =>
      QuizAttemptsResponse(items: readList(json, 'items', QuizAttemptSummary.fromJson));

  final List<QuizAttemptSummary> items;
}
