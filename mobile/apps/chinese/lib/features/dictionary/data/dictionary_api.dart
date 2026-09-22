import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

// Hợp đồng F6/F7 §6.1 (`/dictionary/*`, quyền `study.use`). Lỗi chung: 400 `VALIDATION` (`q` > 64 ký tự, `hanzi`
// không phải một chữ Hán), 404 (id/chữ không có), 503 `CONTENT_UNAVAILABLE` (học liệu chưa nạp — báo tại chỗ, không
// điều hướng).

/// `GET /dictionary/search?q=&hsk=&page=&pageSize=` — `q` rỗng ⇒ liệt kê theo lộ trình (`browse`). Không đặt
/// `skipErrorRedirect`: 403 (mất `study.use`) ⇒ `/403` như web; 404 không xảy ra với tìm kiếm.
Future<SearchResponse> searchWords(
  Dio chineseDio, {
  String q = '',
  int? hsk,
  int page = 1,
  int pageSize = kSearchPageSize,
}) async {
  final trimmed = q.trim();
  final res = await chineseDio.get<Object?>(
    '/dictionary/search',
    queryParameters: {
      if (trimmed.isNotEmpty) 'q': trimmed,
      'hsk': ?hsk,
      'page': page,
      'pageSize': pageSize.clamp(1, 100),
    },
  );
  return SearchResponse.fromJson(asJsonMap(res.data));
}

/// `GET /dictionary/words/{id}` — chi tiết từ kèm khối `srs` của người gọi (§6.1).
///
/// [skipErrorRedirect] (mặc định `true`): gọi từ sheet "Xem chi tiết" trong phiên ôn — 404 (từ bị xoá) báo trong
/// sheet, không kéo người học rời phiên. Trang `/tu-dien/:id` truyền `false` để 404 ⇒ `/404` theo quy tắc trang lỗi
/// thống nhất (giữ nút quay lại).
Future<WordDetail> getWord(Dio chineseDio, String id, {bool skipErrorRedirect = true}) async {
  final res = await chineseDio.get<Object?>(
    '/dictionary/words/${Uri.encodeComponent(id)}',
    options: afOptions(skipErrorRedirect: skipErrorRedirect),
  );
  return WordDetail.fromJson(asJsonMap(res.data));
}

/// `GET /dictionary/characters/{hanzi}` — chữ Hán phải URL-encode (`爱` ⇒ `%E7%88%B1`). 404 ⇒ `/404` (interceptor);
/// 400 (không phải một chữ Hán) báo tại chỗ.
Future<CharacterDetail> getCharacter(Dio chineseDio, String hanzi) async {
  final res = await chineseDio.get<Object?>('/dictionary/characters/${Uri.encodeComponent(hanzi)}');
  return CharacterDetail.fromJson(asJsonMap(res.data));
}
