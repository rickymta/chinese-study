import 'package:flutter/foundation.dart';

import '../data/models.dart';

/// Loại việc trong "Việc hôm nay" — đúng thứ tự R-PG9.
enum TodayTaskKind { review, newCards, lesson, writing, tone, pinyin }

/// Một mục việc hôm nay: tiêu đề, dòng phụ (số lượng, tên bài…), đường dẫn trong app (giống web).
@immutable
class TodayTask {
  const TodayTask({required this.kind, required this.title, this.subtitle, required this.to});

  final TodayTaskKind kind;
  final String title;
  final String? subtitle;
  final String to;

  @override
  bool operator ==(Object other) =>
      other is TodayTask && other.kind == kind && other.title == title && other.subtitle == subtitle && other.to == to;

  @override
  int get hashCode => Object.hash(kind, title, subtitle, to);

  @override
  String toString() => 'TodayTask(${kind.name}, "$title"${subtitle == null ? '' : ', "$subtitle"'}, $to)';
}

/// Ngưỡng "chưa học pinyin đủ" (R-PG9 mục 6).
const kPinyinMinAnswered = 40;

/// Đường dẫn tab luyện thanh của trang Pinyin (`?tab=luyen` — F5/M7).
const kToneDrillPath = '/pinyin?tab=luyen';

/// Danh sách "Việc hôm nay" theo R-PG9 — port `buildTodayTasks` web: đúng thứ tự, chỉ mục còn việc; khối dữ liệu
/// vắng (`null`) thì bỏ qua các mục dựa vào khối đó. Nhãn dùng số thẻ theo `srs` (không dùng `dailyGoal`, vì mục
/// tiêu ngày hiển thị riêng — D8).
List<TodayTask> buildTodayTasks(ProgressOverview overview) {
  final tasks = <TodayTask>[];
  final srs = overview.srs;
  final lessons = overview.lessons;
  final tone = overview.tone;

  if (srs != null && srs.dueToday > 0) {
    tasks.add(TodayTask(kind: TodayTaskKind.review, title: 'Ôn ${srs.dueToday} thẻ đến hạn', to: '/on-tap'));
  }
  if (srs != null && srs.newAvailableToday > 0) {
    tasks.add(TodayTask(kind: TodayTaskKind.newCards, title: 'Học ${srs.newAvailableToday} thẻ mới', to: '/on-tap'));
  }
  final next = lessons?.next;
  if (lessons != null && next != null) {
    tasks.add(
      TodayTask(
        kind: TodayTaskKind.lesson,
        title: lessons.completed == 0 && lessons.inProgress == 0 ? 'Bắt đầu bài học đầu tiên' : 'Bài tiếp theo',
        subtitle: next.title,
        to: '/bai-hoc/${Uri.encodeComponent(next.slug)}',
      ),
    );
  }
  final last = lessons?.lastCompleted;
  if (last != null && last.unpracticedChars > 0) {
    tasks.add(
      TodayTask(
        kind: TodayTaskKind.writing,
        title: 'Luyện viết chữ bài "${last.title}"',
        subtitle: '${last.unpracticedChars} chữ chưa luyện',
        to: '/luyen-viet?tab=bai-hoc&bai=${Uri.encodeComponent(last.slug)}',
      ),
    );
  }
  if (tone != null && tone.recommendedFocus.isNotEmpty) {
    tasks.add(
      TodayTask(
        kind: TodayTaskKind.tone,
        title: 'Luyện thanh ${tone.recommendedFocus.join(', ')}',
        subtitle: 'Thanh bạn nghe chưa vững',
        to: kToneDrillPath,
      ),
    );
  }
  if (tone != null && tone.totalAnswered < kPinyinMinAnswered) {
    tasks.add(
      TodayTask(
        kind: TodayTaskKind.pinyin,
        title: 'Học pinyin trước',
        subtitle: tone.totalAnswered == 0
            ? 'Nghe và phân biệt 4 thanh điệu trước khi học từ'
            : 'Mới trả lời ${tone.totalAnswered}/$kPinyinMinAnswered câu luyện thanh',
        to: '/pinyin',
      ),
    );
  }
  return tasks;
}
