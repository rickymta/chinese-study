import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../api/clients.dart';
import '../../../core/session_scope.dart';
import '../../progress/application/providers.dart';
import '../../srs/application/providers.dart';
import '../data/lessons_api.dart';
import '../data/models.dart';

/// Danh sách bài (`GET /lessons`) — family THEO ID NGƯỜI DÙNG (cùng mẫu `progressOverviewByUserProvider`): đổi tài
/// khoản ⇒ instance mới; làm mới cùng người ⇒ giữ danh sách cũ trong lúc tải. `staleTime: 1 phút` +
/// `refetchOnWindowFocus` của web ⇒ ở đây autoDispose + [invalidateLessons] sau start/nộp quiz + kéo-để-làm-mới.
final lessonsByUserProvider = FutureProvider.autoDispose.family<LessonListResponse, String?>((ref, userId) {
  if (userId == null) throw ApiError('Cần đăng nhập để tiếp tục.', status: 401);
  return getLessons(ref.watch(chineseDioProvider));
});

/// Provider danh sách bài của người dùng HIỆN TẠI — `ref.watch(ref.watch(lessonsProvider))`.
final lessonsProvider = Provider.autoDispose<FutureProvider<LessonListResponse>>(
  (ref) => lessonsByUserProvider(ref.watch(userScopeProvider)),
);

/// Chi tiết bài theo slug (`GET /lessons/{slug}`) — `AsyncNotifier` để ghi thẳng `progress` server trả về sau
/// `start`/nộp quiz vào cache (như `setQueryData` web), không GET lại. autoDispose: rời trang ⇒ huỷ. `build` theo
/// `userScopeProvider` ⇒ đổi người tự dựng lại (trang bài chỉ mở khi đã đăng nhập nên không cần `unwrapPrevious`).
class LessonDetailNotifier extends AsyncNotifier<LessonDetail> {
  LessonDetailNotifier(this.slug);

  /// Tham số family (Riverpod 3 truyền qua constructor).
  final String slug;

  @override
  Future<LessonDetail> build() async {
    ref.watch(userScopeProvider);
    return getLesson(ref.watch(chineseDioProvider), slug);
  }

  /// Ghi tiến độ mới vào bản đang giữ (sau `POST /start` hoặc kết quả quiz) — không có dữ liệu thì bỏ qua.
  void applyProgress(LessonProgress progress) {
    final current = state.value;
    if (!ref.mounted || current == null) return;
    state = AsyncData(current.copyWith(progress: progress));
  }
}

final lessonDetailProvider = AsyncNotifierProvider.autoDispose.family<LessonDetailNotifier, LessonDetail, String>(
  LessonDetailNotifier.new,
);

/// Lịch sử 5 lần làm gần nhất (`GET /lessons/{id}/quiz-attempts?limit=5`) — chỉ tải khi màn mở đầu quiz hiện (widget
/// mới watch). Theo người dùng; làm mới sau khi nộp.
final quizAttemptsProvider = FutureProvider.autoDispose.family<QuizAttemptsResponse, String>((ref, lessonId) {
  ref.watch(userScopeProvider);
  return getQuizAttempts(ref.watch(chineseDioProvider), lessonId);
});

/// Làm mới sau khi bắt đầu bài / nộp quiz — cùng danh sách invalidate với `useSubmitQuiz` web: danh sách bài, lịch
/// sử lần làm, tóm tắt SRS (bài vừa thêm thẻ ⇒ huy hiệu "Ôn tập" đổi), tổng quan tiến độ.
extension LessonsWidgetRefX on WidgetRef {
  void invalidateLessons() => invalidate(lessonsByUserProvider);

  /// Sau nộp quiz thành công: [result] mang `progress` mới (ghi thẳng vào chi tiết bài); `firstCompletion` ⇒ tải lại
  /// chi tiết để tab Từ vựng hiện chip "Đang ôn" (`inSrs` đổi).
  void afterQuizSubmitted(String slug, String lessonId, QuizResult result) {
    final progress = result.progress;
    if (progress != null) read(lessonDetailProvider(slug).notifier).applyProgress(progress);
    invalidateLessons();
    invalidate(quizAttemptsProvider(lessonId));
    invalidateSrsSummary();
    invalidateProgressOverview();
    if (result.firstCompletion) invalidate(lessonDetailProvider(slug));
  }

  /// Sau `POST /start` (R-LS12): ghi `progress` vào chi tiết bài + làm mới danh sách (chip "Đang học") + tổng quan
  /// (số bài "đang học dở" ở trang chủ). Không có toast — thao tác ngầm.
  void afterLessonStarted(String slug, LessonProgress progress) {
    read(lessonDetailProvider(slug).notifier).applyProgress(progress);
    invalidateLessons();
    invalidateProgressOverview();
  }
}

// ─── Công tắc hiển thị tab Nội dung (§5.3.1): lưu bền để bật/tắt giữ qua các bài; mặc định BẬT ───

/// Khoá `shared_preferences` — cùng tên với localStorage web (`displayPrefs.ts`).
const kShowPinyinKey = 'af.chinese.lesson.showPinyin';
const kShowViKey = 'af.chinese.lesson.showVi';

/// Trạng thái hai công tắc "Pinyin" / "Nghĩa tiếng Việt".
@immutable
class DisplayPrefs {
  const DisplayPrefs({this.showPinyin = true, this.showVi = true});

  /// Hiện pinyin (ruby trên chữ Hán nội dòng, dòng pinyin ở hội thoại/ví dụ, đề bài quiz).
  final bool showPinyin;

  /// Hiện nghĩa tiếng Việt ở hội thoại/ví dụ.
  final bool showVi;

  DisplayPrefs copyWith({bool? showPinyin, bool? showVi}) =>
      DisplayPrefs(showPinyin: showPinyin ?? this.showPinyin, showVi: showVi ?? this.showVi);

  @override
  bool operator ==(Object other) => other is DisplayPrefs && other.showPinyin == showPinyin && other.showVi == showVi;

  @override
  int get hashCode => Object.hash(showPinyin, showVi);
}

/// Đọc kho lúc dựng (mặc định bật khi chưa lưu/kho hỏng), ghi kho mỗi lần đổi (ghi hỏng ⇒ chỉ mất ghi nhớ công tắc,
/// như web nuốt lỗi localStorage). Không theo người dùng (tuỳ chọn hiển thị của máy, như web).
class DisplayPrefsController extends Notifier<DisplayPrefs> {
  bool _userChanged = false;

  @override
  DisplayPrefs build() {
    unawaited(_restore());
    return const DisplayPrefs();
  }

  Future<void> _restore() async {
    final store = ref.read(keyValueStoreProvider);
    final pinyin = await store.getBool(kShowPinyinKey);
    final vi = await store.getBool(kShowViKey);
    // Người dùng đã bật/tắt trong lúc chờ đọc kho ⇒ giá trị vừa chọn thắng.
    if (!ref.mounted || _userChanged) return;
    state = DisplayPrefs(showPinyin: pinyin ?? true, showVi: vi ?? true);
  }

  Future<void> setShowPinyin(bool v) async {
    _userChanged = true;
    state = state.copyWith(showPinyin: v);
    await ref.read(keyValueStoreProvider).setBool(kShowPinyinKey, v);
  }

  Future<void> setShowVi(bool v) async {
    _userChanged = true;
    state = state.copyWith(showVi: v);
    await ref.read(keyValueStoreProvider).setBool(kShowViKey, v);
  }
}

final displayPrefsProvider = NotifierProvider<DisplayPrefsController, DisplayPrefs>(DisplayPrefsController.new);
