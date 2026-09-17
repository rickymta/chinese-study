import 'package:af_chinese/features/system/data/models.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  test('SystemInfo.fromJson đọc đủ trường từ fixture', () {
    final info = SystemInfo.fromJson(loadFixture('system_info.json'));
    expect(info.service, 'chinese-backend');
    expect(info.version, '0.1.0');
    expect(info.environment, 'Development');
    expect(info.serverTimeUtc, DateTime.utc(2026, 9, 17, 8));
    expect(info.toJson()['serverTimeUtc'], '2026-09-17T08:00:00.000Z');
  });

  test('SystemInfo.fromJson thiếu trường (WhenWritingNull) ⇒ mặc định, không ném', () {
    final info = SystemInfo.fromJson({'service': 'identity-service'});
    expect(info.service, 'identity-service');
    expect(info.version, '');
    expect(info.environment, '');
    expect(info.serverTimeUtc, isNull);
  });

  test('SystemService nhãn/tên', () {
    expect(SystemService.chinese.label, 'Tiếng Trung');
    expect(SystemService.identity.serviceName, 'identity-service');
  });
}
