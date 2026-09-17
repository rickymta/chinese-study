import 'dart:async';

import 'package:dio/dio.dart';

import 'api_error.dart';
import 'request_options_ext.dart';

/// Bốn endpoint phiên của identity (web `/auth/*` lẫn mobile `/auth/mobile/*`) — 401 ở đây là câu trả lời thật
/// (sai mật khẩu, token hỏng), không kéo theo làm mới. Các endpoint khác dưới `/auth/*` (vd `/auth/mobile/password`
/// cần Bearer) vẫn được làm mới + gửi lại như bình thường.
final _authUrlPattern = RegExp(r'(^|/)auth/(mobile/)?(login|register|refresh|logout)(\?|$)');

bool isAuthUrl(String? url) => url != null && _authUrlPattern.hasMatch(url);

/// (1) Gắn Bearer + làm mới phiên khi 401 rồi gửi lại ĐÚNG MỘT lần (single-flight trong phạm vi client).
///
/// Phải đăng ký TRƯỚC [ErrorMappingInterceptor] (bài học web F1): interceptor này cần `DioException` thô còn
/// `requestOptions` để gửi lại; lần gửi lại đi qua toàn bộ chuỗi interceptor nên lỗi của nó đã là [ApiError].
class AuthRefreshInterceptor extends Interceptor {
  AuthRefreshInterceptor({required this.dio, this.getAccessToken, this.refresh, this.onAuthLost});

  final Dio dio;
  final String? Function()? getAccessToken;
  final Future<String> Function()? refresh;
  final void Function()? onAuthLost;

  Future<String>? _refreshing;

  Future<String> _refreshOnce() {
    final existing = _refreshing;
    if (existing != null) return existing;
    final future = refresh!().whenComplete(() => _refreshing = null);
    _refreshing = future;
    return future;
  }

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final token = getAccessToken?.call();
    if (token != null && token.isNotEmpty && !options.headers.containsKey('Authorization')) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final req = err.requestOptions;
    if (refresh == null || err.response?.statusCode != 401 || req.skipAuthRefresh || isAuthUrl(req.path)) {
      return handler.next(err);
    }

    String token;
    try {
      token = await _refreshOnce();
    } on Object catch (refreshErr) {
      // Chỉ coi là MẤT PHIÊN khi identity từ chối refresh (401 REFRESH_INVALID / 403 ACCOUNT_DISABLED).
      // Lỗi mạng/5xx lúc refresh: không đá người dùng ra — token cũ có thể vẫn còn hạn, lần 401 sau sẽ thử lại.
      final apiErr = refreshErr is ApiError ? refreshErr : null;
      if (apiErr != null && (apiErr.status == 401 || apiErr.status == 403)) onAuthLost?.call();
      return handler.next(err); // trả lỗi 401 gốc của request ban đầu (đi tiếp sang bước chuẩn hoá)
    }

    // Gửi lại ĐÚNG MỘT lần với token mới; lần này vẫn 401 ⇒ coi như mất phiên.
    final retryOptions = req.copyWith(
      headers: {...req.headers, 'Authorization': 'Bearer $token'},
      extra: {...req.extra, kSkipAuthRefresh: true},
    );
    try {
      final response = await dio.fetch<Object?>(retryOptions);
      handler.resolve(response);
    } on DioException catch (retryErr) {
      if (retryErr is ApiError && retryErr.status == 401) onAuthLost?.call();
      // Lỗi lần gửi lại đã qua chuỗi interceptor (là ApiError) ⇒ trả thẳng, không chạy lại các interceptor sau.
      handler.reject(retryErr);
    }
  }
}

/// (2) JSON guard: response 2xx của request mong JSON mà `content-type` không chứa `json` và thân khác rỗng ⇒ lỗi rõ.
///
/// Lý do (RK-M10): proxy dev của Flutter rơi về `index.html` (200 text/html) khi gateway tắt; URL cấu hình sai
/// cũng trả HTML. Không chặn ⇒ lỗi parse khó hiểu ở tầng model.
class JsonGuardInterceptor extends Interceptor {
  static const message = 'Phản hồi máy chủ không hợp lệ — kiểm tra cấu hình AF_*_API_URL hoặc proxy dev.';

  @override
  void onResponse(Response<Object?> response, ResponseInterceptorHandler handler) {
    final status = response.statusCode ?? 0;
    final expectsJson = response.requestOptions.responseType == ResponseType.json;
    if (!expectsJson || status == 204 || status < 200 || status >= 300) return handler.next(response);

    final contentType = (response.headers.value(Headers.contentTypeHeader) ?? '').toLowerCase();
    final data = response.data;
    final bodyNotEmpty = switch (data) {
      null => false,
      final String s => s.trim().isNotEmpty,
      final List<int> b => b.isNotEmpty,
      _ => true,
    };
    if (!contentType.contains('json') && bodyNotEmpty) {
      return handler.reject(
        ApiError(message, status: status, data: data, requestOptions: response.requestOptions, response: response),
      );
    }
    handler.next(response);
  }
}

/// (3) Chuẩn hoá `DioException` → [ApiError]; GET 403/404 (không `skipErrorRedirect`) ⇒ báo app điều hướng.
class ErrorMappingInterceptor extends Interceptor {
  ErrorMappingInterceptor({this.onErrorRedirect});

  final void Function(int status, RequestOptions req)? onErrorRedirect;

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err is ApiError) return handler.reject(err);
    final apiErr = ApiError.fromDioException(err);
    final req = err.requestOptions;
    final status = apiErr.status;
    // CHỈ áp cho GET — request GHI giữ nguyên lỗi để màn hình đang thao tác tự báo tại chỗ.
    if ((status == 403 || status == 404) && !req.skipErrorRedirect && req.method.toUpperCase() == 'GET') {
      onErrorRedirect?.call(status!, req);
    }
    handler.reject(apiErr);
  }
}
