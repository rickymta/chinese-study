import 'package:af_core/af_core.dart';
import 'package:dio/dio.dart';

import 'models.dart';

/// `GET {baseUrl}/system/info`. Truyền `skipErrorRedirect` vì đây là lời gọi KIỂM TRA TRẠNG THÁI nền — service
/// chưa chạy (502 từ gateway) hay 404 không được kéo cả app sang trang lỗi.
Future<SystemInfo> getSystemInfo(Dio client) async {
  final res = await client.get<Object?>('/system/info', options: afOptions(skipErrorRedirect: true));
  final json = asJsonMap(res.data);
  if (json == null) {
    throw ApiError(JsonGuardInterceptor.message, status: res.statusCode, data: res.data);
  }
  return SystemInfo.fromJson(json);
}
