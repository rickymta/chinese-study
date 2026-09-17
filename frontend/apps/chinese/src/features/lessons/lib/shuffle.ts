import type { QuizOption, QuizQuestion } from '../types'

/** Nguồn ngẫu nhiên `[0, 1)` — mặc định `Math.random`; test truyền hàm giả để có kết quả tất định. */
export type Rng = () => number

/** Fisher–Yates, KHÔNG sửa mảng vào; `rng` trả `[0, 1)`. */
export function shuffle<T>(items: readonly T[], rng: Rng = Math.random): T[] {
  const out = items.slice()
  for (let i = out.length - 1; i > 0; i--) {
    const r = rng()
    // Kẹp về [0, i] phòng rng trả 1 (hoặc giá trị lạ) ⇒ không bao giờ đọc ngoài mảng.
    const j = Math.min(i, Math.max(0, Math.floor(r * (i + 1))))
    const tmp = out[i]!
    out[i] = out[j]!
    out[j] = tmp
  }
  return out
}

/** Thứ tự HIỂN THỊ lựa chọn theo từng câu — xáo MỘT LẦN lúc bắt đầu lượt, giữ nguyên khi quay lại câu trước. */
export type OptionOrder = Record<string, QuizOption[]>

export function shuffleOptions(questions: readonly QuizQuestion[], rng: Rng = Math.random): OptionOrder {
  const out: OptionOrder = {}
  for (const q of questions) out[q.id] = shuffle(q.options, rng)
  return out
}

/**
 * Rng tất định (LCG 32-bit — Numerical Recipes) cho test và "làm lại cùng thứ tự" nếu cần.
 * Không dùng cho mục đích bảo mật.
 */
export function seededRng(seed: number): Rng {
  let state = seed >>> 0
  return () => {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0
    return state / 0x1_0000_0000
  }
}
