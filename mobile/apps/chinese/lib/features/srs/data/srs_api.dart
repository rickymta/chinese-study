import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

// Hợp đồng §6.2. Lời gọi GHI giữ lỗi để màn hình/outbox xử lý tại chỗ (`createApiClient` chỉ điều hướng GET
// 403/404). `summary` chạy NỀN ở mọi màn (huy hiệu nhánh "Ôn tập") ⇒ `skipErrorRedirect`.

/// Số thẻ mỗi lần lấy hàng đợi (1..50).
const kQueueLimitDefault = 20;

/// `GET /me/learning-settings` (quyền `study.use`). `skipErrorRedirect`: lời gọi này chạy NỀN từ mọi màn có nút
/// nghe (nguồn `ttsRate`) — 403/404 không được kéo người dùng khỏi trang đang xem.
Future<LearningSettings> getLearningSettings(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/me/learning-settings', options: afOptions(skipErrorRedirect: true));
  return LearningSettings.fromJson(asJsonMap(res.data));
}

/// `PUT /me/learning-settings` — body đủ 5 trường; `400 VALIDATION` `details` theo tên trường camelCase. Lời gọi ghi
/// giữ lỗi để màn hình báo tại chỗ.
Future<LearningSettings> putLearningSettings(Dio chineseDio, LearningSettings body) async {
  final res = await chineseDio.put<Object?>('/me/learning-settings', data: body.toJson());
  return LearningSettings.fromJson(asJsonMap(res.data));
}

/// `GET /srs/summary` — số thẻ đến hạn/mới/đã ôn hôm nay theo múi giờ người học. Chạy nền (huy hiệu) ⇒ không điều hướng.
Future<SrsSummary> getSrsSummary(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/srs/summary', options: afOptions(skipErrorRedirect: true));
  return SrsSummary.fromJson(asJsonMap(res.data));
}

/// `GET /srs/queue?limit=` (1..50) — hàng đợi theo R7-7 kèm `summary`. 403 (mất `study.use`) ⇒ `/403`.
Future<SrsQueueResponse> getSrsQueue(Dio chineseDio, {int limit = kQueueLimitDefault}) async {
  final res = await chineseDio.get<Object?>('/srs/queue', queryParameters: {'limit': limit.clamp(1, 50)});
  return SrsQueueResponse.fromJson(asJsonMap(res.data));
}

/// `POST /srs/cards/{cardId}/reviews` — idempotent theo [clientReviewId] (R7-8).
/// Lỗi: 400 VALIDATION · 404 · 409 CLIENT_REVIEW_ID_CONFLICT · 422 CARD_SUSPENDED | NEW_CARD_LIMIT_REACHED.
Future<ReviewResponse> postReview(
  Dio chineseDio, {
  required String cardId,
  required String clientReviewId,
  required SrsRating rating,
  required int durationMs,
}) async {
  final res = await chineseDio.post<Object?>(
    '/srs/cards/${Uri.encodeComponent(cardId)}/reviews',
    data: {'clientReviewId': clientReviewId, 'rating': rating.apiValue, 'durationMs': durationMs},
  );
  return ReviewResponse.fromJson(asJsonMap(res.data));
}

/// `POST /srs/cards` — thêm thẻ `source='manual'` (201). 422 UNKNOWN_WORD (`details.wordIds`).
Future<AddCardsResponse> addSrsCards(Dio chineseDio, List<String> wordIds) async {
  final res = await chineseDio.post<Object?>('/srs/cards', data: {'wordIds': wordIds});
  return AddCardsResponse.fromJson(asJsonMap(res.data));
}

/// `PUT /srs/cards/{cardId}/suspension` `{ suspended }` ⇒ 200 thẻ trần sau cập nhật · 404.
Future<SrsCardStatus> setSrsCardSuspension(Dio chineseDio, {required String cardId, required bool suspended}) async {
  final res = await chineseDio.put<Object?>(
    '/srs/cards/${Uri.encodeComponent(cardId)}/suspension',
    data: {'suspended': suspended},
  );
  return SrsCardStatus.fromJson(asJsonMap(res.data));
}
