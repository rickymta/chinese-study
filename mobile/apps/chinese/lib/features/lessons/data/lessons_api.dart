import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

// Hợp đồng F8–F11 §6.1 (`/lessons/*`, quyền `study.use`). Lời gọi GET KHÔNG đặt `skipErrorRedirect` (như web): 403
// (mất `study.use`) và 404 (bài không published — R-LS1) đi tới trang lỗi dùng chung qua interceptor. Lời gọi GHI
// (start, nộp quiz) giữ lỗi để màn hình báo tại chỗ. 503 `CONTENT_UNAVAILABLE` không bị điều hướng ⇒ màn hình tự
// hiện "Học liệu chưa sẵn sàng".

/// Giới hạn lịch sử lần làm của server (1..20), mặc định 5.
const kQuizAttemptsLimitDefault = 5;

/// `GET /lessons` — danh sách bài `published` sắp theo `orderIndex`, kèm tiến độ + `nextLessonSlug`.
Future<LessonListResponse> getLessons(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/lessons');
  return LessonListResponse.fromJson(asJsonMap(res.data));
}

/// `GET /lessons/{slug}` — chi tiết bài (không có đáp án quiz, R-LS10). 404 khi không published ⇒ `/404`.
Future<LessonDetail> getLesson(Dio chineseDio, String slug) async {
  final res = await chineseDio.get<Object?>('/lessons/${Uri.encodeComponent(slug)}');
  return LessonDetail.fromJson(asJsonMap(res.data));
}

/// `POST /lessons/{id}/start` — tạo `lesson_progress(in_progress)` nếu chưa có; idempotent (R-LS12).
Future<LessonProgress> startLesson(Dio chineseDio, String lessonId) async {
  final res = await chineseDio.post<Object?>('/lessons/${Uri.encodeComponent(lessonId)}/start');
  return LessonProgress.fromJson(asJsonMap(res.data) ?? const {});
}

/// `POST /lessons/{id}/quiz-attempts` — chấm ở server; 201 lần đầu, 200 khi gửi lại cùng `clientAttemptId`.
/// Lỗi: 400 VALIDATION · 404 · 409 DUPLICATE_ATTEMPT_ID · 422 QUIZ_EMPTY | QUIZ_CHANGED (R-LS8).
Future<QuizResult> submitQuiz(Dio chineseDio, String lessonId, SubmitQuizRequest body) async {
  final res = await chineseDio.post<Object?>(
    '/lessons/${Uri.encodeComponent(lessonId)}/quiz-attempts',
    data: body.toJson(),
  );
  return QuizResult.fromJson(asJsonMap(res.data));
}

/// `GET /lessons/{id}/quiz-attempts?limit=` (1–20) — lịch sử lần làm, mới nhất trước, không kèm chi tiết câu.
Future<QuizAttemptsResponse> getQuizAttempts(
  Dio chineseDio,
  String lessonId, {
  int limit = kQuizAttemptsLimitDefault,
}) async {
  final res = await chineseDio.get<Object?>(
    '/lessons/${Uri.encodeComponent(lessonId)}/quiz-attempts',
    queryParameters: {'limit': limit.clamp(1, 20)},
  );
  return QuizAttemptsResponse.fromJson(asJsonMap(res.data));
}
