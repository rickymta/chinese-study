import { PASS_THRESHOLD_PERCENT, type QuizAnswer, type QuizQuestion } from '../types'

/** Điểm hiển thị = `floor(correct * 100 / total)` (R-LS3); `total = 0` ⇒ 0. */
export function scorePercent(correct: number, total: number): number {
  if (total <= 0 || correct <= 0) return 0
  return Math.floor((Math.min(correct, total) * 100) / total)
}

/**
 * Đạt khi `correct * 100 >= threshold * total` bằng số nguyên, KHÔNG làm tròn (R-LS3): 4/5 = 80 đạt; 7/9 = 77 không.
 * Server là nguồn sự thật (`passed`) — hàm này chỉ để hiển thị/kiểm tra nhất quán.
 */
export function isPassed(correct: number, total: number, threshold = PASS_THRESHOLD_PERCENT): boolean {
  if (total <= 0) return false
  return correct * 100 >= threshold * total
}

/** Số câu đúng tối thiểu để đạt ngưỡng (hiện "cần đúng ≥ N/M câu"). */
export function minCorrectToPass(total: number, threshold = PASS_THRESHOLD_PERCENT): number {
  if (total <= 0) return 0
  return Math.ceil((threshold * total) / 100)
}

/** Số câu chưa trả lời trong lượt hiện tại (nút "Nộp bài" khoá cho tới khi = 0). */
export function countUnanswered(questions: readonly QuizQuestion[], answers: ReadonlyMap<string, string>): number {
  let n = 0
  for (const q of questions) if (!answers.has(q.id)) n++
  return n
}

/** Chỉ số câu đầu tiên chưa trả lời, `-1` khi đã đủ. */
export function firstUnansweredIndex(questions: readonly QuizQuestion[], answers: ReadonlyMap<string, string>): number {
  return questions.findIndex((q) => !answers.has(q.id))
}

/** Đóng gói đáp án theo ĐÚNG thứ tự câu hỏi của lượt (R-LS8: mỗi câu đúng một lần). */
export function buildAnswers(questions: readonly QuizQuestion[], answers: ReadonlyMap<string, string>): QuizAnswer[] {
  const out: QuizAnswer[] = []
  for (const q of questions) {
    const optionId = answers.get(q.id)
    if (optionId !== undefined) out.push({ questionId: q.id, optionId })
  }
  return out
}
