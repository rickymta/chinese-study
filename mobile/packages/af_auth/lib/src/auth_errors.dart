import 'package:af_core/af_core.dart';

/// Kết quả diễn giải lỗi xác thực: thông điệp chung + lỗi gắn vào từng ô (khoá camelCase: `email`, `password`,
/// `displayName`, `timeZone`, `currentPassword`, `newPassword`). Port `authErrors.ts` của `@af/auth`.
class AuthErrorView {
  const AuthErrorView({required this.message, required this.fieldErrors, required this.error});

  final String message;
  final Map<String, String> fieldErrors;
  final ApiError error;

  String? operator [](String field) => fieldErrors[field];
}

/// `HH:mm` giờ địa phương của mốc ISO; chuỗi không hợp lệ ⇒ null.
String? formatLockedUntil(Object? value) {
  if (value is! String) return null;
  final t = DateTime.tryParse(value);
  if (t == null) return null;
  final local = t.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${two(local.hour)}:${two(local.minute)}';
}

/// Diễn giải lỗi của identity-service (mã §6.0/§6.1) thành lời tiếng Việt để hiện tại chỗ trên màn đăng nhập/đăng
/// ký/đổi mật khẩu. Mã lạ ⇒ dùng `error` của máy chủ (đã nằm trong `ApiError.message`) hoặc thông điệp mặc định.
AuthErrorView describeAuthError(Object err) {
  final error = ApiError.from(err);
  final fieldErrors = <String, String>{};
  for (final entry in (error.details ?? const <String, Object?>{}).entries) {
    final first = error.fieldErrors(entry.key);
    if (first.isNotEmpty) fieldErrors[entry.key] = first.first;
  }

  var message = error.message;
  switch (error.code) {
    case 'INVALID_CREDENTIALS':
      message = 'Email hoặc mật khẩu không đúng.';
    case 'ACCOUNT_LOCKED':
      final until = formatLockedUntil(error.details?['lockedUntil']);
      message = until != null
          ? 'Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau $until.'
          : 'Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.';
    case 'ACCOUNT_DISABLED':
      message = 'Tài khoản đã bị khoá. Vui lòng liên hệ quản trị viên.';
    case 'REGISTRATION_CLOSED':
      message = 'Hệ thống hiện chưa mở đăng ký tài khoản mới.';
    case 'ORIGIN_NOT_ALLOWED':
      message = 'Yêu cầu bị từ chối vì nguồn gửi không hợp lệ. Hãy mở ứng dụng bằng đúng bản chính thức.';
    case 'EMAIL_TAKEN':
      fieldErrors.putIfAbsent('email', () => 'Email này đã được đăng ký.');
      message = 'Email này đã được đăng ký. Bạn có thể đăng nhập hoặc dùng email khác.';
    case 'INVALID_TIME_ZONE':
      fieldErrors.putIfAbsent('timeZone', () => 'Múi giờ không hợp lệ.');
      message = 'Múi giờ không hợp lệ.';
    case 'WRONG_PASSWORD':
      fieldErrors.putIfAbsent('currentPassword', () => 'Mật khẩu hiện tại không đúng.');
      message = 'Mật khẩu hiện tại không đúng.';
    case 'PASSWORD_UNCHANGED':
      fieldErrors.putIfAbsent('newPassword', () => 'Mật khẩu mới phải khác mật khẩu hiện tại.');
      message = 'Mật khẩu mới phải khác mật khẩu hiện tại.';
    case 'RATE_LIMITED':
      message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.';
    case 'REFRESH_INVALID':
      message = 'Phiên đăng nhập không còn hợp lệ, vui lòng đăng nhập lại.';
    case 'VALIDATION':
      if (fieldErrors.isNotEmpty) message = 'Vui lòng kiểm tra lại các ô được đánh dấu.';
    default:
      if (error.status == 401) {
        message = 'Email hoặc mật khẩu không đúng.';
      } else if (error.status == 429) {
        message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.';
      }
  }

  return AuthErrorView(message: message, fieldErrors: fieldErrors, error: error);
}
