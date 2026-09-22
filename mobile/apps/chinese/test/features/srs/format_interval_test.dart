import 'package:af_chinese/features/srs/domain/format_interval.dart';
import 'package:flutter_test/flutter_test.dart';

// Chép đủ ca của `formatInterval.test.ts` web.
void main() {
  group('parseDurationSeconds', () {
    test('đọc ISO-8601 duration', () {
      expect(parseDurationSeconds('PT1M'), 60);
      expect(parseDurationSeconds('PT5M30S'), 330);
      expect(parseDurationSeconds('P8D'), 8 * 86400);
      expect(parseDurationSeconds('P1DT2H'), 86400 + 7200);
      expect(parseDurationSeconds('PT0.5S'), 0.5);
    });

    test('đọc TimeSpan .NET (phòng backend trả mặc định)', () {
      expect(parseDurationSeconds('00:10:00'), 600);
      expect(parseDurationSeconds('8.00:00:00'), 8 * 86400);
      expect(parseDurationSeconds('00:05:30.5000000'), 330.5);
    });

    test('trả null khi không hiểu', () {
      expect(parseDurationSeconds(''), isNull);
      expect(parseDurationSeconds(null), isNull);
      expect(parseDurationSeconds('P'), isNull);
      expect(parseDurationSeconds('10 phút'), isNull);
    });
  });

  group('formatInterval — ca bắt buộc của hợp đồng', () {
    const cases = {
      'PT1M': '1 phút',
      'PT5M30S': '5,5 phút',
      'PT10M': '10 phút',
      'PT15M': '15 phút',
      'P1D': '1 ngày',
      'P8D': '8 ngày',
      'P45D': '1,5 tháng',
      'P60D': '2 tháng',
      'P498D': '1,4 năm',
    };
    for (final entry in cases.entries) {
      test('${entry.key} → ${entry.value}', () => expect(formatInterval(entry.key), entry.value));
    }
  });

  group('formatInterval — biên', () {
    test('giờ và ngày làm tròn số nguyên, tối thiểu 1', () {
      expect(formatInterval('PT90M'), '2 giờ');
      expect(formatInterval('PT1H'), '1 giờ');
      expect(formatInterval('P1DT12H'), '2 ngày');
      expect(formatInterval('PT23H50M'), '1 ngày');
    });

    test('dưới 10 phút giữ một chữ số thập phân, từ 10 phút làm tròn', () {
      expect(formatInterval('PT30S'), '0,5 phút');
      expect(formatInterval('PT12M40S'), '13 phút');
      expect(formatInterval('PT59M40S'), '1 giờ');
    });

    test('không hiểu ⇒ gạch ngang', () {
      expect(formatInterval(null), '—');
      expect(formatInterval('abc'), '—');
    });
  });
}
