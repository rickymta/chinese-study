import 'package:af_core/af_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../data/models.dart';
import '../data/pinyin_api.dart';

/// Bảng pinyin (`GET /pinyin/chart`) — học liệu tĩnh (ETag phía server) ⇒ giữ trong bộ nhớ suốt phiên như
/// `staleTime: Infinity` của web: `ref.keepAlive()` CHỈ sau khi tải thành công (lỗi 503/mạng thì autoDispose bình
/// thường để nút "Thử lại"/mở lại tab tải mới). Không theo người dùng: nội dung giống nhau với mọi tài khoản.
final pinyinChartProvider = FutureProvider.autoDispose<PinyinChart>((ref) async {
  final chart = await getPinyinChart(ref.watch(chineseDioProvider));
  ref.keepAlive();
  return chart;
});

/// Hướng dẫn (`GET /pinyin/guide`) — như [pinyinChartProvider].
final pinyinGuideProvider = FutureProvider.autoDispose<PinyinGuide>((ref) async {
  final guide = await getPinyinGuide(ref.watch(chineseDioProvider));
  ref.keepAlive();
  return guide;
});

/// Thống kê thanh (`GET /pinyin/tone-stats`) — family THEO ID NGƯỜI DÙNG (cùng mẫu `progressOverviewByUserProvider`):
/// đổi tài khoản ⇒ instance mới (không mang số người trước); làm mới cùng người ⇒ giữ số cũ trong lúc tải.
/// `staleTime: 0` ở web ⇒ ở đây autoDispose + `invalidateToneStats()` sau khi nộp bài.
final toneStatsByUserProvider = FutureProvider.autoDispose.family<ToneStats, String?>((ref, userId) {
  if (userId == null) throw ApiError('Cần đăng nhập để tiếp tục.', status: 401);
  return getToneStats(ref.watch(chineseDioProvider));
});

/// Provider thống kê của người dùng HIỆN TẠI — `ref.watch(ref.watch(toneStatsProvider))`.
final toneStatsProvider = Provider.autoDispose<FutureProvider<ToneStats>>(
  (ref) => toneStatsByUserProvider(ref.watch(userScopeProvider)),
);

/// Làm mới thống kê thanh (sau khi nộp bài — cùng danh sách invalidate với `useSubmitToneDrill` web, kèm
/// `invalidateProgressOverview()` ở nơi gọi).
extension ToneStatsWidgetRefX on WidgetRef {
  void invalidateToneStats() => invalidate(toneStatsByUserProvider);
}
