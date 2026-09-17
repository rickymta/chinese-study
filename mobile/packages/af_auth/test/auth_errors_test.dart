import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  ApiError err(int status, String code, {Map<String, Object?>? details, String message = 'Lỗi máy chủ'}) =>
      ApiError(message, status: status, code: code, details: details);

  test('INVALID_CREDENTIALS / 401 không mã ⇒ "Email hoặc mật khẩu không đúng."', () {
    expect(describeAuthError(err(401, 'INVALID_CREDENTIALS')).message, 'Email hoặc mật khẩu không đúng.');
    expect(describeAuthError(ApiError('x', status: 401)).message, 'Email hoặc mật khẩu không đúng.');
  });

  test('ACCOUNT_LOCKED kèm lockedUntil ⇒ giờ địa phương HH:mm; thiếu ⇒ 15 phút', () {
    final until = DateTime.utc(2026, 9, 17, 8, 5);
    final local = until.toLocal();
    final hhmm = '${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
    final v = describeAuthError(err(423, 'ACCOUNT_LOCKED', details: {'lockedUntil': until.toIso8601String()}));
    expect(v.message, 'Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau $hhmm.');
    expect(
      describeAuthError(err(423, 'ACCOUNT_LOCKED')).message,
      'Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.',
    );
    expect(formatLockedUntil('không phải ngày'), isNull);
  });

  test('EMAIL_TAKEN gắn lỗi ô email; INVALID_TIME_ZONE ô timeZone; WRONG_PASSWORD/PASSWORD_UNCHANGED', () {
    final taken = describeAuthError(err(409, 'EMAIL_TAKEN'));
    expect(taken['email'], 'Email này đã được đăng ký.');
    expect(taken.message, contains('đã được đăng ký'));
    expect(describeAuthError(err(422, 'INVALID_TIME_ZONE'))['timeZone'], 'Múi giờ không hợp lệ.');
    expect(describeAuthError(err(422, 'WRONG_PASSWORD'))['currentPassword'], 'Mật khẩu hiện tại không đúng.');
    expect(
      describeAuthError(err(422, 'PASSWORD_UNCHANGED'))['newPassword'],
      'Mật khẩu mới phải khác mật khẩu hiện tại.',
    );
  });

  test('ACCOUNT_DISABLED, REGISTRATION_CLOSED, ORIGIN_NOT_ALLOWED, RATE_LIMITED, 429 không mã', () {
    expect(
      describeAuthError(err(403, 'ACCOUNT_DISABLED')).message,
      'Tài khoản đã bị khoá. Vui lòng liên hệ quản trị viên.',
    );
    expect(describeAuthError(err(403, 'REGISTRATION_CLOSED')).message, 'Hệ thống hiện chưa mở đăng ký tài khoản mới.');
    expect(describeAuthError(err(403, 'ORIGIN_NOT_ALLOWED')).message, contains('nguồn gửi không hợp lệ'));
    expect(describeAuthError(err(429, 'RATE_LIMITED')).message, contains('quá nhanh'));
    expect(describeAuthError(ApiError('x', status: 429)).message, contains('quá nhanh'));
  });

  test('VALIDATION với details ⇒ lỗi theo ô + thông điệp chung; mã lạ ⇒ giữ error máy chủ; lỗi mạng', () {
    final v = describeAuthError(
      err(
        400,
        'VALIDATION',
        details: {
          'email': ['Email không hợp lệ.'],
          'X-AF-Client': ['Thiếu hoặc sai định dạng header X-AF-Client.'],
        },
      ),
    );
    expect(v['email'], 'Email không hợp lệ.');
    expect(v['X-AF-Client'], isNotNull);
    expect(v.message, 'Vui lòng kiểm tra lại các ô được đánh dấu.');

    expect(describeAuthError(err(500, 'LA_LAM', message: 'Máy chủ nói gì đó')).message, 'Máy chủ nói gì đó');
    expect(describeAuthError(ApiError.network()).message, contains('Không kết nối được máy chủ'));
    expect(describeAuthError(StateError('bug')).message, 'Đã xảy ra lỗi không xác định.');
  });
}
