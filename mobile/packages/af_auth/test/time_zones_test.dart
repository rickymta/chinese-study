import 'package:af_auth/af_auth.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('normalizeTimeZone quy bí danh cũ, giữ tên khác (trim)', () {
    expect(normalizeTimeZone('Asia/Saigon'), 'Asia/Ho_Chi_Minh');
    expect(normalizeTimeZone('Asia/Calcutta'), 'Asia/Kolkata');
    expect(normalizeTimeZone('Asia/Katmandu'), 'Asia/Kathmandu');
    expect(normalizeTimeZone('Asia/Rangoon'), 'Asia/Yangon');
    expect(normalizeTimeZone('Europe/Kiev'), 'Europe/Kyiv');
    expect(normalizeTimeZone('  Asia/Tokyo '), 'Asia/Tokyo');
  });

  test('looksLikeTimeZone', () {
    expect(looksLikeTimeZone('Asia/Ho_Chi_Minh'), isTrue);
    expect(looksLikeTimeZone('America/Argentina/Buenos_Aires'), isTrue);
    expect(looksLikeTimeZone('Etc/GMT+7'), isTrue);
    expect(looksLikeTimeZone('UTC'), isTrue);
    expect(looksLikeTimeZone(''), isFalse);
    expect(looksLikeTimeZone('GMT+07:00'), isFalse);
    expect(looksLikeTimeZone('a' * 65), isFalse);
  });

  group('deviceTimeZone (kênh flutter_timezone giả)', () {
    const channel = MethodChannel('flutter_timezone');
    final messenger = TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

    tearDown(() => messenger.setMockMethodCallHandler(channel, null));

    test('plugin trả bí danh cũ ⇒ quy về tên hiện hành', () async {
      messenger.setMockMethodCallHandler(channel, (call) async => 'Asia/Saigon');
      expect(await deviceTimeZone(), 'Asia/Ho_Chi_Minh');
    });

    test('plugin trả map có localizedName ⇒ lấy identifier', () async {
      messenger.setMockMethodCallHandler(
        channel,
        (call) async => {'identifier': 'Asia/Tokyo', 'localizedName': 'Giờ Nhật', 'locale': 'vi'},
      );
      expect(await deviceTimeZone(), 'Asia/Tokyo');
    });

    test('plugin lỗi / không có ⇒ Asia/Ho_Chi_Minh', () async {
      messenger.setMockMethodCallHandler(channel, (call) async => throw PlatformException(code: 'x'));
      expect(await deviceTimeZone(), 'Asia/Ho_Chi_Minh');
      messenger.setMockMethodCallHandler(channel, null);
      expect(await deviceTimeZone(fallback: 'UTC'), 'UTC');
    });

    test('giá trị không giống múi giờ ⇒ fallback', () async {
      messenger.setMockMethodCallHandler(channel, (call) async => 'GMT+07:00');
      expect(await deviceTimeZone(), 'Asia/Ho_Chi_Minh');
    });
  });
}
