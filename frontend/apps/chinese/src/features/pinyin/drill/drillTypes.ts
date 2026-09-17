import type { DrillMode, DrillTone } from '../types'
import type { DrillItem } from './generateDrill'

/** Phiên luyện đang chạy — sinh ở client khi bấm "Bắt đầu" (R5-8, R5-10). */
export interface DrillSession {
  /** `crypto.randomUUID()` — khoá idempotent khi nộp. */
  clientSessionId: string
  mode: DrillMode
  items: DrillItem[]
  startedAt: Date
}

/** Một câu đã trả lời (đủ mọi phần). */
export interface AnsweredItem {
  item: DrillItem
  /** Thanh đã chọn cho từng phần, cùng thứ tự `item.parts`. */
  answered: DrillTone[]
  /** Đúng khi MỌI phần đúng (R5-9). */
  correct: boolean
  /** Từ lúc phát xong lần đầu tới lúc chọn đủ; `null` nếu chưa từng phát. */
  responseMs: number | null
  replayCount: number
}

export interface DrillOutcome {
  session: DrillSession
  answers: AnsweredItem[]
  finishedAt: Date
}

/** Cận trên `responseMs` backend chấp nhận (validator: 0..600000). */
export const RESPONSE_MS_MAX = 600_000

/**
 * Thời gian trả lời của CÂU (ms): từ lúc phát xong lần đầu tới lúc chọn đủ. Chưa từng phát ⇒ `null`.
 * Quá 10 phút (để yên rồi quay lại) ⇒ `null` — gửi giá trị ngoài 0..600000 là 400 và mất cả phiên.
 */
export function computeResponseMs(firstPlayEndAt: number | null, now: number): number | null {
  if (firstPlayEndAt === null) return null
  const ms = Math.round(now - firstPlayEndAt)
  if (!Number.isFinite(ms) || ms < 0) return null
  return ms > RESPONSE_MS_MAX ? null : ms
}

/** Thống kê theo PHẦN tính ở client (hiện tạm trong lúc chờ server, và khi gửi lỗi). */
export function summarizeByTone(answers: AnsweredItem[]): Record<DrillTone, { total: number; correct: number }> {
  const out: Record<DrillTone, { total: number; correct: number }> = { 1: { total: 0, correct: 0 }, 2: { total: 0, correct: 0 }, 3: { total: 0, correct: 0 }, 4: { total: 0, correct: 0 } }
  for (const a of answers) {
    a.item.parts.forEach((p, i) => {
      out[p.tone].total++
      if (a.answered[i] === p.tone) out[p.tone].correct++
    })
  }
  return out
}
