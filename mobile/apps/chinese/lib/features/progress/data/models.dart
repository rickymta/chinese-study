/// Kiểu dữ liệu F11 theo hợp đồng chi tiết F8–F11 §6.4 (chinese-backend `GET /api/progress/overview`) — port
/// `features/progress/types.ts`. Serializer backend bật `WhenWritingNull` ⇒ khối/trường `null` bị LƯỢC khỏi JSON:
/// thiếu khoá coi như vắng, không ném (`json_read`). Mọi "ngày" (`localDate`, `activity[].date`) là `yyyy-MM-dd`
/// theo MÚI GIỜ HỒ SƠ của người học (R-PG2) — app KHÔNG tự tính "hôm nay" bằng đồng hồ máy.
library;

import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';

/// Chuỗi ngày học (R-PG3): hôm nay chưa học thì đếm từ hôm qua — chưa đứt tới hết ngày.
@immutable
class ProgressStreak {
  const ProgressStreak({required this.current, required this.longest, required this.studiedToday});

  factory ProgressStreak.fromJson(JsonMap? json) => ProgressStreak(
    current: readIntOr(json, 'current'),
    longest: readIntOr(json, 'longest'),
    studiedToday: readBoolOr(json, 'studiedToday'),
  );

  final int current;

  /// Chuỗi dài nhất trong lịch sử, luôn ≥ [current].
  final int longest;
  final bool studiedToday;

  @override
  bool operator ==(Object other) =>
      other is ProgressStreak &&
      other.current == current &&
      other.longest == longest &&
      other.studiedToday == studiedToday;

  @override
  int get hashCode => Object.hash(current, longest, studiedToday);
}

/// Hoạt động hôm nay (theo `kind` của sổ hoạt động).
@immutable
class ProgressToday {
  const ProgressToday({
    this.srsReviews = 0,
    this.newCards = 0,
    this.toneDrillItems = 0,
    this.writingAttempts = 0,
    this.quizzes = 0,
    this.lessonsCompleted = 0,
    this.activityCount = 0,
  });

  factory ProgressToday.fromJson(JsonMap? json) => ProgressToday(
    srsReviews: readIntOr(json, 'srsReviews'),
    newCards: readIntOr(json, 'newCards'),
    toneDrillItems: readIntOr(json, 'toneDrillItems'),
    writingAttempts: readIntOr(json, 'writingAttempts'),
    quizzes: readIntOr(json, 'quizzes'),
    lessonsCompleted: readIntOr(json, 'lessonsCompleted'),
    activityCount: readIntOr(json, 'activityCount'),
  );

  final int srsReviews;
  final int newCards;
  final int toneDrillItems;
  final int writingAttempts;
  final int quizzes;
  final int lessonsCompleted;

  /// Tổng `quantity` mọi loại hôm nay.
  final int activityCount;
}

/// Trích từ `GET /api/srs/summary` (K13).
@immutable
class ProgressSrs {
  const ProgressSrs({
    required this.dueToday,
    required this.dueNow,
    required this.newAvailableToday,
    required this.newIntroducedToday,
    required this.reviewedToday,
    this.nextDueAt,
  });

  factory ProgressSrs.fromJson(JsonMap? json) => ProgressSrs(
    dueToday: readIntOr(json, 'dueToday'),
    dueNow: readIntOr(json, 'dueNow'),
    newAvailableToday: readIntOr(json, 'newAvailableToday'),
    newIntroducedToday: readIntOr(json, 'newIntroducedToday'),
    reviewedToday: readIntOr(json, 'reviewedToday'),
    nextDueAt: readDateTime(json, 'nextDueAt'),
  );

  final int dueToday;
  final int dueNow;
  final int newAvailableToday;
  final int newIntroducedToday;
  final int reviewedToday;
  final DateTime? nextDueAt;

  /// Số hiện trên huy hiệu nhánh "Ôn tập" (hợp đồng §5.3.8): thẻ đến hạn LÚC NÀY + thẻ mới còn được học hôm nay.
  int get reviewBadge => dueNow + newAvailableToday;
}

/// Mục tiêu ngày (R-PG5) — backend tính; vắng khi `srs` vắng.
@immutable
class ProgressDailyGoal {
  const ProgressDailyGoal({required this.done, required this.total, required this.achieved});

  factory ProgressDailyGoal.fromJson(JsonMap? json) => ProgressDailyGoal(
    done: readIntOr(json, 'done'),
    total: readIntOr(json, 'total'),
    achieved: readBoolOr(json, 'achieved'),
  );

  final int done;
  final int total;
  final bool achieved;
}

@immutable
class ProgressVocabulary {
  const ProgressVocabulary({
    required this.totalInPath,
    required this.introduced,
    required this.learning,
    required this.mature,
  });

  factory ProgressVocabulary.fromJson(JsonMap? json) => ProgressVocabulary(
    totalInPath: readIntOr(json, 'totalInPath'),
    introduced: readIntOr(json, 'introduced'),
    learning: readIntOr(json, 'learning'),
    mature: readIntOr(json, 'mature'),
  );

  /// Số từ trong lộ trình (`path_order IS NOT NULL`).
  final int totalInPath;

  /// Thẻ đã được giới thiệu (đã ôn ít nhất một lần).
  final int introduced;
  final int learning;

  /// Thẻ `review` có `stability ≥ 21`.
  final int mature;
}

@immutable
class ProgressLessonRef {
  const ProgressLessonRef({required this.slug, required this.title});

  /// Thiếu `slug` ⇒ `null` (không điều hướng được thì coi như vắng).
  static ProgressLessonRef? fromJson(JsonMap? json) {
    final slug = readString(json, 'slug');
    if (slug == null || slug.isEmpty) return null;
    return ProgressLessonRef(slug: slug, title: readStringOr(json, 'title', slug));
  }

  final String slug;
  final String title;
}

@immutable
class ProgressLastCompletedLesson {
  const ProgressLastCompletedLesson({
    required this.slug,
    required this.title,
    this.completedAt,
    required this.unpracticedChars,
  });

  static ProgressLastCompletedLesson? fromJson(JsonMap? json) {
    final slug = readString(json, 'slug');
    if (slug == null || slug.isEmpty) return null;
    return ProgressLastCompletedLesson(
      slug: slug,
      title: readStringOr(json, 'title', slug),
      completedAt: readDateTime(json, 'completedAt'),
      unpracticedChars: readIntOr(json, 'unpracticedChars'),
    );
  }

  final String slug;
  final String title;
  final DateTime? completedAt;

  /// Số chữ của bài chưa từng luyện viết (R-PG9 mục 4).
  final int unpracticedChars;
}

@immutable
class ProgressLessons {
  const ProgressLessons({
    required this.published,
    required this.completed,
    required this.inProgress,
    this.next,
    this.lastCompleted,
  });

  factory ProgressLessons.fromJson(JsonMap? json) => ProgressLessons(
    published: readIntOr(json, 'published'),
    completed: readIntOr(json, 'completed'),
    inProgress: readIntOr(json, 'inProgress'),
    next: ProgressLessonRef.fromJson(readMap(json, 'next')),
    lastCompleted: ProgressLastCompletedLesson.fromJson(readMap(json, 'lastCompleted')),
  );

  final int published;
  final int completed;
  final int inProgress;

  /// Bài tiếp theo (R-LS4) — vắng khi đã học hết.
  final ProgressLessonRef? next;

  /// Bài hoàn thành gần nhất — vắng khi chưa hoàn thành bài nào.
  final ProgressLastCompletedLesson? lastCompleted;
}

@immutable
class ProgressWriting {
  const ProgressWriting({
    required this.practicedChars,
    required this.masteredChars,
    required this.weakChars,
    required this.totalChars,
  });

  factory ProgressWriting.fromJson(JsonMap? json) => ProgressWriting(
    practicedChars: readIntOr(json, 'practicedChars'),
    masteredChars: readIntOr(json, 'masteredChars'),
    weakChars: readIntOr(json, 'weakChars'),
    totalChars: readIntOr(json, 'totalChars'),
  );

  final int practicedChars;
  final int masteredChars;
  final int weakChars;

  /// Kích thước bộ `hsk1`.
  final int totalChars;
}

@immutable
class ProgressTone {
  const ProgressTone({required this.totalAnswered, this.accuracy, this.recommendedFocus = const []});

  factory ProgressTone.fromJson(JsonMap? json) => ProgressTone(
    totalAnswered: readIntOr(json, 'totalAnswered'),
    accuracy: readDouble(json, 'accuracy'),
    // Số nguyên trong JSON có thể về `int` hoặc `double` tuỳ nguồn ⇒ nhận `num` rồi ép.
    recommendedFocus: readPrimitiveList<num>(json, 'recommendedFocus').map((n) => n.toInt()).toList(),
  );

  final int totalAnswered;

  /// 0..1; `null` khi chưa trả lời câu nào.
  final double? accuracy;

  /// Thanh nên luyện (1–4), rỗng khi chưa đủ dữ liệu hoặc đã vững.
  final List<int> recommendedFocus;
}

/// Một ngày trong lịch 90 ngày (R-PG6).
@immutable
class ActivityDay {
  const ActivityDay({required this.date, required this.count});

  /// Thiếu `date` ⇒ `null` (bị `readList` bỏ qua).
  static ActivityDay? fromJson(JsonMap? json) {
    final date = readString(json, 'date');
    if (date == null) return null;
    return ActivityDay(date: date, count: readIntOr(json, 'count'));
  }

  /// `yyyy-MM-dd` theo múi giờ hồ sơ.
  final String date;

  /// `SUM(quantity)` mọi loại hoạt động trong ngày; 0 khi không học.
  final int count;

  @override
  bool operator ==(Object other) => other is ActivityDay && other.date == date && other.count == count;

  @override
  int get hashCode => Object.hash(date, count);
}

/// Tổng quan tiến độ (trang chủ). `localDate`/`timeZone`/`streak`/`today`/`activity` luôn có; các khối còn lại có thể
/// VẮNG khi nguồn dữ liệu không dùng được (R-PG7) ⇒ màn hình ẩn khối, không phải lỗi.
@immutable
class ProgressOverview {
  const ProgressOverview({
    required this.localDate,
    required this.timeZone,
    required this.streak,
    required this.today,
    this.srs,
    this.dailyGoal,
    this.vocabulary,
    this.lessons,
    this.writing,
    this.tone,
    this.activity = const [],
  });

  factory ProgressOverview.fromJson(JsonMap? json) => ProgressOverview(
    localDate: readStringOr(json, 'localDate'),
    timeZone: readStringOr(json, 'timeZone'),
    streak: ProgressStreak.fromJson(readMap(json, 'streak')),
    today: ProgressToday.fromJson(readMap(json, 'today')),
    srs: _opt(readMap(json, 'srs'), ProgressSrs.fromJson),
    dailyGoal: _opt(readMap(json, 'dailyGoal'), ProgressDailyGoal.fromJson),
    vocabulary: _opt(readMap(json, 'vocabulary'), ProgressVocabulary.fromJson),
    lessons: _opt(readMap(json, 'lessons'), ProgressLessons.fromJson),
    writing: _opt(readMap(json, 'writing'), ProgressWriting.fromJson),
    tone: _opt(readMap(json, 'tone'), ProgressTone.fromJson),
    activity: readList(json, 'activity', ActivityDay.fromJson),
  );

  /// "Hôm nay" theo múi giờ hồ sơ.
  final String localDate;

  /// Múi giờ IANA đã dùng để cắt ngày.
  final String timeZone;
  final ProgressStreak streak;
  final ProgressToday today;
  final ProgressSrs? srs;
  final ProgressDailyGoal? dailyGoal;
  final ProgressVocabulary? vocabulary;
  final ProgressLessons? lessons;
  final ProgressWriting? writing;
  final ProgressTone? tone;

  /// 90 phần tử `[today − 89, today]` tăng dần theo ngày (R-PG6).
  final List<ActivityDay> activity;

  static T? _opt<T>(JsonMap? json, T Function(JsonMap? json) parse) => json == null ? null : parse(json);
}
