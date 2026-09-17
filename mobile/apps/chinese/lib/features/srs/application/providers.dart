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
/// dữ liệu cũ. M6 gọi `ref.invalidate(srsSummaryProvider)` sau khi lưu (hạn mức thẻ mới đổi ⇒ số thẻ đến hạn đổi).
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
