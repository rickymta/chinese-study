import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

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
