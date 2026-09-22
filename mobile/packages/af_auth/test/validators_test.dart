import 'package:af_auth/af_auth.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('validateEmail (thông điệp như schemas.ts)', () {
    test('rỗng / quá dài / sai dạng / hợp lệ', () {
      expect(validateEmail(null), 'Vui lòng nhập email');
      expect(validateEmail('   '), 'Vui lòng nhập email');
      expect(validateEmail('${'a' * 250}@b.vn'), 'Email quá dài (tối đa 254 ký tự)');
      expect(validateEmail('khong-phai-email'), 'Email không hợp lệ');
      expect(validateEmail('a@b'), 'Email không hợp lệ');
      expect(validateEmail('.a@b.vn'), 'Email không hợp lệ');
      expect(validateEmail('a..b@b.vn'), 'Email không hợp lệ');
      expect(validateEmail('  ban@vidu.com  '), isNull);
      expect(validateEmail("ten.ho+tag'_@sub.vidu.com"), isNull);
    });
  });

  group('validatePassword / validateLoginPassword / validateConfirmPassword', () {
    test('đăng ký: 8–128 ký tự, không trim', () {
      expect(validatePassword(''), 'Vui lòng nhập mật khẩu');
      expect(validatePassword('1234567'), 'Mật khẩu cần ít nhất 8 ký tự');
      expect(validatePassword('x' * 129), 'Mật khẩu tối đa 128 ký tự');
      expect(validatePassword('12345678'), isNull);
      expect(validatePassword('       8'), isNull);
    });

    test('đăng nhập: chỉ cần có nhập (không lộ luật)', () {
      expect(validateLoginPassword(''), 'Vui lòng nhập mật khẩu');
      expect(validateLoginPassword('1'), isNull);
    });

    test('nhập lại: bắt buộc và khớp', () {
      expect(validateConfirmPassword('', 'abc'), 'Vui lòng nhập lại mật khẩu');
      expect(validateConfirmPassword('abd', 'abc'), 'Mật khẩu nhập lại không khớp');
      expect(validateConfirmPassword('abc', 'abc'), isNull);
    });
  });

  test('validateDisplayName 1–100 (trim)', () {
    expect(validateDisplayName('  '), 'Vui lòng nhập tên hiển thị');
    expect(validateDisplayName('a' * 101), 'Tên hiển thị tối đa 100 ký tự');
    expect(validateDisplayName(' Quân '), isNull);
  });

  test('validateTimeZone bắt buộc, ≤ 64', () {
    expect(validateTimeZone(''), 'Vui lòng chọn múi giờ');
    expect(validateTimeZone('a' * 65), 'Múi giờ không hợp lệ');
    expect(validateTimeZone('Asia/Ho_Chi_Minh'), isNull);
  });
}
