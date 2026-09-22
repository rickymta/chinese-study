/// Tính điểm/đếm câu của quiz — port 1-1 `features/lessons/lib/quizScore.ts` web. Server là nguồn sự thật (`passed`,
/// `scorePercent`); các hàm này chỉ để hiển thị trước khi nộp và kiểm tra nhất quán.
library;

import '../data/models.dart';

/// Điểm hiển thị = `floor(correct * 100 / total)` (R-LS3); `total = 0` ⇒ 0; `correct > total` kẹp về 100.
int scorePercent(int correct, int total) {
  if (total <= 0 || correct <= 0) return 0;
  final c = correct < total ? correct : total;
  return (c * 100) ~/ total;
}

/// Đạt khi `correct * 100 >= threshold * total` bằng số nguyên, KHÔNG làm tròn (R-LS3): 4/5 = 80 đạt; 7/9 = 77 không.
bool isPassed(int correct, int total, [int threshold = kPassThresholdPercent]) {
  if (total <= 0) return false;
  return correct * 100 >= threshold * total;
}

/// Số câu đúng tối thiểu để đạt ngưỡng (hiện "cần đúng ≥ N/M câu").
int minCorrectToPass(int total, [int threshold = kPassThresholdPercent]) {
  if (total <= 0) return 0;
  return (threshold * total / 100).ceil();
}

/// Số câu chưa trả lời trong lượt hiện tại (nút "Nộp bài" khoá cho tới khi = 0).
int countUnanswered(List<QuizQuestion> questions, Map<String, String> answers) {
  var n = 0;
  for (final q in questions) {
    if (!answers.containsKey(q.id)) n++;
  }
  return n;
}

/// Chỉ số câu đầu tiên chưa trả lời, `-1` khi đã đủ.
int firstUnansweredIndex(List<QuizQuestion> questions, Map<String, String> answers) {
  for (var i = 0; i < questions.length; i++) {
    if (!answers.containsKey(questions[i].id)) return i;
  }
  return -1;
}

/// Đóng gói đáp án theo ĐÚNG thứ tự câu hỏi của lượt (R-LS8: mỗi câu đúng một lần), bỏ đáp án thừa/câu chưa trả lời.
List<QuizAnswer> buildAnswers(List<QuizQuestion> questions, Map<String, String> answers) {
  final out = <QuizAnswer>[];
  for (final q in questions) {
    final optionId = answers[q.id];
    if (optionId != null) out.add(QuizAnswer(questionId: q.id, optionId: optionId));
  }
  return out;
}
