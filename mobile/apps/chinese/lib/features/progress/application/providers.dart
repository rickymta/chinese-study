import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../data/models.dart';
import '../data/progress_api.dart';

/// Tổng quan tiến độ (`GET /progress/overview`) — tương đương `useProgressOverview` web. Family THEO ID NGƯỜI DÙNG
/// (đọc qua [progressOverviewProvider]):
/// - đổi tài khoản trên cùng máy ⇒ instance mới, không mang `.value` của người trước (mục "Đổi tài khoản" README —
///   cùng mục đích với `watch(userScopeProvider)` + `unwrapPrevious()`);
/// - làm mới cùng người (`invalidate`, kéo-để-làm-mới, app resumed, chọn lại tab) ⇒ giữ số cũ trong lúc tải, huy hiệu
///   "Ôn tập" không nháy về 0 — điều `unwrapPrevious()` trên một provider chung không làm được.
/// `autoDispose`: trang chủ + huy hiệu ở shell cùng giữ; đăng xuất ⇒ shell gỡ ⇒ cache tự huỷ.
final progressOverviewByUserProvider = FutureProvider.autoDispose.family<ProgressOverview, String?>((ref, userId) {
  // Chỉ chạy khi đã đăng nhập; `AuthGate` bảo đảm điều đó, đây là chốt phòng hờ (không gọi API không token).
  if (userId == null) throw ApiError('Cần đăng nhập để tiếp tục.', status: 401);
  return getProgressOverview(ref.watch(chineseDioProvider));
});

/// Provider tổng quan của người dùng HIỆN TẠI (theo `userScopeProvider`) — `ref.watch(ref.watch(progressOverviewProvider))`.
final progressOverviewProvider = Provider.autoDispose<FutureProvider<ProgressOverview>>(
  (ref) => progressOverviewByUserProvider(ref.watch(userScopeProvider)),
);

/// Làm mới tổng quan (mọi người dùng — chỉ instance đang được giữ mới tải lại): sau khi lưu hồ sơ (múi giờ đổi ⇒
/// "hôm nay" đổi), lưu cài đặt học tập (hạn mức thẻ mới đổi), app resumed, chọn lại tab Trang chủ; M6+ gọi sau khi
/// gửi đánh giá/nộp quiz/viết chữ như danh sách invalidate của web.
extension ProgressWidgetRefX on WidgetRef {
  void invalidateProgressOverview() => invalidate(progressOverviewByUserProvider);
}

/// Bản cho provider/Notifier (M6 gửi outbox xong ⇒ làm mới).
extension ProgressRefX on Ref {
  void invalidateProgressOverview() => invalidate(progressOverviewByUserProvider);
}
