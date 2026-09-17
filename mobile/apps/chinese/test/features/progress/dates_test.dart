import 'package:af_chinese/features/progress/domain/dates.dart';
import 'package:flutter_test/flutter_test.dart';

/// Chép đủ ca của `features/progress/lib/dates.test.ts` (web).
void main() {
  group('dayNumber / dateFromDayNumber', () {
    test('đi và về cùng một ngày', () {
      for (final d in ['1970-01-01', '2026-09-17', '2024-02-29', '1999-12-31']) {
        expect(dateFromDayNumber(dayNumber(d)!), d);
      }
      expect(dayNumber('1970-01-01'), 0);
    });

    test('từ chối chuỗi sai định dạng hoặc ngày không tồn tại', () {
      expect(dayNumber('2026-9-7'), isNull);
      expect(dayNumber('2026-02-30'), isNull);
      expect(dayNumber('2026-09-17T00:00:00Z'), isNull);
      expect(dayNumber(''), isNull);
    });

    test('trước epoch: số âm đúng (1969-12-31 = −1)', () {
      expect(dayNumber('1969-12-31'), -1);
      expect(dateFromDayNumber(-1), '1969-12-31');
    });
  });

  group('addDays', () {
    test('qua ranh giới tháng, năm và năm nhuận', () {
      expect(addDays('2026-09-17', 1), '2026-09-18');
      expect(addDays('2026-12-31', 1), '2027-01-01');
      expect(addDays('2024-02-28', 1), '2024-02-29');
      expect(addDays('2026-09-17', -89), '2026-06-20');
    });
    test('chuỗi sai ⇒ trả nguyên', () {
      expect(addDays('abc', 3), 'abc');
    });
  });

  group('mondayIndex / weekdayName', () {
    test('2026-09-17 là Thứ Năm, 2026-09-20 là Chủ nhật, 2026-09-21 là Thứ Hai', () {
      expect(mondayIndex('2026-09-17'), 3);
      expect(weekdayName('2026-09-17'), 'Thứ Năm');
      expect(mondayIndex('2026-09-20'), 6);
      expect(weekdayName('2026-09-20'), 'Chủ nhật');
      expect(mondayIndex('2026-09-21'), 0);
    });
    test('trước epoch vẫn đúng (1969-12-31 là Thứ Tư); chuỗi sai ⇒ 0', () {
      expect(mondayIndex('1969-12-31'), 2);
      expect(mondayIndex('xyz'), 0);
    });
  });

  group('formatDdMm / monthOf', () {
    test('định dạng dd/MM và lấy tháng', () {
      expect(formatDdMm('2026-09-07'), '07/09');
      expect(monthOf('2026-12-01'), 12);
      expect(formatDdMm('xyz'), 'xyz');
      expect(monthOf('xyz'), isNull);
    });
    test('nhãn thứ viết tắt T2…CN', () {
      expect(kWeekdayShortVi, ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']);
    });
  });
}
