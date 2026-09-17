import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('AppConfig.resolve', () {
    test('web dev không có URL ⇒ dùng origin trang (proxy cùng origin)', () {
      final c = AppConfig.resolve({'AF_ENV': 'dev'}, isWeb: true, base: Uri.parse('http://localhost:3291/#/on-tap'));
      expect(c.env, AfEnv.dev);
      expect(c.identityApiUrl, 'http://localhost:3291/identity/api');
      expect(c.chineseApiUrl, 'http://localhost:3291/chinese/api');
      expect(c.isWeb, isTrue);
      expect(c.isProd, isFalse);
    });

    test('native dev với AF_GATEWAY_URL (bỏ dấu / cuối)', () {
      final c = AppConfig.resolve({'AF_ENV': 'dev', 'AF_GATEWAY_URL': 'http://10.0.2.2:5280/'}, isWeb: false);
      expect(c.identityApiUrl, 'http://10.0.2.2:5280/identity/api');
      expect(c.chineseApiUrl, 'http://10.0.2.2:5280/chinese/api');
    });

    test('URL riêng ưu tiên hơn gateway', () {
      final c = AppConfig.resolve({
        'AF_GATEWAY_URL': 'http://localhost:5280',
        'AF_IDENTITY_API_URL': 'https://id.antfarms.xyz/api',
      }, isWeb: false);
      expect(c.identityApiUrl, 'https://id.antfarms.xyz/api');
      expect(c.chineseApiUrl, 'http://localhost:5280/chinese/api');
    });

    test('AF_ENV rỗng ⇒ dev', () {
      final c = AppConfig.resolve({'AF_ENV': '', 'AF_GATEWAY_URL': 'http://localhost:5280'}, isWeb: false);
      expect(c.env, AfEnv.dev);
    });

    test('prod hợp lệ', () {
      final c = AppConfig.resolve({
        'AF_ENV': 'prod',
        'AF_IDENTITY_API_URL': 'https://id.antfarms.xyz/api',
        'AF_CHINESE_API_URL': 'https://chinese.antfarms.xyz/chinese/api',
      }, isWeb: false);
      expect(c.isProd, isTrue);
    });

    test('native thiếu hết ⇒ AppConfigException thông điệp hướng dẫn', () {
      expect(
        () => AppConfig.resolve({}, isWeb: false),
        throwsA(isA<AppConfigException>().having((e) => e.message, 'message', AppConfig.missingMessage)),
      );
    });

    test('web nhưng base không có origin ⇒ vẫn thiếu', () {
      expect(() => AppConfig.resolve({}, isWeb: true), throwsA(isA<AppConfigException>()));
    });

    test('prod với URL http ⇒ lỗi cấu hình', () {
      expect(
        () => AppConfig.resolve({'AF_ENV': 'prod', 'AF_GATEWAY_URL': 'http://localhost:5280'}, isWeb: false),
        throwsA(isA<AppConfigException>().having((e) => e.message, 'message', contains('https://'))),
      );
    });

    test('AF_ENV lạ ⇒ lỗi', () {
      expect(
        () => AppConfig.resolve({'AF_ENV': 'staging', 'AF_GATEWAY_URL': 'http://x'}, isWeb: false),
        throwsA(isA<AppConfigException>()),
      );
    });
  });
}
