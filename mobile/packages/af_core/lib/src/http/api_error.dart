import 'package:dio/dio.dart';

/// Lỗi API đã chuẩn hoá — [message] luôn là tiếng Việt đọc được; [status]/[code]/[details] để màn hình rẽ nhánh.
///
/// Kế thừa [DioException] có chủ đích: dio chỉ ném được `DioException` ra khỏi `fetch`, nên interceptor lỗi của
/// [createApiClient] `reject` bằng chính đối tượng này ⇒ nơi gọi bắt thẳng `on ApiError catch (e)` mà không cần
/// bóc `DioException.error`. Khi tự tạo ngoài dio (JSON guard, af_auth...) không cần truyền `requestOptions`.
class ApiError extends DioException {
  ApiError(
    String message, {
    this.status,
    this.code,
    this.details,
    this.data,
    RequestOptions? requestOptions,
    super.response,
    super.type,
    Object? cause,
    super.stackTrace,
  }) : super(requestOptions: requestOptions ?? RequestOptions(), message: message, error: cause);

  /// Lỗi mạng tự tạo (vd af_auth báo không tới được identity) — `isNetwork == true`.
  factory ApiError.network([String message = 'Không kết nối được máy chủ. Kiểm tra mạng hoặc dịch vụ chưa chạy.']) =>
      ApiError(message, type: DioExceptionType.connectionError);

  /// Thông điệp tiếng Việt (không bao giờ null — khác `DioException.message`).
  @override
  String get message => super.message ?? 'Đã xảy ra lỗi không xác định.';

  /// Mã HTTP (null khi lỗi mạng/timeout).
  final int? status;

  /// Mã lỗi nghiệp vụ trong thân lỗi §6.0 (`REFRESH_INVALID`, `VALIDATION`...).
  final String? code;

  /// `details` của thân lỗi §6.0, vd `{ "email": ["Email đã được dùng."] }` khi `VALIDATION`.
  final Map<String, Object?>? details;

  /// Thân response thô (nếu có) — cho luồng cần đọc cấu trúc riêng.
  final Object? data;

  /// Danh sách thông điệp lỗi của một trường trong [details] (rỗng nếu không có).
  List<String> fieldErrors(String field) {
    final v = details?[field];
    if (v is List) return v.map((e) => e.toString()).toList();
    if (v is String) return [v];
    return const [];
  }

  /// Lỗi MẠNG thật: dio loại `connectionError`/timeout và không có response. Lỗi lập trình (`TypeError`...) bọc qua
  /// [from] hoặc `ApiError` tự tạo không truyền `type` ⇒ `false` (không hiện "Không kết nối được máy chủ" oan).
  bool get isNetwork => response == null && status == null && kNetworkErrorTypes.contains(type);
  bool get isUnauthorized => status == 401;
  bool get isForbidden => status == 403;
  bool get isNotFound => status == 404;

  /// Chuyển bất kỳ lỗi nào thành [ApiError] để hiển thị: [ApiError] giữ nguyên; [DioException] khác ⇒ chuẩn hoá theo
  /// cùng luật với interceptor; còn lại ⇒ thông điệp chung (không lộ chi tiết kỹ thuật cho người học).
  static ApiError from(Object error) {
    if (error is ApiError) return error;
    if (error is DioException) return fromDioException(error);
    return ApiError('Đã xảy ra lỗi không xác định.', cause: error);
  }

  /// Dựng thông điệp tiếng Việt từ lỗi dio — ưu tiên thân lỗi §6.0, rồi tới lỗi mạng/timeout (y hệt `toApiError` web).
  static ApiError fromDioException(DioException err) {
    final status = err.response?.statusCode;
    final body = err.response?.data;
    final bodyObj = body is Map ? body : null;
    final bodyError = bodyObj?['error'];

    final String message;
    if (bodyError != null && bodyError.toString().isNotEmpty) {
      message = bodyError.toString();
    } else if (err.type == DioExceptionType.connectionTimeout ||
        err.type == DioExceptionType.sendTimeout ||
        err.type == DioExceptionType.receiveTimeout) {
      message = 'Máy chủ phản hồi quá lâu, vui lòng thử lại.';
    } else if (err.response == null) {
      message = 'Không kết nối được máy chủ. Kiểm tra mạng hoặc dịch vụ chưa chạy.';
    } else if (status == 502 || status == 503 || status == 504) {
      message = 'Dịch vụ chưa sẵn sàng (gateway không tới được service).';
    } else if (status == 429) {
      message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.';
    } else if (status == 404) {
      message = 'Không tìm thấy tài nguyên.';
    } else if (status == 403) {
      message = 'Bạn không có quyền thực hiện thao tác này.';
    } else if (status == 401) {
      message = 'Cần đăng nhập để tiếp tục.';
    } else {
      final raw = err.message;
      message = (raw == null || raw.isEmpty) ? 'Đã xảy ra lỗi không xác định.' : raw;
    }

    final code = bodyObj?['code'];
    final details = bodyObj?['details'];
    return ApiError(
      message,
      status: status,
      code: code?.toString(),
      details: details is Map ? details.map((k, v) => MapEntry(k.toString(), v)) : null,
      data: body,
      requestOptions: err.requestOptions,
      response: err.response,
      type: err.type,
      cause: err.error,
      stackTrace: err.stackTrace,
    );
  }

  @override
  String toString() => 'ApiError($status${code == null ? '' : ' $code'}): $message';
}

/// Các loại `DioExceptionType` được coi là lỗi mạng.
const kNetworkErrorTypes = {
  DioExceptionType.connectionError,
  DioExceptionType.connectionTimeout,
  DioExceptionType.sendTimeout,
  DioExceptionType.receiveTimeout,
};

bool isApiError(Object? err) => err is ApiError;
