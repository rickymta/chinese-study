import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('uuidV4 đúng định dạng RFC 4122 v4, chữ thường', () {
    for (var i = 0; i < 200; i++) {
      final id = uuidV4();
      expect(id, hasLength(36));
      expect(isUuidV4(id), isTrue, reason: id);
    }
  });

  test('uuidV4 không trùng trong 1000 lần', () {
    final ids = {for (var i = 0; i < 1000; i++) uuidV4()};
    expect(ids, hasLength(1000));
  });

  test('isUuidV4 từ chối chuỗi sai', () {
    expect(isUuidV4(''), isFalse);
    expect(isUuidV4('123e4567-e89b-12d3-a456-426614174000'), isFalse); // version 1
    expect(isUuidV4('123E4567-E89B-42D3-A456-426614174000'), isFalse); // chữ hoa
    expect(isUuidV4('123e4567-e89b-42d3-a456-426614174000'), isTrue);
  });
}
