import 'package:af_chinese/features/dictionary/data/models.dart';
import 'package:af_chinese/features/dictionary/domain/character_reading.dart';
import 'package:af_chinese/features/dictionary/domain/pos.dart';
import 'package:af_chinese/features/dictionary/domain/source_labels.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('posLabels', () {
    test('mã quen ⇒ nhãn Việt; mã lạ ẩn; trùng gộp; không phân biệt hoa thường', () {
      expect(posLabels(['n', 'V', ' a ', 'vn', 'n']), ['danh từ', 'động từ', 'tính từ']);
      expect(posLabels(null), isEmpty);
      expect(posLabel('zz'), isNull);
    });
  });

  group('readingAt', () {
    test('ưu tiên âm tiết trong từ nếu nằm trong cách đọc của chữ', () {
      expect(readingAt('ni3 hao3', 1, ['hao3', 'hao4']), 'hao3');
    });

    test('không khớp ⇒ cách đọc đầu; chữ không có cách đọc ⇒ âm tiết trong từ; thiếu cả hai ⇒ null', () {
      expect(readingAt('yin2 hang2', 1, ['xing2', 'hang2']), 'hang2');
      expect(readingAt('yin2 hang2', 1, ['xing2']), 'xing2');
      expect(readingAt('ni3 hao3', 0, null), 'ni3');
      expect(readingAt('', 0, null), isNull);
    });

    test('bỏ r5 nhi hoá khi đếm vị trí; ü ⇒ v', () {
      expect(readingAt('na3 r5', 0, ['na3', 'nei3']), 'na3');
      expect(readingAt('nü3', 0, ['nv3']), 'nv3');
    });
  });

  group('source labels', () {
    test('khoá quen ⇒ nhãn + giấy phép; lạ ⇒ null; nguồn nghĩa Việt', () {
      expect(sourceLabel('cvdict')?.license, 'CC BY-SA 4.0');
      expect(sourceLabel('unknown'), isNull);
      expect(meaningViSourceLabel('machine'), 'Dịch máy');
      expect(meaningViSourceLabel(null), isNull);
    });
  });

  group('WordDetail.fromJson', () {
    test('thiếu khoá ⇒ mặc định; khối srs/characters đọc được', () {
      final w = WordDetail.fromJson({
        'id': 'w1',
        'simplified': '你好',
        'traditional': '你好',
        'pinyin': 'ni3 hao3',
        'meaningsVi': ['xin chào'],
        'meaningViStatus': 'reviewed',
        'characters': [
          {
            'hanzi': '你',
            'pinyinReadings': ['ni3'],
            'hanViet': ['nhĩ'],
          },
          {'hanzi': ''},
        ],
        'srs': {'cardId': 'c1', 'state': 'new', 'dueAt': '2026-09-18T00:00:00Z', 'isSuspended': false},
      });
      expect(w.showTraditional, isFalse);
      expect(w.characters.map((c) => c.hanzi), ['你']);
      expect(w.srs?.cardId, 'c1');
      expect(w.srs?.dueAt, DateTime.utc(2026, 9, 18));
      expect(w.pos, isEmpty);
      expect(WordDetail.fromJson(const {}).srs, isNull);
    });
  });
}
