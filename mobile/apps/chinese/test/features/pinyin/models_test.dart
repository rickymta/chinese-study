import 'package:af_chinese/features/pinyin/data/models.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  test('PinyinChart.fromJson parse fixture (tập con học liệu thật): 22 thanh mẫu, 37 vận mẫu, ma đủ 4 thanh', () {
    final c = PinyinChart.fromJson(loadFixture('pinyin_chart.json'));
    expect(c.version, 'fixture-m7');
    expect(c.initials, hasLength(22));
    expect(c.initials.first.code, '');
    expect(c.initials.first.group, 'khong');
    expect(c.initials[1].examples.single.pinyin, 'bai2');
    expect(c.finals, hasLength(37));
    expect(c.finals.firstWhere((f) => f.code == 'v').group, 'v');
    expect(c.finals.firstWhere((f) => f.code == 'ian').standaloneSpelling, 'yan');
    final ma = c.syllableByKey('ma')!;
    expect(ma.initial, 'm');
    expect(ma.final_, 'a');
    expect(ma.tones.keys.toList(), [1, 2, 3, 4]);
    expect(ma.tones[1], const ToneExample(hanzi: '妈', meaningVi: 'mẹ'));
    expect(ma.tones[3]!.hanzi, '马');
    // Âm tiết chưa có chữ minh hoạ ⇒ tones rỗng, vẫn parse (ô mờ trong bảng).
    expect(c.syllableByKey('a')!.hasAnyTone, isFalse);
    expect(c.syllableByKey('xyz'), isNull);
  });

  test('PinyinSyllable.fromJson bỏ khoá thanh lạ/ví dụ thiếu hanzi', () {
    final s = PinyinSyllable.fromJson({
      'syllable': 'ma',
      'initial': 'm',
      'final': 'a',
      'tones': {
        '1': {'hanzi': '妈', 'meaningVi': 'mẹ'},
        '5': {'hanzi': '吗'},
        '2': {'meaningVi': 'thiếu chữ'},
        '3': 'không phải object',
      },
    });
    expect(s.tones.keys.toList(), [1]);
  });

  test('PinyinGuide.fromJson: sắp chủ đề theo order, block đa hình, kiểu lạ bị bỏ', () {
    final g = PinyinGuide.fromJson(loadFixture('pinyin_guide.json'));
    expect(g.topics.map((t) => t.id).toList(), ['bon-thanh', 'thanh-mau', 'bien-dieu']);
    final first = g.topics.first;
    expect(first.blocks.whereType<GuideParagraph>(), isNotEmpty);
    expect(first.blocks.whereType<GuideToneContour>().single.tones, [1, 2, 3, 4]);
    final examples = first.blocks.whereType<GuideExamples>().first.items;
    expect(examples.first.pinyin, 'ma1');
    expect(examples.first.hanzi, '妈');
    expect(first.blocks.whereType<GuideTip>(), isNotEmpty);
    final compare = g.topics[1].blocks.whereType<GuideCompare>().single;
    expect(compare.title, isNotEmpty);
    expect(compare.pairs.first.left.hanzi, '白');
    expect(compare.pairs.first.right.pinyin, 'pai2');
    expect(compare.pairs.first.noteVi, isNotNull);

    final odd = GuideTopic.fromJson({
      'id': 'x',
      'title': 'x',
      'order': 1,
      'blocks': [
        {'type': 'video', 'url': 'x'},
        {'type': 'paragraph', 'text': 'ok'},
      ],
    });
    expect(odd.blocks, hasLength(1));
  });

  test('normalizeToneStats: người mới (accuracy vắng trong byTone, null ở gốc) ⇒ null/0, đủ 4 khoá', () {
    final s = ToneStats.fromJson(loadFixture('tone_stats_new_user.json'));
    expect(s.totalAnswered, 0);
    expect(s.sessionsCount, 0);
    expect(s.lastSessionAt, isNull);
    expect(s.windowSize, 200);
    expect(s.accuracy, isNull);
    expect(s.byTone.keys.toList(), [1, 2, 3, 4]);
    expect(s.byTone[2], const ToneAccuracy());
    expect(s.confusions, isEmpty);
    expect(s.recommendedFocus, isEmpty);
    expect(s.g0Reached, isFalse);
  });

  test('normalizeToneStats: đủ dữ liệu', () {
    final s = ToneStats.fromJson(loadFixture('tone_stats_full.json'));
    expect(s.totalAnswered, 140);
    expect(s.sessionsCount, 7);
    expect(s.lastSessionAt, DateTime.utc(2026, 9, 18, 2, 15));
    expect(s.accuracy, closeTo(0.8214, 0.001));
    expect(s.byTone[1], const ToneAccuracy(total: 36, correct: 33, accuracy: 0.9166666666666666));
    expect(s.confusions.first, const ToneConfusion(expected: 2, answered: 3, count: 6));
    expect(s.recommendedFocus, [2, 3]);
    expect(s.g0Reached, isFalse);
  });

  test('normalizeToneStats: thân rỗng/thiếu hết/kiểu sai ⇒ mặc định (RK41), windowSize 200, byTone thiếu khoá', () {
    final empty = normalizeToneStats(null);
    expect(empty.windowSize, 200);
    expect(empty.byTone, hasLength(4));
    final partial = normalizeToneStats({
      'byTone': {
        '1': {'total': 5, 'correct': 4, 'accuracy': 'không phải số'},
      },
      'accuracy': '0.8',
      'confusions': 'sai kiểu',
      'recommendedFocus': [2, 'x', 3],
      'g0Reached': 'true',
    });
    expect(partial.byTone[1], const ToneAccuracy(total: 5, correct: 4));
    expect(partial.byTone[4], const ToneAccuracy());
    expect(partial.accuracy, isNull);
    expect(partial.confusions, isEmpty);
    expect(partial.recommendedFocus, [2, 3]);
    expect(partial.g0Reached, isTrue);
  });

  test('SubmitToneDrillResponse.fromJson: mode snake_case, byTone đủ 4 khoá', () {
    final r = SubmitToneDrillResponse.fromJson({
      'id': 's-1',
      'clientSessionId': 'c-1',
      'mode': 'tone_pair',
      'total': 20,
      'correct': 15,
      'localDate': '2026-09-18',
      'byTone': {
        '1': {'total': 10, 'correct': 9},
        '3': {'total': 8, 'correct': 4},
      },
    });
    expect(r.mode, DrillMode.tonePair);
    expect(r.localDate, '2026-09-18');
    expect(r.byTone[1], const ToneCount(total: 10, correct: 9));
    expect(r.byTone[2], const ToneCount());
    expect(r.byTone[3], const ToneCount(total: 8, correct: 4));
  });

  test('DrillMode: giá trị API/URL', () {
    expect(DrillMode.fromApi('listen_tone'), DrillMode.listenTone);
    expect(DrillMode.fromApi(null), DrillMode.listenTone);
    expect(DrillMode.fromParam('cap'), DrillMode.tonePair);
    expect(DrillMode.fromParam('mot'), DrillMode.listenTone);
    expect(DrillMode.tonePair.param, 'cap');
    expect(DrillMode.listenTone.apiValue, 'listen_tone');
  });
}
