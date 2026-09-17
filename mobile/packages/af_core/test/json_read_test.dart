import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final json = <String, Object?>{
    's': 'chuỗi',
    'n': 3,
    'd': 1.5,
    'dInt': 4.0,
    'b': true,
    'bs': 'false',
    'ns': '12',
    't': '2026-09-16T08:00:00Z',
    'tBad': 'không phải ngày',
    'list': [
      {'id': 1},
      'rác',
      {'id': 2},
    ],
    'prims': ['a', 1, 'b', null],
    'obj': {'k': 'v'},
    'nul': null,
  };

  group('json_read — thiếu khoá coi như null (WhenWritingNull)', () {
    test('readString', () {
      expect(readString(json, 's'), 'chuỗi');
      expect(readString(json, 'n'), '3');
      expect(readString(json, 'thieu'), isNull);
      expect(readString(json, 'nul'), isNull);
      expect(readString(json, 'obj'), isNull);
      expect(readStringOr(json, 'thieu', 'mặc định'), 'mặc định');
      expect(readString(null, 's'), isNull);
    });

    test('readInt', () {
      expect(readInt(json, 'n'), 3);
      expect(readInt(json, 'dInt'), 4);
      expect(readInt(json, 'd'), isNull);
      expect(readInt(json, 'ns'), 12);
      expect(readInt(json, 's'), isNull);
      expect(readIntOr(json, 'thieu', 7), 7);
    });

    test('readDouble', () {
      expect(readDouble(json, 'd'), 1.5);
      expect(readDouble(json, 'n'), 3.0);
      expect(readDouble(json, 'thieu'), isNull);
      expect(readDoubleOr(json, 'thieu', 0.5), 0.5);
    });

    test('readBool', () {
      expect(readBool(json, 'b'), isTrue);
      expect(readBool(json, 'bs'), isFalse);
      expect(readBool(json, 'n'), isNull);
      expect(readBoolOr(json, 'thieu', true), isTrue);
    });

    test('readDateTime trả UTC, chuỗi hỏng ⇒ null', () {
      expect(readDateTime(json, 't'), DateTime.utc(2026, 9, 16, 8));
      expect(readDateTime(json, 't')!.isUtc, isTrue);
      expect(readDateTime(json, 'tBad'), isNull);
      expect(readDateTime(json, 'thieu'), isNull);
    });

    test('readList bỏ phần tử không phải Map / convert null', () {
      final ids = readList(json, 'list', (m) => readInt(m, 'id'));
      expect(ids, [1, 2]);
      expect(readList(json, 'thieu', (m) => m), isEmpty);
      expect(readList(json, 's', (m) => m), isEmpty);
    });

    test('readPrimitiveList lọc theo kiểu', () {
      expect(readPrimitiveList<String>(json, 'prims'), ['a', 'b']);
      expect(readPrimitiveList<int>(json, 'prims'), [1]);
      expect(readPrimitiveList<String>(json, 'thieu'), isEmpty);
    });

    test('readMap / asJsonMap', () {
      expect(readMap(json, 'obj'), {'k': 'v'});
      expect(readMap(json, 's'), isNull);
      expect(asJsonMap({1: 'a'}), {'1': 'a'});
      expect(asJsonMap('x'), isNull);
    });
  });
}
