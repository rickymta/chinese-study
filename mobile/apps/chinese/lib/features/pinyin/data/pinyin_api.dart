import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

// Hợp đồng §6.2 (HĐ45 §6.1). Lời gọi GET ở đây KHÔNG đặt `skipErrorRedirect` (như web): 403 (mất `study.use`) phải
// kéo sang `/403`. 503 `CONTENT_UNAVAILABLE` không bị điều hướng ⇒ màn hình tự hiện lỗi "Học liệu chưa sẵn sàng".
// Lời gọi GHI (nộp bài) giữ lỗi để `DrillResult` báo tại chỗ + "Gửi lại".

/// `GET /pinyin/chart` — bảng thanh mẫu/vận mẫu/âm tiết (ETag phía server; app giữ trong bộ nhớ suốt phiên).
Future<PinyinChart> getPinyinChart(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/pinyin/chart');
  return PinyinChart.fromJson(asJsonMap(res.data));
}

/// `GET /pinyin/guide` — chủ đề hướng dẫn (đã sắp theo `order` trong `PinyinGuide.fromJson`).
Future<PinyinGuide> getPinyinGuide(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/pinyin/guide');
  return PinyinGuide.fromJson(asJsonMap(res.data));
}

/// `GET /pinyin/tone-stats` — thống kê thanh (chuẩn hoá `null`/thiếu khoá — RK41). Không cache.
Future<ToneStats> getToneStats(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/pinyin/tone-stats');
  return ToneStats.fromJson(asJsonMap(res.data));
}

/// `POST /pinyin/tone-drills` — 201 tạo mới, 200 khi `clientSessionId` đã nộp (trả lại cùng kết quả).
/// Lỗi: 400 VALIDATION · 422 UNKNOWN_SYLLABLE | TONE_NOT_AVAILABLE | INVALID_SESSION_TIME · 503 CONTENT_UNAVAILABLE.
Future<SubmitToneDrillResponse> submitToneDrill(Dio chineseDio, SubmitToneDrillRequest body) async {
  final res = await chineseDio.post<Object?>('/pinyin/tone-drills', data: body.toJson());
  return SubmitToneDrillResponse.fromJson(asJsonMap(res.data));
}
