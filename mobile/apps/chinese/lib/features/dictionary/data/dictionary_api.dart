import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

/// `GET /dictionary/words/{id}` (quyền `study.use`) — chi tiết từ kèm khối `srs` của người gọi (§6.1).
/// `skipErrorRedirect`: gọi từ sheet "Xem chi tiết" trong phiên ôn — 404 (từ bị xoá) báo trong sheet, không kéo người
/// học rời phiên.
Future<WordDetail> getWord(Dio chineseDio, String id) async {
  final res = await chineseDio.get<Object?>(
    '/dictionary/words/${Uri.encodeComponent(id)}',
    options: afOptions(skipErrorRedirect: true),
  );
  return WordDetail.fromJson(asJsonMap(res.data));
}
