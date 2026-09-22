/// Kiểm tra biểu mẫu tài khoản — thông điệp Y HỆT `schemas.ts` của `@af/utils` (R-A1/R-A2, độ dài cột §5.1.1).
/// Trả `null` khi hợp lệ, ngược lại là thông điệp hiện dưới ô nhập (dùng trực tiếp làm `validator:` của
/// `TextFormField`).
library;

/// Regex email của zod 4 (`z.email()` mặc định) — port nguyên văn để web/mobile chấp nhận cùng tập email.
final RegExp _emailPattern = RegExp(
  r"^(?!\.)(?!.*\.\.)([A-Za-z0-9_'+\-\.]*)[A-Za-z0-9_+-]@([A-Za-z0-9][A-Za-z0-9\-]*\.)+[A-Za-z]{2,}$",
);

/// Email: cắt khoảng trắng, bắt buộc, ≤ 254 ký tự, đúng dạng.
String? validateEmail(String? value) {
  final v = (value ?? '').trim();
  if (v.isEmpty) return 'Vui lòng nhập email';
  if (v.length > 254) return 'Email quá dài (tối đa 254 ký tự)';
  if (!_emailPattern.hasMatch(v)) return 'Email không hợp lệ';
  return null;
}

/// Mật khẩu khi ĐĂNG KÝ/ĐỔI: 8–128 ký tự, KHÔNG bắt buộc ký tự đặc biệt (R-A2, NIST SP 800-63B). Không trim.
String? validatePassword(String? value) {
  final v = value ?? '';
  if (v.isEmpty) return 'Vui lòng nhập mật khẩu';
  if (v.length < 8) return 'Mật khẩu cần ít nhất 8 ký tự';
  if (v.length > 128) return 'Mật khẩu tối đa 128 ký tự';
  return null;
}

/// Mật khẩu khi ĐĂNG NHẬP: chỉ cần "có nhập" — không nhắc luật 8 ký tự để không gợi ý kẻ dò mật khẩu (R-A8).
String? validateLoginPassword(String? value) => (value ?? '').isEmpty ? 'Vui lòng nhập mật khẩu' : null;

/// Nhập lại mật khẩu: bắt buộc và phải khớp [password].
String? validateConfirmPassword(String? value, String password) {
  final v = value ?? '';
  if (v.isEmpty) return 'Vui lòng nhập lại mật khẩu';
  if (v != password) return 'Mật khẩu nhập lại không khớp';
  return null;
}

/// Tên hiển thị 1–100 ký tự (cột `display_name varchar(100)`), đã trim.
String? validateDisplayName(String? value) {
  final v = (value ?? '').trim();
  if (v.isEmpty) return 'Vui lòng nhập tên hiển thị';
  if (v.length > 100) return 'Tên hiển thị tối đa 100 ký tự';
  return null;
}

/// Múi giờ IANA: bắt buộc, ≤ 64 ký tự; tính hợp lệ thật do backend kiểm.
String? validateTimeZone(String? value) {
  final v = (value ?? '').trim();
  if (v.isEmpty) return 'Vui lòng chọn múi giờ';
  if (v.length > 64) return 'Múi giờ không hợp lệ';
  return null;
}
