import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:af_chinese/features/pinyin/data/models.dart';
import 'package:af_chinese/features/pinyin/domain/generate_drill.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

/// Bộ sinh giả tất định (LCG) — CÙNG công thức với `seeded()` trong `generateDrill.test.ts` web
/// (`s = (s * 1664525 + 1013904223) >>> 0; s / 2^32`) để so chéo kết quả Dart ⇄ TS.
class SeededLcg implements Random {
  SeededLcg([int seed = 42]) : _s = seed & 0xFFFFFFFF;

  int _s;

  @override
  double nextDouble() {
    _s = (_s * 1664525 + 1013904223) & 0xFFFFFFFF;
    return _s / 4294967296;
  }

  @override
  int nextInt(int max) => (nextDouble() * max).floor();

  @override
  bool nextBool() => nextDouble() < 0.5;
}

/// Catalog giả như web: [count] âm tiết, mỗi âm tiết đủ 4 thanh, chữ minh hoạ duy nhất (từ U+4E00).
PinyinChart fakeChart([int count = 30]) {
  final syllables = <PinyinSyllable>[];
  var code = 0x4e00;
  for (var i = 0; i < count; i++) {
    final tones = <int, ToneExample>{};
    for (final t in kDrillTones) {
      tones[t] = ToneExample(hanzi: String.fromCharCode(code++), meaningVi: 'nghĩa $i-$t');
    }
    syllables.add(PinyinSyllable(syllable: 's$i', initial: '', final_: 'a', tones: tones));
  }
  return PinyinChart(version: 'fake', initials: const [], finals: const [], syllables: syllables);
}

void main() {
  final chart = fakeChart();
  final hanziSet = {
    for (final s in chart.syllables)
      for (final t in s.tones.values) t.hanzi,
  };

  group('generateDrill — listen_tone', () {
    test('đủ 20 câu, mỗi câu 1 phần', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg());
      expect(items, hasLength(20));
      expect(items.every((it) => it.parts.length == 1), isTrue);
    });

    test('không focus ⇒ mỗi thanh đúng 5 câu', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg(7));
      final count = {1: 0, 2: 0, 3: 0, 4: 0};
      for (final it in items) {
        count[it.parts[0].tone] = count[it.parts[0].tone]! + 1;
      }
      expect(count, {1: 5, 2: 5, 3: 5, 4: 5});
    });

    test('focus [2] ⇒ ít nhất 10 câu thanh 2', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: chart, focus: const [2], random: SeededLcg(3));
      expect(items.where((it) => it.parts[0].tone == 2).length, greaterThanOrEqualTo(10));
    });

    test('mọi phần có chữ minh hoạ trong chart', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg(9));
      expect(items.every((it) => hanziSet.contains(it.parts[0].hanzi)), isTrue);
    });

    test('không lặp chữ khi còn lựa chọn', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg(11));
      final all = items.map((it) => it.parts[0].hanzi).toList();
      expect(all.toSet(), hasLength(all.length));
    });

    test('hết lựa chọn thì cho lặp (catalog rất nhỏ) — vẫn đủ câu', () {
      final items = generateDrill(mode: DrillMode.listenTone, chart: fakeChart(1), random: SeededLcg(1));
      expect(items, hasLength(20));
    });

    test('tất định theo random', () {
      final a = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg(5));
      final b = generateDrill(mode: DrillMode.listenTone, chart: chart, random: SeededLcg(5));
      expect(a, b);
    });
  });

  group('generateDrill — tone_pair', () {
    test('có 15 tổ hợp, không có 3-3', () {
      expect(kTonePairCombos, hasLength(15));
      expect(kTonePairCombos.any((c) => c.$1 == 3 && c.$2 == 3), isFalse);
    });

    test('đủ 20 câu, mỗi câu 2 phần, không 3-3, hai âm tiết khác nhau, chữ có trong chart', () {
      final items = generateDrill(mode: DrillMode.tonePair, chart: chart, random: SeededLcg(21));
      expect(items, hasLength(20));
      for (final it in items) {
        expect(it.parts, hasLength(2));
        final [a, b] = it.parts;
        expect(a.tone == 3 && b.tone == 3, isFalse);
        expect(a.syllable, isNot(b.syllable));
        expect(hanziSet.contains(a.hanzi), isTrue);
        expect(hanziSet.contains(b.hanzi), isTrue);
      }
    });

    test('focus [3] ⇒ ít nhất 10 câu có một phần thanh 3', () {
      final items = generateDrill(mode: DrillMode.tonePair, chart: chart, focus: const [3], random: SeededLcg(2));
      expect(items.where((it) => it.parts.any((p) => p.tone == 3)).length, greaterThanOrEqualTo(10));
    });

    test('không lặp chữ khi còn lựa chọn', () {
      final items = generateDrill(mode: DrillMode.tonePair, chart: chart, random: SeededLcg(8));
      final all = items.expand((it) => it.parts.map((p) => p.hanzi)).toList();
      expect(all.toSet(), hasLength(all.length));
    });
  });

  group('so chéo với generateDrill.ts (fixture sinh bằng Node từ mã web, cùng LCG)', () {
    final cases = (jsonDecode(File('test/fixtures/generate_drill_crosscheck.json').readAsStringSync()) as Map)
        .cast<String, Object?>();
    final fixtureChart = PinyinChart.fromJson(loadFixture('pinyin_chart.json'));

    for (final entry in cases.entries) {
      test(entry.key, () {
        final c = (entry.value! as Map).cast<String, Object?>();
        final chart = c['fake'] == true ? fakeChart(c['fakeCount'] as int) : fixtureChart;
        final items = generateDrill(
          mode: DrillMode.fromApi(c['mode'] as String),
          chart: chart,
          focus: (c['focus'] as List).cast<int>(),
          random: SeededLcg(c['seed'] as int),
        );
        final expected = (c['items'] as List)
            .map((it) => (it as List).map((p) => (p as List).map((x) => x.toString()).join('|')).toList())
            .toList();
        final actual = items.map((it) => it.parts.map((p) => '${p.syllable}|${p.tone}|${p.hanzi}').toList()).toList();
        expect(actual, expected);
      });
    }
  });

  test('buildTonePools: gom theo thanh, bỏ thanh không có chữ', () {
    final pools = buildTonePools(
      const PinyinChart(
        version: '',
        initials: [],
        finals: [],
        syllables: [
          PinyinSyllable(
            syllable: 'ma',
            initial: 'm',
            final_: 'a',
            tones: {1: ToneExample(hanzi: '妈')},
          ),
          PinyinSyllable(syllable: 'a', initial: '', final_: 'a'),
        ],
      ),
    );
    expect(pools[1], [const DrillPart(syllable: 'ma', hanzi: '妈', tone: 1)]);
    expect(pools[2], isEmpty);
    expect(pools[3], isEmpty);
    expect(pools[4], isEmpty);
  });
}
