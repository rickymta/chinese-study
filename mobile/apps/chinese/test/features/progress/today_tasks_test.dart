import 'package:af_chinese/features/progress/data/models.dart';
import 'package:af_chinese/features/progress/domain/today_tasks.dart';
import 'package:flutter_test/flutter_test.dart';

/// Tổng quan mẫu theo §6.4 — mọi khối có việc để kiểm thứ tự (như `overview()` của `todayTasks.test.ts`).
/// Tham số `*Set` để phân biệt "truyền null" với "không truyền".
ProgressOverview overview({
  ProgressSrs? srs = const ProgressSrs(
    dueToday: 23,
    dueNow: 20,
    newAvailableToday: 10,
    newIntroducedToday: 0,
    reviewedToday: 0,
  ),
  ProgressLessons? lessons = const ProgressLessons(
    published: 5,
    completed: 2,
    inProgress: 1,
    next: ProgressLessonRef(slug: 'so-dem', title: 'Số đếm'),
    lastCompleted: ProgressLastCompletedLesson(slug: 'ban-than', title: 'Giới thiệu bản thân', unpracticedChars: 9),
  ),
  ProgressTone? tone = const ProgressTone(totalAnswered: 30, accuracy: 0.82, recommendedFocus: [2, 3]),
}) => ProgressOverview(
  localDate: '2026-09-18',
  timeZone: 'Asia/Ho_Chi_Minh',
  streak: const ProgressStreak(current: 5, longest: 12, studiedToday: false),
  today: const ProgressToday(),
  srs: srs,
  dailyGoal: const ProgressDailyGoal(done: 0, total: 33, achieved: false),
  vocabulary: const ProgressVocabulary(totalInPath: 500, introduced: 64, learning: 52, mature: 12),
  lessons: lessons,
  writing: const ProgressWriting(practicedChars: 24, masteredChars: 5, weakChars: 3, totalChars: 297),
  tone: tone,
);

/// Chép đủ ca của `features/progress/lib/todayTasks.test.ts` (web).
void main() {
  group('buildTodayTasks — thứ tự R-PG9', () {
    test('đủ 6 mục theo đúng thứ tự khi mọi khối còn việc', () {
      final tasks = buildTodayTasks(overview());
      expect(tasks.map((t) => t.kind), [
        TodayTaskKind.review,
        TodayTaskKind.newCards,
        TodayTaskKind.lesson,
        TodayTaskKind.writing,
        TodayTaskKind.tone,
        TodayTaskKind.pinyin,
      ]);
      expect(tasks[0], const TodayTask(kind: TodayTaskKind.review, title: 'Ôn 23 thẻ đến hạn', to: '/on-tap'));
      expect(tasks[1], const TodayTask(kind: TodayTaskKind.newCards, title: 'Học 10 thẻ mới', to: '/on-tap'));
      expect(
        tasks[2],
        const TodayTask(kind: TodayTaskKind.lesson, title: 'Bài tiếp theo', subtitle: 'Số đếm', to: '/bai-hoc/so-dem'),
      );
      expect(tasks[3].subtitle, '9 chữ chưa luyện');
      expect(tasks[3].to, '/luyen-viet?tab=bai-hoc&bai=ban-than');
      expect(tasks[3].title, contains('Giới thiệu bản thân'));
      expect(tasks[4].title, 'Luyện thanh 2, 3');
      expect(tasks[4].to, '/pinyin?tab=luyen');
      expect(tasks[5].title, 'Học pinyin trước');
      expect(tasks[5].subtitle, 'Mới trả lời 30/40 câu luyện thanh');
      expect(tasks[5].to, '/pinyin');
    });

    test('mục bằng 0 thì ẩn', () {
      final tasks = buildTodayTasks(
        overview(
          srs: const ProgressSrs(dueToday: 0, dueNow: 0, newAvailableToday: 0, newIntroducedToday: 3, reviewedToday: 8),
          lessons: const ProgressLessons(
            published: 5,
            completed: 5,
            inProgress: 0,
            lastCompleted: ProgressLastCompletedLesson(slug: 'x', title: 'X', unpracticedChars: 0),
          ),
          tone: const ProgressTone(totalAnswered: 240, accuracy: 0.9, recommendedFocus: []),
        ),
      );
      expect(tasks, isEmpty);
    });

    test('khối null/vắng bị bỏ qua, các mục khác vẫn có', () {
      expect(buildTodayTasks(overview(srs: null, lessons: null, tone: null)), isEmpty);
      final only = buildTodayTasks(overview(srs: null, tone: null));
      expect(only.map((t) => t.kind), [TodayTaskKind.lesson, TodayTaskKind.writing]);
    });

    test('người mới: bài đầu + học pinyin trước', () {
      final tasks = buildTodayTasks(
        overview(
          srs: const ProgressSrs(dueToday: 0, dueNow: 0, newAvailableToday: 0, newIntroducedToday: 0, reviewedToday: 0),
          lessons: const ProgressLessons(
            published: 5,
            completed: 0,
            inProgress: 0,
            next: ProgressLessonRef(slug: 'chao-hoi', title: 'Chào hỏi'),
          ),
          tone: const ProgressTone(totalAnswered: 0),
        ),
      );
      expect(tasks.map((t) => t.kind), [TodayTaskKind.lesson, TodayTaskKind.pinyin]);
      expect(tasks[0].title, 'Bắt đầu bài học đầu tiên');
      expect(tasks[1].subtitle, 'Nghe và phân biệt 4 thanh điệu trước khi học từ');
    });

    test('slug bài được encode trong đường dẫn', () {
      final tasks = buildTodayTasks(
        overview(
          srs: null,
          tone: null,
          lessons: const ProgressLessons(
            published: 1,
            completed: 0,
            inProgress: 0,
            next: ProgressLessonRef(slug: 'a b', title: 'Lạ'),
            lastCompleted: ProgressLastCompletedLesson(slug: 'c&d', title: 'T', unpracticedChars: 1),
          ),
        ),
      );
      expect(tasks[0].to, '/bai-hoc/a%20b');
      expect(tasks[1].to, '/luyen-viet?tab=bai-hoc&bai=c%26d');
    });

    test('hằng số giữ nguyên như web', () {
      expect(kPinyinMinAnswered, 40);
      expect(kToneDrillPath, '/pinyin?tab=luyen');
    });
  });
}
