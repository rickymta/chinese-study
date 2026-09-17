import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('buildClientHeader', () {
    test('ghép đúng định dạng backend chấp nhận', () {
      final h = buildClientHeader(appSlug: 'chinese-mobile', version: '0.1.0+1', platform: ClientPlatform.android);
      expect(h, 'chinese-mobile/0.1.0+1 (android)');
      expect(clientHeaderPattern.hasMatch(h), isTrue);
    });

    test('loại ký tự lạ, cắt độ dài, vẫn khớp pattern', () {
      final h = buildClientHeader(
        appSlug: 'Chinese Mobile!!' * 5,
        version: '1.0.0 (dev)' * 8,
        platform: ClientPlatform.web,
      );
      expect(clientHeaderPattern.hasMatch(h), isTrue, reason: h);
      expect(h, endsWith(' (web)'));
    });

    test('rỗng ⇒ giá trị an toàn', () {
      final h = buildClientHeader(appSlug: '', version: '', platform: ClientPlatform.ios);
      expect(h, 'app/0 (ios)');
    });
  });

  test('resolveClientHeader không có plugin ⇒ dùng fallback, không ném', () async {
    TestWidgetsFlutterBinding.ensureInitialized();
    final h = await resolveClientHeader(
      appSlug: 'chinese-mobile',
      fallbackVersion: '0.1.0+1',
      platform: ClientPlatform.web,
    );
    expect(clientHeaderPattern.hasMatch(h), isTrue, reason: h);
    expect(h, endsWith(' (web)'));
  });
}
