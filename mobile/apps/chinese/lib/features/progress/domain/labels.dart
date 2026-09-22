import 'package:af_auth/af_auth.dart';

import '../data/models.dart';
import 'dates.dart';

/// Tiêu đề trang chủ: `Hôm nay, Thứ Năm 17/09` theo `localDate` của server (không dùng ngày máy) — port `labels.ts`.
String todayHeading(String localDate) {
  if (dayNumber(localDate) == null) return 'Hôm nay';
  return 'Hôm nay, ${weekdayName(localDate)} ${formatDdMm(localDate)}';
}

/// Câu nhắc dưới số chuỗi ngày (StreakCard).
String streakMessage(ProgressStreak streak) {
  if (streak.studiedToday) {
    return streak.current == 1 ? 'Hôm nay đã học — chuỗi bắt đầu!' : 'Hôm nay đã học — chuỗi được giữ.';
  }
  if (streak.current == 0) return 'Học 1 hoạt động hôm nay để bắt đầu chuỗi.';
  return 'Học 1 hoạt động để giữ chuỗi.';
}

/// Nhãn chuỗi dài nhất; bằng chuỗi hiện tại (và > 0) ⇒ đang ở kỷ lục.
String longestLabel(ProgressStreak streak) {
  if (streak.longest <= 0) return 'Chưa có chuỗi nào';
  if (streak.current == streak.longest) return 'Kỷ lục: ${streak.longest} ngày — đang ở mức cao nhất';
  return 'Dài nhất: ${streak.longest} ngày';
}

/// So sánh múi giờ hồ sơ với múi giờ máy — khác ⇒ trang chủ hiện dòng nhắc (§5.3.4). Quy bí danh CLDR cũ về tên
/// hiện hành trước khi so (`Asia/Saigon` ≡ `Asia/Ho_Chi_Minh`) — không quy thì MỌI học viên ở VN thấy dòng nhắc oan.
/// Thiếu một trong hai ⇒ không nhắc.
bool isDeviceTimeZoneDifferent(String profileTimeZone, String? deviceTimeZone) {
  if (profileTimeZone.isEmpty || deviceTimeZone == null || deviceTimeZone.isEmpty) return false;
  return normalizeTimeZone(profileTimeZone).toLowerCase() != normalizeTimeZone(deviceTimeZone).toLowerCase();
}

/// `0.82` ⇒ `82`; `null`/NaN ⇒ `null`; kẹp 0..100.
int? toPercent(double? ratio) {
  if (ratio == null || ratio.isNaN) return null;
  return (ratio.clamp(0.0, 1.0) * 100).round();
}
