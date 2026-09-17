import { describe, expect, it } from 'vitest'
import { buildAnswers, countUnanswered, firstUnansweredIndex, isPassed, minCorrectToPass, scorePercent } from './quizScore'
import type { QuizQuestion } from '@af/chinese-kit'

const q = (id: string): QuizQuestion => ({ id, type: 'single_choice', prompt: 'p', promptLang: 'vi', options: [] })

describe('scorePercent', () => {
  it('làm tròn XUỐNG (7/9 = 77, không phải 78)', () => {
    expect(scorePercent(7, 9)).toBe(77)
    expect(scorePercent(4, 5)).toBe(80)
    expect(scorePercent(6, 7)).toBe(85)
    expect(scorePercent(0, 7)).toBe(0)
    expect(scorePercent(7, 7)).toBe(100)
  })

  it('total = 0 hoặc correct âm ⇒ 0; correct > total kẹp về 100', () => {
    expect(scorePercent(3, 0)).toBe(0)
    expect(scorePercent(-1, 5)).toBe(0)
    expect(scorePercent(9, 5)).toBe(100)
  })
})

describe('isPassed', () => {
  it('so sánh số nguyên không làm tròn: 4/5 đạt, 7/9 không đạt', () => {
    expect(isPassed(4, 5)).toBe(true)
    expect(isPassed(7, 9)).toBe(false)
    expect(isPassed(8, 10)).toBe(true)
    expect(isPassed(5, 7)).toBe(false) // 71%
    expect(isPassed(6, 7)).toBe(true) // 85%
  })

  it('total = 0 ⇒ không đạt; ngưỡng tuỳ chỉnh', () => {
    expect(isPassed(0, 0)).toBe(false)
    expect(isPassed(1, 2, 50)).toBe(true)
  })
})

describe('minCorrectToPass', () => {
  it('cần đúng ≥ N câu', () => {
    expect(minCorrectToPass(5)).toBe(4)
    expect(minCorrectToPass(7)).toBe(6)
    expect(minCorrectToPass(9)).toBe(8)
    expect(minCorrectToPass(10)).toBe(8)
    expect(minCorrectToPass(0)).toBe(0)
  })
})

describe('countUnanswered / firstUnansweredIndex / buildAnswers', () => {
  const questions = [q('a'), q('b'), q('c')]

  it('đếm và tìm câu chưa trả lời', () => {
    const answers = new Map([['a', 'x'], ['c', 'y']])
    expect(countUnanswered(questions, answers)).toBe(1)
    expect(firstUnansweredIndex(questions, answers)).toBe(1)
    expect(firstUnansweredIndex(questions, new Map([['a', '1'], ['b', '2'], ['c', '3']]))).toBe(-1)
  })

  it('đóng gói theo thứ tự câu hỏi, bỏ câu chưa trả lời, không có đáp án thừa', () => {
    const answers = new Map([['c', 'z'], ['a', 'x'], ['zz', 'thừa']])
    expect(buildAnswers(questions, answers)).toEqual([
      { questionId: 'a', optionId: 'x' },
      { questionId: 'c', optionId: 'z' },
    ])
  })
})
