// Chép ĐỦ các ca của `frontend/apps/chinese/src/lib/pinyin.test.ts` (hợp đồng mobile M3) + vài ca riêng Dart
// (dạng tổ hợp NFD thật vì Dart không có `normalize`). Thứ tự nhóm giữ như bản web để đối chiếu.
import 'package:af_chinese/core/pinyin/pinyin.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('syllableToMarked — quy tắc đặt dấu', () {
    const cases = <(String, String)>[
      ('lve4', 'lüè'),
      ('gui4', 'guì'),
      ('liu2', 'liú'),
      ('huo3', 'huǒ'),
      ('er2', 'ér'),
      ('r5', 'r'),
      ('zhuang4', 'zhuàng'),
      ('xue2', 'xué'),
      ('jiong3', 'jiǒng'),
      ('ou1', 'ōu'),
      ('lv5', 'lü'),
      ('nv3', 'nǚ'),
      ('A1', 'Ā'),
      ('ma1', 'mā'),
      ('hao3', 'hǎo'),
      ('xie4', 'xiè'),
      ('de5', 'de'),
    ];
    for (final (input, expected) in cases) {
      test('$input → $expected', () => expect(syllableToMarked(input), expected));
    }

    test('token sai giữ nguyên văn', () {
      expect(syllableToMarked('ma'), 'ma');
      expect(syllableToMarked('ma6'), 'ma6');
      expect(syllableToMarked(''), '');
    });

    test('không nguyên âm (m/n/ng) không ném, trả không dấu', () {
      expect(syllableToMarked('ng2'), 'ng');
      expect(syllableToMarked('m2'), 'm');
    });
  });

  group('numberedToMarked', () {
    test('ni3 hao3 → nǐ hǎo', () => expect(numberedToMarked('ni3 hao3'), 'nǐ hǎo'));
    test('na3 r5 → nǎr (nhi hoá dính âm tiết trước)', () => expect(numberedToMarked('na3 r5'), 'nǎr'));
    test('giữ chữ hoa: Ou1 zhou1 → Ōu zhōu', () => expect(numberedToMarked('Ou1 zhou1'), 'Ōu zhōu'));
    test("join: Xi1 an1 → Xī'ān", () {
      expect(numberedToMarked('Xi1 an1', join: true), "Xī'ān");
      expect(numberedToMarked('ni3 hao3', join: true), 'nǐhǎo');
    });
    test('token sai giữ nguyên, token đúng vẫn chuyển', () => expect(numberedToMarked('ni3 xyz hao3'), 'nǐ xyz hǎo'));
    test('gộp khoảng trắng thừa', () => expect(numberedToMarked('  ni3   hao3 '), 'nǐ hǎo'));
    test('dấu câu dính cuối âm tiết vẫn đổi dấu thanh (F9 review)', () {
      expect(numberedToMarked('Ni3 hao3!'), 'Nǐ hǎo!');
      expect(numberedToMarked('Lao3 shi1, nin2 hao3 ma5?'), 'Lǎo shī, nín hǎo ma?');
      expect(numberedToMarked('Bu4 ke4 qi5.'), 'Bù kè qi.');
    });
    test('dấu câu toàn khổ và dấu ngoặc kép hai đầu', () {
      expect(numberedToMarked('“Xie4 xie5，” ta1 shuo1。'), '“Xiè xie，” tā shuō。');
      expect(numberedToMarked('(wo3) men5'), '(wǒ) men');
    });
    test('giữ đúng ü và thanh nhẹ: nv3 er2 → nǚ ér, xie4 xie5 → xiè xie, lv4 → lǜ', () {
      expect(numberedToMarked('nv3 er2'), 'nǚ ér');
      expect(numberedToMarked('xie4 xie5'), 'xiè xie');
      expect(numberedToMarked('lv4'), 'lǜ');
    });
    test('nhi hoá có dấu câu sau: na3 r5? → nǎr?', () => expect(numberedToMarked('na3 r5?'), 'nǎr?'));
    test('r5 đứng đầu hoặc có dấu câu mở phía trước thì KHÔNG dính', () {
      expect(numberedToMarked('r5'), 'r');
      expect(numberedToMarked('na3 (r5'), 'nǎ (r');
    });
  });

  group('normalizeNumbered', () {
    test('lü4 → lv4', () => expect(normalizeNumbered('lü4'), 'lv4'));
    test('lu:4 → lv4', () => expect(normalizeNumbered('lu:4'), 'lv4'));
    test('LÜ4 → Lv4 (giữ hoa chữ đầu)', () => expect(normalizeNumbered('LÜ4'), 'Lv4'));
    test('gộp khoảng trắng: "ni3  hao3 " → ni3 hao3', () => expect(normalizeNumbered('ni3  hao3 '), 'ni3 hao3'));
    test('Bei3 jing1 giữ hoa', () => expect(normalizeNumbered('Bei3 jing1'), 'Bei3 jing1'));
    test('thiếu thanh → null', () => expect(normalizeNumbered('ma'), isNull));
    test('thanh 6 → null', () => expect(normalizeNumbered('ma6'), isNull));
    test('thanh 0 → null', () => expect(normalizeNumbered('ma0'), isNull));
    test('rỗng → null', () => expect(normalizeNumbered('   '), isNull));
  });

  group('markedToNumbered', () {
    test('nǐ hǎo → ni3 hao3', () => expect(markedToNumbered('nǐ hǎo'), 'ni3 hao3'));
    test('lǜ → lv4', () => expect(markedToNumbered('lǜ'), 'lv4'));
    test('nǚ → nv3', () => expect(markedToNumbered('nǚ'), 'nv3'));
    test('ma → ma5 (không dấu = thanh nhẹ)', () => expect(markedToNumbered('ma'), 'ma5'));
    test("Xī'ān → Xi1 an1", () => expect(markedToNumbered("Xī'ān"), 'Xi1 an1'));
    test('dấu tổ hợp (NFD: u + ̈ + ̌) vẫn đọc được', () {
      expect(markedToNumbered('nǚ'), 'nv3');
      // NFD thật: n + u + U+0308 + U+030C — Dart không có normalize, `_composePinyin` phải ghép được.
      expect(markedToNumbered('nǚ'), 'nv3');
      expect(markedToNumbered('nǐ hǎo'), 'ni3 hao3');
      expect(markedToNumbered('Ā'), 'A1');
    });
    test('ký tự lạ → null', () => expect(markedToNumbered('ni3!'), isNull));
    test('rỗng → null', () => expect(markedToNumbered(''), isNull));
    test('hai dấu thanh trong một âm tiết → null', () => expect(markedToNumbered('nǐǎo'), isNull));

    test('round-trip 10 âm tiết số → dấu → số', () {
      const list = ['ni3', 'hao3', 'lve4', 'gui4', 'liu2', 'zhuang4', 'xue2', 'jiong3', 'er2', 'nv3'];
      for (final s in list) {
        expect(markedToNumbered(syllableToMarked(s)), s);
      }
    });
  });

  group('parseSyllable / toneOf / stripTone / displaySyllableKey', () {
    test('Lv4 → letters lv, tone 4, capitalized', () {
      expect(parseSyllable('Lv4'), const ParsedSyllable(letters: 'lv', tone: 4, capitalized: true));
    });
    test('token sai → null', () => expect(parseSyllable('ma'), isNull));
    test('toneOf', () {
      expect(toneOf('ma3'), 3);
      expect(toneOf('ma'), isNull);
    });
    test('stripTone', () {
      expect(stripTone('lv4'), 'lv');
      expect(stripTone('abc'), 'abc');
    });
    test('displaySyllableKey', () {
      expect(displaySyllableKey('nv'), 'nü');
      expect(displaySyllableKey('lve'), 'lüe');
      expect(displaySyllableKey('ju'), 'ju');
    });
  });

  group('splitPunctuation / stripPunctuation', () {
    test('hao3! → core hao3, trail !', () {
      expect(splitPunctuation('hao3!'), (lead: '', core: 'hao3', trail: '!'));
      expect(splitPunctuation('“Xie4'), (lead: '“', core: 'Xie4', trail: ''));
      expect(splitPunctuation('(wo3)'), (lead: '(', core: 'wo3', trail: ')'));
    });
    test('stripPunctuation bỏ dấu câu và khoảng trắng khỏi chữ Hán', () {
      expect(stripPunctuation('不是，我很好。'), '不是我很好');
      expect(stripPunctuation('你 好！'), '你好');
    });
  });

  group('sandhiHints', () {
    test('ni3 hao3 → index 0 gợi ý thanh 2', () {
      final h = sandhiHints('ni3 hao3');
      expect(h, hasLength(1));
      expect(h[0].index, 0);
      expect(h[0].kind, SandhiKind.thirdTone);
      expect(h[0].suggestedTone, 2);
    });
    test('wo3 hen3 hao3 → index 0 và 1', () {
      expect(sandhiHints('wo3 hen3 hao3').map((h) => h.index), [0, 1]);
    });
    test('bu4 shi4 + 不是 → bú', () {
      final h = sandhiHints('bu4 shi4', '不是');
      expect(h, hasLength(1));
      expect(h[0].index, 0);
      expect(h[0].kind, SandhiKind.bu);
      expect(h[0].suggestedTone, 2);
    });
    test('bu4 hao3 + 不好 → rỗng', () => expect(sandhiHints('bu4 hao3', '不好'), isEmpty));
    test('yi1 ge4 + 一个 → yí', () {
      final h = sandhiHints('yi1 ge4', '一个')[0];
      expect((h.index, h.kind, h.suggestedTone), (0, SandhiKind.yi, 2));
    });
    test('yi1 tian1 + 一天 → yì', () {
      final h = sandhiHints('yi1 tian1', '一天')[0];
      expect((h.index, h.kind, h.suggestedTone), (0, SandhiKind.yi, 4));
    });
    test('yi1 fu5 + 衣服 → rỗng (không phải 一)', () => expect(sandhiHints('yi1 fu5', '衣服'), isEmpty));
    test('tong3 yi1 + 统一 → rỗng (一 đứng cuối)', () => expect(sandhiHints('tong3 yi1', '统一'), isEmpty));
    test('không có hanzi → không gợi ý 不/一', () => expect(sandhiHints('bu4 shi4'), isEmpty));
    test('bỏ dấu câu trước khi đếm âm tiết: "Ni3 hao3!" + "你好！" → index 0', () {
      expect(sandhiHints('Ni3 hao3!', '你好！').map((h) => h.index), [0]);
    });
    test('dấu câu giữa câu không làm lệch ánh xạ chữ Hán: "Bu4 shi4, wo3 hen3 hao3." + "不是，我很好。"', () {
      final h = sandhiHints('Bu4 shi4, wo3 hen3 hao3.', '不是，我很好。');
      expect(h.map((x) => (x.index, x.kind)), [
        (0, SandhiKind.bu),
        (2, SandhiKind.thirdTone),
        (3, SandhiKind.thirdTone),
      ]);
    });
    test('chuỗi ≥ 3 thanh 3 ghi "tuỳ cách ngắt nhịp"', () {
      final h = sandhiHints('wo3 hen3 hao3');
      expect(h.every((x) => x.text.contains('tuỳ cách ngắt nhịp')), isTrue);
    });
    test('nhi hoá: chữ Hán không có 儿 riêng vẫn ánh xạ được 不', () {
      // "bu4 dian3 r5" + "不点" (2 chữ, 3 token) — nhánh nonErhua.
      expect(sandhiHints('bu4 dian3 r5', '不点'), isEmpty);
      expect(sandhiHints('bu4 shi4 r5', '不是').map((h) => h.kind), [SandhiKind.bu]);
    });
  });
}
