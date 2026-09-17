import 'package:af_chinese/features/lessons/data/models.dart';
import 'package:af_chinese/features/lessons/domain/shuffle.dart';
import 'package:flutter_test/flutter_test.dart';

QuizQuestion question(String id, [int n = 4]) => QuizQuestion(
  id: id,
  prompt: 'p',
  options: [for (var i = 0; i < n; i++) QuizOption(id: 'abcd'[i], text: 'opt $i')],
);

// Chép đủ ca của `shuffle.test.ts` web (6 ca) + 1 ca so chéo LCG với web.
void main() {
  group('shuffle', () {
    test('trả hoán vị: cùng phần tử, cùng số lượng, không sửa mảng vào', () {
      final input = List<int>.unmodifiable([1, 2, 3, 4, 5, 6, 7, 8]);
      final out = shuffle(input, seededRng(7));
      expect(out, hasLength(input.length));
      expect([...out]..sort(), input);
      expect(input, [1, 2, 3, 4, 5, 6, 7, 8]);
    });

    test('cùng seed ⇒ cùng thứ tự (ổn định); seed khác ⇒ có thể khác', () {
      final input = ['a', 'b', 'c', 'd', 'e', 'f'];
      expect(shuffle(input, seededRng(42)), shuffle(input, seededRng(42)));
      final many = {for (var s = 0; s < 20; s++) shuffle(input, seededRng(s)).join()};
      expect(many.length, greaterThan(1));
    });

    test('mảng rỗng và một phần tử', () {
      expect(shuffle(<int>[], seededRng(1)), isEmpty);
      expect(shuffle(['x'], seededRng(1)), ['x']);
    });

    test('rng trả 0 ⇒ mọi phần tử đổi chỗ với phần tử đầu (không đọc ngoài mảng); rng trả 1 vẫn an toàn', () {
      expect(shuffle([1, 2, 3], () => 0), [2, 3, 1]);
      expect(shuffle([1, 2, 3], () => 1), [1, 2, 3]);
    });

    test('mặc định (không truyền rng) vẫn là hoán vị', () {
      final out = shuffle([1, 2, 3, 4, 5]);
      expect([...out]..sort(), [1, 2, 3, 4, 5]);
    });
  });

  group('shuffleOptions', () {
    test('mỗi câu một hoán vị của lựa chọn gốc, khoá theo id câu', () {
      final qs = [question('q1'), question('q2', 3)];
      final order = shuffleOptions(qs, seededRng(3));
      expect(order.keys.toList(), ['q1', 'q2']);
      expect(order['q1']!.map((o) => o.id).toList()..sort(), ['a', 'b', 'c', 'd']);
      expect(order['q2']!.map((o) => o.id).toList()..sort(), ['a', 'b', 'c']);
    });

    test('cùng seed ⇒ cùng thứ tự cho cả bộ câu', () {
      final qs = [question('q1'), question('q2'), question('q3')];
      final a = shuffleOptions(qs, seededRng(99));
      final b = shuffleOptions(qs, seededRng(99));
      expect(a, b);
    });
  });

  group('seededRng', () {
    test('LCG cùng hằng số với web: 3 giá trị đầu của seed 42', () {
      // Tính theo công thức web `(Math.imul(state, 1664525) + 1013904223) >>> 0`, chia 2^32.
      final r = seededRng(42);
      expect(r(), closeTo(0.2523451747838408, 1e-12));
      expect(r(), closeTo(0.08812504541128874, 1e-12));
      expect(r(), closeTo(0.5772811982315034, 1e-12));
    });
  });
}
