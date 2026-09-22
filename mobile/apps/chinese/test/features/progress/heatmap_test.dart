import 'package:af_chinese/features/progress/data/models.dart';
import 'package:af_chinese/features/progress/domain/dates.dart';
import 'package:af_chinese/features/progress/domain/heatmap.dart';
import 'package:flutter_test/flutter_test.dart';

/// 90 phần tử `[today − 89, today]` như backend trả (R-PG6), `count` theo hàm cho trước.
List<ActivityDay> activity90(String today, [int Function(String date, int i)? countOf]) {
  final start = defaultRangeStart(today);
  return [
    for (var i = 0; i < 90; i++) ActivityDay(date: addDays(start, i), count: countOf?.call(addDays(start, i), i) ?? 0),
  ];
}

/// Chép đủ ca của `features/progress/lib/heatmap.test.ts` (web).
void main() {
  group('levelOf — ngưỡng biên', () {
    for (final (count, level) in const [
      (0, 0),
      (1, 1),
      (9, 1),
      (10, 2),
      (29, 2),
      (30, 3),
      (59, 3),
      (60, 4),
      (500, 4),
    ]) {
      test('$count lượt ⇒ mức $level', () => expect(levelOf(count), level));
    }
    test('số âm coi như 0', () => expect(levelOf(-3), 0));
  });

  group('buildHeatmap — kích thước lưới', () {
    test('today là Chủ nhật (2026-09-20) ⇒ 13 cột, cột cuối đủ 7 ô, ô cuối là hôm nay', () {
      final grid = buildHeatmap(activity90('2026-09-20'), '2026-09-20');
      expect(grid.weeks, hasLength(13));
      for (final week in grid.weeks) {
        expect(week, hasLength(7));
      }
      final last = grid.weeks[12];
      expect(last[6]?.date, '2026-09-20');
      expect(last[6]?.isToday, isTrue);
      // Ngày đầu khoảng 2026-06-23 là Thứ Ba ⇒ ô Thứ Hai của cột đầu trống.
      expect(grid.weeks[0][0], isNull);
      expect(grid.weeks[0][1]?.date, '2026-06-23');
    });

    test('today là Thứ Hai (2026-09-21) ⇒ 14 cột, cột cuối chỉ có ô Thứ Hai', () {
      final grid = buildHeatmap(activity90('2026-09-21'), '2026-09-21');
      expect(grid.weeks, hasLength(14));
      final last = grid.weeks[13];
      expect(last[0]?.date, '2026-09-21');
      expect(last.sublist(1).every((c) => c == null), isTrue);
    });

    test('today là Thứ Năm (2026-09-17) ⇒ 14 cột (20/06 là Thứ Bảy), các ô sau hôm nay trống', () {
      final grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17');
      expect(grid.weeks, hasLength(14));
      final last = grid.weeks[13];
      expect(last[3]?.isToday, isTrue);
      expect(last[4], isNull);
      expect(last[6], isNull);
    });

    test('đúng 90 ô có dữ liệu, còn lại là null', () {
      final grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17');
      expect(grid.cells, hasLength(90));
    });

    test('activity rỗng ⇒ vẫn dựng 90 ngày trống kết thúc ở hôm nay', () {
      final grid = buildHeatmap(const [], '2026-09-17');
      final cells = grid.cells;
      expect(cells, hasLength(90));
      expect(cells.first.date, '2026-06-20');
      expect(cells.last.date, '2026-09-17');
      expect(grid.total, 0);
      expect(grid.activeDays, 0);
    });

    test('today sai định dạng ⇒ lưới rỗng, không ném', () {
      final grid = buildHeatmap(activity90('2026-09-17'), 'hôm nay');
      expect(grid.weeks, isEmpty);
      expect(grid.monthLabels, isEmpty);
      expect(grid.total, 0);
      expect(grid.activeDays, 0);
    });
  });

  group('buildHeatmap — số liệu và mức màu', () {
    test('tính tổng, số ngày có học và mức màu từng ô', () {
      const today = '2026-09-17';
      final data = activity90(
        today,
        (date, _) => date == today
            ? 12
            : date == '2026-09-16'
            ? 1
            : 0,
      );
      final grid = buildHeatmap(data, today);
      expect(grid.total, 13);
      expect(grid.activeDays, 2);
      expect(grid.cells.firstWhere((c) => c.date == today).level, 2);
      expect(grid.cells.firstWhere((c) => c.date == '2026-09-16').level, 1);
    });

    test('ngày trùng được cộng dồn; ngày tương lai và chuỗi sai bị bỏ qua', () {
      const today = '2026-09-17';
      const data = [
        ActivityDay(date: today, count: 5),
        ActivityDay(date: today, count: 7),
        ActivityDay(date: '2026-09-18', count: 99),
        ActivityDay(date: 'không-phải-ngày', count: 99),
      ];
      final grid = buildHeatmap(data, today);
      expect(grid.cells.firstWhere((c) => c.date == today).count, 12);
      expect(grid.total, 12);
      // Chỉ có 1 ngày dữ liệu ⇒ khoảng là [today, today] ⇒ đúng 1 ô.
      expect(grid.cells, hasLength(1));
    });

    test('count âm coi như 0', () {
      final grid = buildHeatmap(const [ActivityDay(date: '2026-09-17', count: -4)], '2026-09-17');
      expect(grid.total, 0);
      expect(grid.cells.single.level, 0);
    });
  });

  group('buildHeatmap — nhãn tháng', () {
    test('90 ngày 20/06 → 17/09 có nhãn T6, T7, T8, T9 (20/06 là Thứ Bảy ⇒ cột 0–2 thuộc tháng 6)', () {
      final grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17');
      expect(grid.monthLabels.map((m) => m.label), ['T6', 'T7', 'T8', 'T9']);
      expect(grid.monthLabels[0].weekIndex, 0);
      // Chỉ số cột tăng dần, mỗi nhãn cách nhau ≥ 3 cột để không đè chữ.
      for (var i = 1; i < grid.monthLabels.length; i++) {
        expect(grid.monthLabels[i].weekIndex - grid.monthLabels[i - 1].weekIndex, greaterThanOrEqualTo(3));
      }
    });

    test('cột đầu chỉ có 1–2 ngày cuối tháng cũ mà cột kế đã sang tháng mới ⇒ bỏ nhãn cột đầu', () {
      // 2026-08-31 là Thứ Hai ⇒ cột 0 mở đầu bằng 31/08 (T8) rồi 01–06/09; cột 1 = 07/09 (T9). Cột 0 chỉ có một ngày
      // của tháng 8 mà cột kế đã sang tháng 9 ⇒ chỉ gắn T9 ở cột 1.
      final data = [for (var i = 0; i < 8; i++) ActivityDay(date: addDays('2026-08-31', i), count: 0)];
      final grid = buildHeatmap(data, '2026-09-07');
      expect(grid.monthLabels, const [HeatmapMonthLabel(weekIndex: 1, label: 'T9')]);
    });

    test('khoảng bắt đầu đầu tháng ⇒ cột đầu có nhãn tháng đó', () {
      // 2026-09-01 là Thứ Ba; 5 ngày dữ liệu tới 2026-09-05.
      final data = [for (var i = 0; i < 5; i++) ActivityDay(date: addDays('2026-09-01', i), count: 0)];
      final grid = buildHeatmap(data, '2026-09-05');
      expect(grid.monthLabels, const [HeatmapMonthLabel(weekIndex: 0, label: 'T9')]);
    });
  });
}
