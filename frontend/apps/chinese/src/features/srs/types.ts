// Kiểu dữ liệu F7 theo hợp đồng chi tiết F6/F7 §6.2 (chinese-backend `/api/srs/*`, `/api/me/learning-settings`).
// Serializer backend bật `WhenWritingNull` ⇒ trường có thể `null` bị LƯỢC khỏi JSON: khai `?:`/`| null`.
// Pinyin luôn dạng SỐ THANH (`ai4`) — hiển thị dạng dấu qua `lib/pinyin.ts`.

/** Mức chấm thẻ (FSRS-6): 1 Quên · 2 Khó · 3 Được · 4 Dễ. */
export type SrsRating = 'again' | 'hard' | 'good' | 'easy'
export const SRS_RATINGS: readonly SrsRating[] = ['again', 'hard', 'good', 'easy'] as const

/** Trạng thái thẻ theo FSRS. */
export type SrsCardState = 'new' | 'learning' | 'review' | 'relearning'

/** Nhóm trong hàng đợi (R7-7): 1 học dở đến hạn · 2 ôn · 3 mới · 4 học trước (≤ 20 phút). */
export type SrsQueueGroup = 'learning' | 'review' | 'new' | 'ahead'

export interface SrsSummary {
  /** Ngày địa phương của người học (`yyyy-MM-dd`) theo `timeZone`. */
  localDate: string
  timeZone: string
  dueToday: number
  dueNow: number
  /** Mọi lượt chấm hôm nay. */
  reviewedToday: number
  /** Lượt chấm hôm nay có `state_before = 'review'` (tính vào giới hạn ôn/ngày). */
  reviewsDoneToday: number
  reviewLimitRemaining: number
  dailyReviewLimit: number
  newIntroducedToday: number
  newAvailableToday: number
  dailyNewCards: number
  /** Thẻ khác `new`. */
  totalCards: number
  /** Thẻ `review` có `stability >= 21` (R7-13). */
  matureCards: number
  /** `min(due_at)` của thẻ chưa đến hạn — `null` khi không có. */
  nextDueAt?: string | null
}

/** Thông tin từ rút gọn kèm thẻ trong hàng đợi (`meaningsVi` tối đa 3). */
export interface SrsQueueWord {
  id: string
  simplified: string
  traditional?: string | null
  pinyin: string
  hanViet?: string | null
  meaningsVi?: string[] | null
  meaningViStatus: 'machine' | 'reviewed'
}

/** Khoảng dự kiến cho từng mức chấm — ISO-8601 duration (`PT10M`, `P8D`); định dạng qua `lib/formatInterval.ts`. */
export type SrsIntervals = Record<SrsRating, string>

export interface SrsQueueCard {
  cardId: string
  state: SrsCardState
  queue: SrsQueueGroup
  dueAt: string
  word: SrsQueueWord
  intervals: SrsIntervals
}

export interface SrsQueueResponse {
  generatedAt: string
  cards: SrsQueueCard[]
  summary: SrsSummary
}

export interface ReviewRequest {
  /** UUID do client sinh (`crypto.randomUUID()`), giữ nguyên khi gửi lại (R7-8). */
  clientReviewId: string
  rating: SrsRating
  /** 0..600000 ms — từ lúc thẻ hiện tới lúc bấm. */
  durationMs?: number
}

export interface SrsCardStatus {
  cardId: string
  state: SrsCardState
  step?: number | null
  dueAt: string
  /** Chưa có khi thẻ `new` (chưa từng chấm). */
  stability?: number | null
  difficulty?: number | null
  reps: number
  lapses: number
  lastReviewAt?: string | null
  isSuspended: boolean
}

export interface ReviewResponse {
  reviewId: string
  /** `true` ⇒ trùng `clientReviewId` cùng thẻ: server trả kết quả cũ, không tạo log thứ hai. */
  duplicate: boolean
  card: SrsCardStatus
  summary: SrsSummary
}

export interface AddCardsRequest {
  /** 1..100 phần tử, không trùng. */
  wordIds: string[]
}

export interface AddCardsResponse {
  added: number
  skipped: number
  cards: { wordId: string; cardId: string; created: boolean }[]
}

export interface LearningSettings {
  /** 0..50 */
  dailyNewCards: number
  /** 10..1000 */
  dailyReviewLimit: number
  /** 0.80..0.97 */
  desiredRetention: number
  /** 0.5..1.2 */
  ttsRate: number
  autoPlayAudio: boolean
}

export interface LearningSettingsResponse extends LearningSettings {
  /** `true` ⇒ người học chưa lưu cài đặt nào (đang dùng mặc định). */
  isDefault: boolean
}

/** Cài đặt mặc định của server (§6.2) — dùng khi chưa tải được để giao diện không trống. */
export const DEFAULT_LEARNING_SETTINGS: LearningSettingsResponse = {
  dailyNewCards: 10,
  dailyReviewLimit: 200,
  desiredRetention: 0.9,
  ttsRate: 0.8,
  autoPlayAudio: true,
  isDefault: true,
}
