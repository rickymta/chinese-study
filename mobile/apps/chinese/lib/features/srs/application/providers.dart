import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../data/models.dart';
import '../data/srs_api.dart';

/// Cài đặt học tập của người dùng hiện tại — NGUỒN SỰ THẬT của `ttsRate`/`autoPlayAudio` cho mọi màn (tương đương
/// `useLearningSettings` web). Theo người dùng (`userScopeProvider`): đăng xuất/đổi người ⇒ dựng lại; chưa đăng
/// nhập ⇒ mặc định (không gọi API).
///
/// [save] ghi `PUT` rồi đặt thẳng kết quả vào state (không GET lại); lỗi ném cho màn hình báo tại chỗ, state giữ
/// dữ liệu cũ. Nơi gọi `save` phải `ref.invalidateSrsSummary()` sau đó (hạn mức thẻ mới đổi ⇒ số thẻ đến hạn đổi).
class LearningSettingsNotifier extends AsyncNotifier<LearningSettings> {
  @override
  Future<LearningSettings> build() async {
    final userId = ref.watch(userScopeProvider);
    if (userId == null) return LearningSettings.defaults;
    return getLearningSettings(ref.watch(chineseDioProvider));
  }

  /// `PUT /me/learning-settings`; thành công ⇒ state = bản server trả về (`isDefault=false`).
  Future<LearningSettings> save(LearningSettings body) async {
    final saved = await putLearningSettings(ref.read(chineseDioProvider), body);
    if (ref.mounted) state = AsyncData(saved);
    return saved;
  }
}

final learningSettingsProvider = AsyncNotifierProvider<LearningSettingsNotifier, LearningSettings>(
  LearningSettingsNotifier.new,
);

/// Tóm tắt SRS (`GET /srs/summary`) — tương đương `useSrsSummary` web: nguồn DUY NHẤT của huy hiệu nhánh "Ôn tập"
/// và trang `/on-tap` (chốt M6 theo gợi ý review M5). Family THEO ID NGƯỜI DÙNG như `progressOverviewByUserProvider`:
/// đổi tài khoản ⇒ instance mới (không mang số của người trước); làm mới cùng người ⇒ giữ số cũ trong lúc tải (huy hiệu
/// không nháy về 0). KHÔNG autoDispose để `apply` từ outbox (lúc shell không mount — phiên ôn toàn màn) không bị huỷ
/// ngay; `build` tự chặn khi user đổi nên lần đăng nhập sau tải mới, không thấy số cũ.
class SrsSummaryNotifier extends AsyncNotifier<SrsSummary> {
  SrsSummaryNotifier(this.userId);

  /// Tham số family (Riverpod 3 truyền qua constructor).
  final String? userId;

  @override
  Future<SrsSummary> build() async {
    // Chỉ gọi API khi đúng là người đang đăng nhập: đăng xuất/đổi người ⇒ dựng lại và dừng ở đây (không gọi
    // `/srs/summary` không token — review M6); người này đăng nhập lại ⇒ dựng lại và tải mới.
    if (userId == null || ref.watch(userScopeProvider) != userId) {
      throw ApiError('Cần đăng nhập để tiếp tục.', status: 401);
    }
    return getSrsSummary(ref.watch(chineseDioProvider));
  }

  /// Ghi `summary` nhận từ phản hồi chấm thẻ/hàng đợi (không cần GET lại) — port `useApplySrsSummary`.
  void apply(SrsSummary summary) {
    if (ref.mounted) state = AsyncData(summary);
  }
}

final srsSummaryByUserProvider = AsyncNotifierProvider.family<SrsSummaryNotifier, SrsSummary, String?>(
  SrsSummaryNotifier.new,
);

/// Provider tóm tắt của người dùng HIỆN TẠI — `ref.watch(ref.watch(srsSummaryProvider))`.
final srsSummaryProvider = Provider<AsyncNotifierProvider<SrsSummaryNotifier, SrsSummary>>(
  (ref) => srsSummaryByUserProvider(ref.watch(userScopeProvider)),
);

/// Số trên huy hiệu nhánh "Ôn tập" = `dueNow + newAvailableToday` (hợp đồng §5.3.8; `AfShellScaffold` cắt "99+").
/// Chưa tải/lỗi/không có quyền học ⇒ 0. Nguồn: [srsSummaryProvider] (không phải tổng quan — một nguồn, một số).
final reviewBadgeProvider = Provider<int>((ref) {
  if (!ref.watch(permissionsProvider).contains(kStudyUsePermission)) return 0;
  return ref.watch(ref.watch(srsSummaryProvider)).value?.toStart ?? 0;
});

/// Làm mới tóm tắt SRS (mọi người dùng — chỉ instance đang giữ mới tải lại): sau khi lưu cài đặt học tập, lưu hồ sơ
/// (múi giờ đổi ⇒ "hôm nay" đổi), thêm/tạm dừng thẻ, app resumed. Sau khi CHẤM thì outbox `apply` summary server trả.
extension SrsWidgetRefX on WidgetRef {
  void invalidateSrsSummary() => invalidate(srsSummaryByUserProvider);
}

/// Bản cho provider/Notifier.
extension SrsRefX on Ref {
  void invalidateSrsSummary() => invalidate(srsSummaryByUserProvider);
}
