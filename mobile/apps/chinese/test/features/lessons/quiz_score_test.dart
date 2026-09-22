import 'package:af_chinese/features/lessons/data/models.dart';
import 'package:af_chinese/features/lessons/domain/quiz_score.dart';
import 'package:flutter_test/flutter_test.dart';

QuizQuestion q(String id) => QuizQuestion(id: id, prompt: 'p');

// Chép đủ ca của `quizScore.test.ts` web (7 ca).
void main() {
  group('scorePercent', () {
    test('làm tròn XUỐNG (7/9 = 77, không phải 78)', () {
      expect(scorePercent(7, 9), 77);
      expect(scorePercent(4, 5), 80);
      expect(scorePercent(6, 7), 85);
      expect(scorePercent(0, 7), 0);
      expect(scorePercent(7, 7), 100);
    });

    test('total = 0 hoặc correct âm ⇒ 0; correct > total kẹp về 100', () {
      expect(scorePercent(3, 0), 0);
      expect(scorePercent(-1, 5), 0);
      expect(scorePercent(9, 5), 100);
    });
  });

  group('isPassed', () {
    test('so sánh số nguyên không làm tròn: 4/5 đạt, 7/9 không đạt', () {
      expect(isPassed(4, 5), isTrue);
      expect(isPassed(7, 9), isFalse);
      expect(isPassed(8, 10), isTrue);
      expect(isPassed(5, 7), isFalse); // 71%
      expect(isPassed(6, 7), isTrue); // 85%
    });

    test('total = 0 ⇒ không đạt; ngưỡng tuỳ chỉnh', () {
      expect(isPassed(0, 0), isFalse);
      expect(isPassed(1, 2, 50), isTrue);
    });
  });

  group('minCorrectToPass', () {
    test('cần đúng ≥ N câu', () {
      expect(minCorrectToPass(5), 4);
      expect(minCorrectToPass(7), 6);
      expect(minCorrectToPass(9), 8);
      expect(minCorrectToPass(10), 8);
      expect(minCorrectToPass(0), 0);
    });
  });

  group('countUnanswered / firstUnansweredIndex / buildAnswers', () {
    final questions = [q('a'), q('b'), q('c')];

    test('đếm và tìm câu chưa trả lời', () {
      final answers = {'a': 'x', 'c': 'y'};
      expect(countUnanswered(questions, answers), 1);
      expect(firstUnansweredIndex(questions, answers), 1);
      expect(firstUnansweredIndex(questions, {'a': '1', 'b': '2', 'c': '3'}), -1);
    });

    test('đóng gói theo thứ tự câu hỏi, bỏ câu chưa trả lời, không có đáp án thừa', () {
      final answers = {'c': 'z', 'a': 'x', 'zz': 'thừa'};
      expect(buildAnswers(questions, answers), [
        const QuizAnswer(questionId: 'a', optionId: 'x'),
        const QuizAnswer(questionId: 'c', optionId: 'z'),
      ]);
    });
  });
}
