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
  test('normalizeTimeZoneList: quy bí danh, bỏ giá trị lạ, gộp extra, bỏ trùng, sắp tên', () {
    final out = normalizeTimeZoneList(
      ['Asia/Saigon', 'Asia/Tokyo', 'GMT+07:00', 'Asia/Tokyo', ''],
      extra: ['Europe/Kiev', 'Asia/Ho_Chi_Minh'],
    );
    expect(out, ['Asia/Ho_Chi_Minh', 'Asia/Tokyo', 'Europe/Kyiv']);
  });

  test('matchesTimeZoneQuery: không phân biệt hoa thường, `_` ≡ khoảng trắng, thứ tự từ tự do', () {
    const hcm = 'Asia/Ho_Chi_Minh';
    for (final q in ['Ho_Chi', 'ho chi', 'ho chi minh', 'asia/ho', 'minh ho chi', '', '  ', 'HO-CHI']) {
      expect(matchesTimeZoneQuery(hcm, q), isTrue, reason: 'query "$q"');
    }
    expect(matchesTimeZoneQuery(hcm, 'tokyo'), isFalse);
    expect(matchesTimeZoneQuery(hcm, 'ho chi tokyo'), isFalse);
    expect(matchesTimeZoneQuery('UTC', 'utc'), isTrue);
  });

  test('kFallbackTimeZones có Asia/Ho_Chi_Minh và toàn giá trị hợp lệ', () {
    expect(kFallbackTimeZones, contains('Asia/Ho_Chi_Minh'));
    expect(kFallbackTimeZones.every(looksLikeTimeZone), isTrue);
  });

  group('listTimeZones (kênh flutter_timezone giả)', () {
    const channel = MethodChannel('flutter_timezone');
    final messenger = TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

    tearDown(() => messenger.setMockMethodCallHandler(channel, null));

    test('plugin trả danh sách ⇒ chuẩn hoá + gộp extra', () async {
      messenger.setMockMethodCallHandler(
        channel,
        (call) async => ['Asia/Saigon', 'Asia/Tokyo', 'Europe/Paris', 'America/New_York', 'UTC', 'Asia/Seoul'],
      );
      final out = await listTimeZones(extra: ['Asia/Bangkok']);
      expect(out, contains('Asia/Ho_Chi_Minh'));
      expect(out, isNot(contains('Asia/Saigon')));
      expect(out, contains('Asia/Bangkok'));
      expect(out, orderedEquals([...out]..sort()));
    });

    test('plugin lỗi hoặc chỉ trả 1 giá trị (Safari cũ) ⇒ danh sách dự phòng', () async {
      messenger.setMockMethodCallHandler(channel, (call) async => throw PlatformException(code: 'x'));
      expect(await listTimeZones(), containsAll(kFallbackTimeZones));
      messenger.setMockMethodCallHandler(channel, (call) async => ['Asia/Tokyo']);
      final out = await listTimeZones();
      expect(out, containsAll(kFallbackTimeZones));
      expect(out.length, kFallbackTimeZones.length);
    });
  });
}
