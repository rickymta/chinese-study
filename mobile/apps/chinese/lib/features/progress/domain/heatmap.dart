import 'package:flutter/foundation.dart';

import '../data/models.dart';
import 'dates.dart';

/// Mức màu ô lịch (R-PG6 + §5.3.4): 0 · 1–9 · 10–29 · 30–59 · ≥ 60 lượt. Giá trị 0..4.
typedef HeatLevel = int;

/// Một ô của lịch hoạt động.
@immutable
class HeatmapCell {
  const HeatmapCell({required this.date, required this.count, required this.level, required this.isToday});

  final String date;
  final int count;
  final HeatLevel level;
  final bool isToday;

  @override
  bool operator ==(Object other) =>
      other is HeatmapCell &&
      other.date == date &&
      other.count == count &&
      other.level == level &&
      other.isToday == isToday;

  @override
  int get hashCode => Object.hash(date, count, level, isToday);

  @override
  String toString() => 'HeatmapCell($date, $count, mức $level${isToday ? ', hôm nay' : ''})';
}

/// Nhãn tháng đặt ở đầu một cột (tuần).
@immutable
class HeatmapMonthLabel {
  const HeatmapMonthLabel({required this.weekIndex, required this.label});

  /// Chỉ số cột (tuần) đặt nhãn.
  final int weekIndex;

  /// `T9`, `T10`…
  final String label;

  @override
  bool operator ==(Object other) => other is HeatmapMonthLabel && other.weekIndex == weekIndex && other.label == label;

  @override
  int get hashCode => Object.hash(weekIndex, label);

  @override
  String toString() => 'HeatmapMonthLabel($weekIndex, $label)';
}

/// Lưới lịch hoạt động: mỗi cột là một tuần Thứ Hai → Chủ nhật (7 phần tử); ô ngoài khoảng dữ liệu ⇒ `null`.
@immutable
class HeatmapGrid {
  const HeatmapGrid({required this.weeks, required this.monthLabels, required this.total, required this.activeDays});

  static const empty = HeatmapGrid(weeks: [], monthLabels: [], total: 0, activeDays: 0);

  final List<List<HeatmapCell?>> weeks;
  final List<HeatmapMonthLabel> monthLabels;

  /// Tổng lượt trong khoảng.
  final int total;

  /// Số ngày có ≥ 1 lượt.
  final int activeDays;

  /// Mọi ô có dữ liệu theo thứ tự thời gian (tiện cho test/nhãn).
  List<HeatmapCell> get cells => [
    for (final week in weeks)
      for (final c in week) ?c,
  ];
}

HeatLevel levelOf(int count) {
  if (count <= 0) return 0;
  if (count < 10) return 1;
  if (count < 30) return 2;
  if (count < 60) return 3;
  return 4;
}

/// Số ngày mặc định của lịch khi server trả ít hơn (backend luôn trả 90 — R-PG6).
const kHeatmapDefaultSpan = 90;

/// Dựng lưới lịch hoạt động từ [activity] (ngày `yyyy-MM-dd` theo múi giờ hồ sơ) và [today] (`localDate` của server)
/// — port `buildHeatmap` web, giữ nguyên luật:
/// - Khoảng hiển thị `[start, today]` với `start` = ngày sớm nhất trong dữ liệu (hoặc `today − 89` khi dữ liệu trống).
/// - Cột = tuần bắt đầu Thứ Hai; ô trước `start` hoặc sau `today` ⇒ `null` (vẽ trống). 90 ngày ⇒ 13–14 cột.
/// - Ngày trùng trong dữ liệu được cộng dồn; ngày sai định dạng hoặc ngoài khoảng bị bỏ qua.
/// - Nhãn tháng đặt ở cột đầu và mỗi cột mà tháng của ô Thứ Hai (hoặc ô đầu tiên có dữ liệu) đổi so với cột trước.
HeatmapGrid buildHeatmap(List<ActivityDay> activity, String today) {
  final todayN = dayNumber(today);
  if (todayN == null) return HeatmapGrid.empty;

  final counts = <int, int>{};
  var minN = todayN;
  for (final day in activity) {
    final n = dayNumber(day.date);
    if (n == null || n > todayN) continue;
    counts[n] = (counts[n] ?? 0) + (day.count < 0 ? 0 : day.count);
    if (n < minN) minN = n;
  }
  final startN = activity.isEmpty ? todayN - (kHeatmapDefaultSpan - 1) : minN;

  // Lùi về Thứ Hai của tuần chứa `start`, tiến tới Chủ nhật của tuần chứa `today`.
  final gridStartN = startN - mondayIndex(dateFromDayNumber(startN));
  final gridEndN = todayN + (6 - mondayIndex(today));

  final weeks = <List<HeatmapCell?>>[];
  var total = 0;
  var activeDays = 0;
  for (var weekStart = gridStartN; weekStart <= gridEndN; weekStart += 7) {
    final week = <HeatmapCell?>[];
    for (var i = 0; i < 7; i++) {
      final n = weekStart + i;
      if (n < startN || n > todayN) {
        week.add(null);
        continue;
      }
      final count = counts[n] ?? 0;
      total += count;
      if (count > 0) activeDays++;
      week.add(HeatmapCell(date: dateFromDayNumber(n), count: count, level: levelOf(count), isToday: n == todayN));
    }
    weeks.add(week);
  }

  final monthLabels = <HeatmapMonthLabel>[];
  int? prevMonth;
  for (var weekIndex = 0; weekIndex < weeks.length; weekIndex++) {
    final first = _firstCell(weeks[weekIndex]);
    if (first == null) continue;
    final month = monthOf(first.date);
    if (month == null) continue;
    if (month != prevMonth) {
      // Cột đầu chỉ có 1–2 ngày cuối tháng cũ mà cột kế đã sang tháng mới ⇒ hai nhãn sát nhau, bỏ nhãn cột đầu.
      final next = weekIndex + 1 < weeks.length ? _firstCell(weeks[weekIndex + 1]) : null;
      final nextMonth = next == null ? null : monthOf(next.date);
      if (!(weekIndex == 0 && nextMonth != null && nextMonth != month)) {
        monthLabels.add(HeatmapMonthLabel(weekIndex: weekIndex, label: 'T$month'));
      }
      prevMonth = month;
    }
  }

  return HeatmapGrid(weeks: weeks, monthLabels: monthLabels, total: total, activeDays: activeDays);
}

HeatmapCell? _firstCell(List<HeatmapCell?> week) {
  for (final c in week) {
    if (c != null) return c;
  }
  return null;
}

/// Ngày đầu tiên của lưới (để test/nhãn): `today − (span − 1)`.
String defaultRangeStart(String today, [int span = kHeatmapDefaultSpan]) => addDays(today, -(span - 1));
