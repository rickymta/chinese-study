// Sinh câu hỏi luyện thanh Ở CLIENT từ catalog (R5-7, R5-8, D28) — port 1-1 `features/pinyin/drill/generateDrill.ts`.
// Hàm thuần, nhận `Random` để test tất định (so chéo với bản web bằng cùng bộ sinh LCG — xem test).
import 'dart:math';

import 'package:flutter/foundation.dart';

import '../data/models.dart';

/// Một phần câu hỏi: âm tiết + chữ minh hoạ + thanh đúng + nghĩa.
@immutable
class DrillPart {
  const DrillPart({required this.syllable, required this.hanzi, required this.tone, this.meaningVi = ''});

  final String syllable;
  final String hanzi;
  final int tone;
  final String meaningVi;

  @override
  bool operator ==(Object other) =>
      other is DrillPart &&
      other.syllable == syllable &&
      other.hanzi == hanzi &&
      other.tone == tone &&
      other.meaningVi == meaningVi;

  @override
  int get hashCode => Object.hash(syllable, hanzi, tone, meaningVi);

  @override
  String toString() => 'DrillPart($syllable$tone $hanzi)';
}

/// Một câu: 1 phần (`listen_tone`) hoặc 2 phần (`tone_pair`).
@immutable
class DrillItem {
  const DrillItem(this.parts);

  final List<DrillPart> parts;

  /// Chữ Hán cần đọc (ghép các phần).
  String get text => parts.map((p) => p.hanzi).join();

  @override
  bool operator ==(Object other) => other is DrillItem && listEquals(other.parts, parts);

  @override
  int get hashCode => Object.hashAll(parts);

  @override
  String toString() => 'DrillItem($parts)';
}

List<T> _shuffle<T>(List<T> arr, Random random) {
  final out = [...arr];
  for (var i = out.length - 1; i > 0; i--) {
    final j = (random.nextDouble() * (i + 1)).floor();
    final tmp = out[i];
    out[i] = out[j];
    out[j] = tmp;
  }
  return out;
}

int _pickIndex(int n, Random random) => min(n - 1, (random.nextDouble() * n).floor());

/// Mọi cặp (âm tiết, thanh) có chữ minh hoạ, gom theo thanh (thứ tự: theo `syllables` rồi thanh 1..4).
Map<int, List<DrillPart>> buildTonePools(PinyinChart chart) {
  final pools = <int, List<DrillPart>>{for (final t in kDrillTones) t: []};
  for (final s in chart.syllables) {
    for (final t in kDrillTones) {
      final ex = s.tones[t];
      if (ex != null && ex.hanzi.isNotEmpty) {
        pools[t]!.add(DrillPart(syllable: s.syllable, hanzi: ex.hanzi, tone: t, meaningVi: ex.meaningVi));
      }
    }
  }
  return pools;
}

/// 15 tổ hợp thanh cho `tone_pair` — loại 3-3 (TTS sẽ biến điệu thành 2-3, chấm sai oan — RK35).
final List<(int, int)> kTonePairCombos = [
  for (final a in kDrillTones)
    for (final b in kDrillTones)
      if (!(a == 3 && b == 3)) (a, b),
];

/// Dãy thanh cho `listen_tone`: có focus ⇒ nửa đầu lấy từ focus (xoay vòng), nửa sau ngẫu nhiên đều; không focus ⇒
/// mỗi thanh `count/4` câu (dư chia ngẫu nhiên). Kết quả được xáo.
List<int> _toneSequence(int count, List<int> focus, Random random) {
  final seq = <int>[];
  if (focus.isNotEmpty) {
    final half = count ~/ 2;
    for (var i = 0; i < half; i++) {
      seq.add(focus[i % focus.length]);
    }
    for (var i = half; i < count; i++) {
      seq.add(kDrillTones[_pickIndex(4, random)]);
    }
  } else {
    final per = count ~/ 4;
    for (final t in kDrillTones) {
      for (var i = 0; i < per; i++) {
        seq.add(t);
      }
    }
    while (seq.length < count) {
      seq.add(kDrillTones[_pickIndex(4, random)]);
    }
  }
  return _shuffle(seq, random);
}

List<(int, int)> _pairSequence(int count, List<int> focus, Random random) {
  final seq = <(int, int)>[];
  final focused = kTonePairCombos.where((c) => focus.contains(c.$1) || focus.contains(c.$2)).toList();
  if (focus.isNotEmpty && focused.isNotEmpty) {
    final half = count ~/ 2;
    final f = _shuffle(focused, random);
    for (var i = 0; i < half; i++) {
      seq.add(f[i % f.length]);
    }
    final all = _shuffle(kTonePairCombos, random);
    for (var i = half; i < count; i++) {
      seq.add(all[(i - half) % all.length]);
    }
  } else {
    // Không focus: đi hết 15 tổ hợp (xáo) rồi lặp — phân bố đều nhất có thể với 20 câu.
    final all = _shuffle(kTonePairCombos, random);
    for (var i = 0; i < count; i++) {
      seq.add(all[i % all.length]);
    }
  }
  return _shuffle(seq, random);
}

/// Sinh [count] câu (mặc định 20). Không lặp cùng chữ minh hoạ trong một phiên khi còn lựa chọn khác (RK39: hết
/// lựa chọn thì cho lặp). `tone_pair`: hai âm tiết KHÁC nhau. Catalog không có chữ cho thanh nào ⇒ bỏ thanh đó
/// (có thể trả ít hơn [count] — chỉ xảy ra với học liệu thiếu). [focus] = `recommendedFocus` từ thống kê — 50% câu
/// dồn vào đây khi khác rỗng.
List<DrillItem> generateDrill({
  required DrillMode mode,
  required PinyinChart chart,
  List<int> focus = const [],
  int count = 20,
  Random? random,
}) {
  final rng = random ?? Random();
  final pools = buildTonePools(chart);
  final usedHanzi = <String>{};

  DrillPart? pick(int tone, [String? excludeSyllable]) {
    final pool = pools[tone]!.where((p) => p.syllable != excludeSyllable).toList();
    if (pool.isEmpty) return null;
    final fresh = pool.where((p) => !usedHanzi.contains(p.hanzi)).toList();
    final from = fresh.isNotEmpty ? fresh : pool;
    final chosen = from[_pickIndex(from.length, rng)];
    usedHanzi.add(chosen.hanzi);
    return chosen;
  }

  final items = <DrillItem>[];
  if (mode == DrillMode.listenTone) {
    for (final tone in _toneSequence(count, focus, rng)) {
      final part = pick(tone);
      if (part != null) items.add(DrillItem([part]));
    }
  } else {
    for (final (a, b) in _pairSequence(count, focus, rng)) {
      final first = pick(a);
      if (first == null) continue;
      final second = pick(b, first.syllable);
      if (second == null) continue;
      items.add(DrillItem([first, second]));
    }
  }
  return items;
}
