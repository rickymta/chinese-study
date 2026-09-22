import 'package:af_chinese/features/progress/data/models.dart';
import 'package:af_chinese/features/progress/domain/labels.dart';
import 'package:flutter_test/flutter_test.dart';

/// Chép đủ ca của `features/progress/lib/labels.test.ts` (web); `isBrowserTimeZoneDifferent` ⇒ `isDeviceTimeZoneDifferent`.
void main() {
  group('todayHeading', () {
    test('theo localDate của server, không phụ thuộc đồng hồ máy', () {
      expect(todayHeading('2026-09-17'), 'Hôm nay, Thứ Năm 17/09');
      expect(todayHeading('2026-09-20'), 'Hôm nay, Chủ nhật 20/09');
      expect(todayHeading('2027-01-01'), 'Hôm nay, Thứ Sáu 01/01');
    });
    test('chuỗi sai ⇒ chỉ "Hôm nay"', () {
      expect(todayHeading(''), 'Hôm nay');
      expect(todayHeading('17/09/2026'), 'Hôm nay');
    });
  });

  group('streakMessage / longestLabel', () {
    test('chưa học hôm nay', () {
      expect(
        streakMessage(const ProgressStreak(current: 0, longest: 0, studiedToday: false)),
        'Học 1 hoạt động hôm nay để bắt đầu chuỗi.',
      );
      expect(
        streakMessage(const ProgressStreak(current: 4, longest: 9, studiedToday: false)),
        'Học 1 hoạt động để giữ chuỗi.',
      );
    });
    test('đã học hôm nay', () {
      expect(
        streakMessage(const ProgressStreak(current: 1, longest: 1, studiedToday: true)),
        'Hôm nay đã học — chuỗi bắt đầu!',
      );
      expect(
        streakMessage(const ProgressStreak(current: 5, longest: 12, studiedToday: true)),
        'Hôm nay đã học — chuỗi được giữ.',
      );
    });
    test('nhãn chuỗi dài nhất', () {
      expect(longestLabel(const ProgressStreak(current: 0, longest: 0, studiedToday: false)), 'Chưa có chuỗi nào');
      expect(longestLabel(const ProgressStreak(current: 2, longest: 10, studiedToday: true)), 'Dài nhất: 10 ngày');
      expect(
        longestLabel(const ProgressStreak(current: 10, longest: 10, studiedToday: true)),
        contains('Kỷ lục: 10 ngày'),
      );
    });
  });

  group('isDeviceTimeZoneDifferent', () {
    test('so sánh không phân biệt hoa thường; thiếu dữ liệu ⇒ không nhắc', () {
      expect(isDeviceTimeZoneDifferent('Asia/Ho_Chi_Minh', 'Asia/Ho_Chi_Minh'), isFalse);
      expect(isDeviceTimeZoneDifferent('Asia/Ho_Chi_Minh', 'asia/ho_chi_minh'), isFalse);
      expect(isDeviceTimeZoneDifferent('America/New_York', 'Asia/Ho_Chi_Minh'), isTrue);
      expect(isDeviceTimeZoneDifferent('America/New_York', null), isFalse);
      expect(isDeviceTimeZoneDifferent('America/New_York', ''), isFalse);
      expect(isDeviceTimeZoneDifferent('', 'Asia/Ho_Chi_Minh'), isFalse);
    });
    test('bí danh cũ (máy trả Asia/Saigon) coi như cùng múi giờ', () {
      expect(isDeviceTimeZoneDifferent('Asia/Ho_Chi_Minh', 'Asia/Saigon'), isFalse);
      expect(isDeviceTimeZoneDifferent('Asia/Saigon', 'Asia/Ho_Chi_Minh'), isFalse);
      expect(isDeviceTimeZoneDifferent('America/Los_Angeles', 'Asia/Saigon'), isTrue);
    });
  });

  group('toPercent', () {
    test('làm tròn và kẹp 0..100', () {
      expect(toPercent(0.82), 82);
      expect(toPercent(0.825), 83);
      expect(toPercent(1.2), 100);
      expect(toPercent(-0.5), 0);
      expect(toPercent(null), isNull);
      expect(toPercent(double.nan), isNull);
    });
  });
}
