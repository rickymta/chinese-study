// Kiểu dữ liệu F8 theo hợp đồng chi tiết F8–F11 §6.2 (chinese-backend `/api/writing/*`).
// Serializer backend bật `WhenWritingNull` ⇒ trường có thể `null` bị LƯỢC khỏi JSON: khai `?:`/`| null`.
// Pinyin luôn dạng SỐ THANH (`ai4`) — hiển thị dạng dấu qua `lib/pinyin.ts`.

import type { MeaningViStatus } from '@af/chinese-kit'

/** Bước ghi DB (R-W2): Tô theo = `guided`, Tự viết = `recall`; bước Xem không ghi. */
export type WritingMode = 'guided' | 'recall'

/** Trạng thái thuộc chữ (R-W5): `mastered` khi tự viết sạch ở ≥ 2 ngày khác nhau. */
export type MasteryStatus = 'new' | 'practicing' | 'mastered'

/** Bộ chữ gửi lên `GET /api/writing/characters?set=` (R-W6). */
export type WritingSet = 'hsk1' | `lesson:${string}` | 'weak' | 'practiced'

/** Thống kê luyện viết của người học cho một chữ (`character_writing_stats`). */
export interface WritingStats {
  hanzi: string
  attempts: number
  guidedAttempts: number
  recallAttempts: number
  lastMistakes: number
  lastHints: number
  /** `null` khi chưa có lần tự viết nào. */
  bestRecallMistakes?: number | null
  cleanRecallDays: number
  masteryStatus: MasteryStatus
  isWeak: boolean
  firstPracticedAt: string
  lastPracticedAt: string
}

/** Một ô trong danh sách chữ theo bộ. `lastMistakes`/`lastPracticedAt` vắng khi `masteryStatus = new`. */
export interface WritingCharacterItem {
  hanzi: string
  pinyinReadings?: string[] | null
  hanViet?: string[] | null
  strokeCount?: number | null
  masteryStatus: MasteryStatus
  lastMistakes?: number | null
  lastPracticedAt?: string | null
}

export interface WritingCharactersResponse {
  set: string
  items: WritingCharacterItem[]
  page: number
  pageSize: number
  totalCount: number
}

/** Từ chứa chữ (≤ 5) trong chi tiết chữ luyện viết. */
export interface WritingCharacterWord {
  id: string
  simplified: string
  pinyin: string
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
  hsk3Level?: number | null
}

export interface WritingCharacterDetail {
  hanzi: string
  traditionalVariants?: string[] | null
  pinyinReadings?: string[] | null
  hanViet?: string[] | null
  strokeCount?: number | null
  radical?: string | null
  words?: WritingCharacterWord[] | null
  /** Vắng khi người học chưa viết chữ này lần nào. */
  stats?: WritingStats | null
}

export interface RecordWritingAttemptRequest {
  /** UUID sinh bằng `crypto.randomUUID()` LÚC BẮT ĐẦU lượt; gửi lại cùng id ⇒ server trả kết quả cũ (R-W8). */
  clientAttemptId: string
  hanzi: string
  mode: WritingMode
  /** 1–64 — số nét theo dữ liệu hanzi-writer (không đối chiếu `stroke_count` Unihan, R-W7). */
  totalStrokes: number
  /** 0–500 */
  totalMistakes: number
  /** 0–200: số nét tự hiện gợi ý + số lần bấm "Gợi ý nét" (R-W3). */
  hintsUsed: number
  /** 0–3 600 000 */
  durationMs?: number | null
}

export interface RecordWritingAttemptResponse {
  attemptId: string
  completedAt: string
  /** `mode = recall` và 0 lỗi, 0 gợi ý. */
  isClean: boolean
  stats: WritingStats
  /** `true` đúng lần viết đưa chữ sang `mastered` — hiện "Đã thuộc chữ …!". */
  becameMastered: boolean
}

export interface WritingSummary {
  practicedChars: number
  masteredChars: number
  weakChars: number
  attemptsToday: number
  /** Tổng số chữ trong bộ HSK 1 (mẫu số của "đã luyện"). */
  totalChars: number
}
