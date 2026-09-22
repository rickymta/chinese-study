import 'package:dio/dio.dart';

import 'interceptors.dart';

/// Tạo `Dio` dùng chung (tương đương `createApiClient` của `@af/api` web): `baseUrl`, JSON, Bearer token, làm mới
/// phiên khi 401 (single-flight, gửi lại một lần), JSON guard, chuẩn hoá lỗi thành [ApiError], báo app điều hướng
/// GET 403/404 (trừ `skipErrorRedirect`).
///
/// Thứ tự interceptor QUAN TRỌNG: (1) làm mới token → (2) JSON guard → (3) chuẩn hoá lỗi. Dio chạy `onError`
/// theo thứ tự đăng ký; sau bước chuẩn hoá không còn `DioException` thô để gửi lại.
///
/// - [headers]: header cố định cho mọi request (vd `X-AF-Client` cho identity).
/// - [getAccessToken]: access token trong bộ nhớ (af_auth cung cấp ở M2).
/// - [refresh]: trả access token MỚI, ném [ApiError] 401/403 nếu mất phiên (single-flight thật nằm ở af_auth;
///   client vẫn gộp trong phạm vi của mình).
/// - [onAuthLost]: làm mới thất bại vì 401/403, hoặc gửi lại vẫn 401.
/// - [onErrorRedirect]: GET 403/404 không `skipErrorRedirect` ⇒ app tự `go('/403')` / `push('/404')`.
Dio createApiClient({
  required String baseUrl,
  Duration timeout = const Duration(seconds: 15),
  Map<String, String> headers = const {},
  String? Function()? getAccessToken,
  Future<String> Function()? refresh,
  void Function()? onAuthLost,
  void Function(int status, RequestOptions req)? onErrorRedirect,
}) {
  final dio = Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: timeout,
      sendTimeout: timeout,
      receiveTimeout: timeout,
      headers: {'Accept': 'application/json', ...headers},
      // Body Map/List ⇒ dio tự mã hoá JSON + đặt Content-Type (mặc định của dio, không ép cho GET không body).
      responseType: ResponseType.json,
    ),
  );

  dio.interceptors.addAll([
    AuthRefreshInterceptor(dio: dio, getAccessToken: getAccessToken, refresh: refresh, onAuthLost: onAuthLost),
    JsonGuardInterceptor(),
    ErrorMappingInterceptor(onErrorRedirect: onErrorRedirect),
  ]);
  return dio;
}
