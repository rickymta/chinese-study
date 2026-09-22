import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

/// `GET /progress/overview` (§6.4, cần `study.use`) — tổng quan tiến độ theo múi giờ hồ sơ.
///
/// Trang chủ là điểm vào của app và nguồn của huy hiệu "Ôn tập": `skipErrorRedirect` để lỗi 403/404 không đẩy người
/// dùng sang trang lỗi mà trang tự hiện `ErrorView` giải thích (giống `getProgressOverview` web).
Future<ProgressOverview> getProgressOverview(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/progress/overview', options: afOptions(skipErrorRedirect: true));
  return ProgressOverview.fromJson(asJsonMap(res.data));
}
