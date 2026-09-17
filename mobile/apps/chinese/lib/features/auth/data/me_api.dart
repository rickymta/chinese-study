import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

/// `GET /chinese/api/me` (HĐG §6.3) — nguồn sự thật DUY NHẤT về vai trò/quyền ở service tiếng Trung (RM-S5).
///
/// `skipErrorRedirect`: lời gọi này chạy NỀN ngay khi mở app — chinese-backend chưa chạy (404/502) hay từ chối (403)
/// thì `AuthController` đã có màn "Không kết nối được"/`/403`; không được kéo cả app sang `/404`. 401 vẫn đi qua làm
/// mới token + `onAuthLost` như mọi request. Fail-closed: thiếu `permissions` ⇒ 0 quyền (không ném).
Future<MeInfo> loadMe(Dio chineseDio) async {
  final res = await chineseDio.get<Object?>('/me', options: afOptions(skipErrorRedirect: true));
  return MeInfo.fromJson(asJsonMap(res.data));
}
