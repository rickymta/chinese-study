import 'package:af_chinese/features/dictionary/application/providers.dart';
import 'package:af_chinese/features/dictionary/data/models.dart';
import 'package:af_chinese/features/dictionary/domain/character_reading.dart';
import 'package:af_chinese/features/dictionary/domain/pos.dart';
import 'package:af_chinese/features/dictionary/domain/search_errors.dart';
import 'package:af_chinese/features/dictionary/domain/source_labels.dart';
import 'package:af_core/af_core.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

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

  group('SearchResponse.fromJson (fixture dictionary_search.json)', () {
    test('đọc items/page/totalCount; dòng thiếu id hoặc simplified bị bỏ; meaningsVi ≤ 3', () {
      final r = SearchResponse.fromJson(loadFixture('dictionary_search.json'));
      expect(r.items.map((w) => w.id), ['w1', 'w2']);
      expect(r.page, 1);
      expect(r.pageSize, 20);
      expect(r.totalCount, 2);
      expect(r.rawCount, 4); // 4 dòng thô, 2 dòng hợp lệ
      expect(r.isLastPage, isTrue); // 4 < pageSize 20
      final ai = r.items.first;
      expect(ai.simplified, '爱');
      expect(ai.traditional, '愛');
      expect(ai.pinyin, 'ai4');
      expect(ai.hsk3Level, 1);
      expect(ai.hanViet, 'ái');
      expect(ai.meaningsVi, ['yêu', 'thích', 'yêu thích']);
      expect(ai.meaningViStatus, 'machine');
      expect(ai.matchKind, 'hanzi');
      expect(r.items[1].traditional, isNull);
    });

    test('thân rỗng ⇒ mặc định (không ném); trang đủ pageSize ⇒ chưa phải trang cuối', () {
      final r = SearchResponse.fromJson(const {});
      expect(r.items, isEmpty);
      expect(r.totalCount, 0);
      expect(r.pageSize, kSearchPageSize);
      expect(r.isLastPage, isTrue);
      final full = SearchResponse.fromJson({
        'items': [
          for (var i = 0; i < 20; i++) {'id': 'w$i', 'simplified': '字', 'pinyin': 'zi4'},
        ],
        'pageSize': 20,
        'totalCount': 500,
      });
      expect(full.isLastPage, isFalse);
    });

    test('mergeUniqueById bỏ dòng trùng id, giữ thứ tự', () {
      WordSummary w(String id) => WordSummary(id: id, simplified: '字', pinyin: 'zi4');
      final merged = mergeUniqueById([w('a'), w('b')], [w('b'), w('c'), w('a'), w('d')]);
      expect(merged.map((x) => x.id), ['a', 'b', 'c', 'd']);
    });
  });

  group('WordDetail.fromJson (fixture dictionary_word.json — không có srs)', () {
    test('đủ trường; srs vắng ⇒ null; showTraditional', () {
      final w = WordDetail.fromJson(loadFixture('dictionary_word.json'));
      expect(w.id, 'w1');
      expect(w.showTraditional, isTrue);
      expect(w.pos, ['v', 'n']);
      expect(w.meaningsEn, hasLength(3));
      expect(w.meaningViSource, 'cvdict');
      expect(w.hanVietStatus, 'derived');
      expect(w.sources, ['hsk30-official', 'cc-cedict', 'cvdict']);
      expect(w.characters.single.strokeCount, 10);
      expect(w.srs, isNull);
    });
  });

  group('CharacterDetail.fromJson (fixture dictionary_character.json)', () {
    test('đọc đủ trường, words[] là WordSummary, phồn thể trùng giản thể bị lược khi hiển thị', () {
      final c = CharacterDetail.fromJson(loadFixture('dictionary_character.json'));
      expect(c.hanzi, '爱');
      expect(c.traditionalVariants, ['愛']);
      expect(c.traditionalShown, ['愛']);
      expect(c.pinyinReadings, ['ai4']);
      expect(c.hanViet, ['ái']);
      expect(c.hanVietByPinyin, {'ai4': 'ái'});
      expect(c.hanVietStatus, 'reviewed');
      expect(c.strokeCount, 10);
      expect(c.radical, '爪');
      expect(c.radicalNumber, 87);
      expect(c.words.map((w) => w.id), ['w1', 'w2']);

      final same = CharacterDetail.fromJson({
        'hanzi': '一',
        'traditionalVariants': ['一'],
      });
      expect(same.traditionalShown, isEmpty);
      expect(same.words, isEmpty);
      expect(CharacterDetail.fromJson(const {}).hanzi, '');
    });
  });

  group('dictionaryErrorMessage (port QueryErrorAlert)', () {
    test('503 ⇒ câu cố định, không thử lại; 400 ⇒ thông điệp đầu trong details; khác ⇒ message + thử lại', () {
      final e503 = ApiError('Dịch vụ chưa sẵn sàng', status: 503, code: 'CONTENT_UNAVAILABLE');
      expect(dictionaryErrorMessage(e503), kDictionaryUnavailableMessage);
      expect(dictionaryErrorRetryable(e503), isFalse);

      final e400 = ApiError(
        'Dữ liệu không hợp lệ.',
        status: 400,
        code: 'VALIDATION',
        details: {
          'q': ['Từ khoá tối đa 64 ký tự.'],
        },
      );
      expect(dictionaryErrorMessage(e400), 'Từ khoá tối đa 64 ký tự.');
      expect(dictionaryErrorRetryable(e400), isFalse);
      expect(
        dictionaryErrorMessage(ApiError('chung', status: 400, details: {'hanzi': 'Phải là một chữ Hán.'})),
        'Phải là một chữ Hán.',
      );
      expect(dictionaryErrorMessage(ApiError('chung', status: 400, details: {'x': <String>[]})), 'chung');
      expect(firstDetailMessage(ApiError('chung', status: 400)), isNull);

      final net = ApiError.network();
      expect(dictionaryErrorMessage(net), net.message);
      expect(dictionaryErrorRetryable(net), isTrue);
    });
  });
}
