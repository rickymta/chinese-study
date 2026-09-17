// Hàng đợi gửi lại đánh giá thẻ (hợp đồng F6/F7 §5.3.2): chấm thẻ là LẠC QUAN — chuyển thẻ kế ngay, gửi ở nền;
// mất mạng/5xx thì giữ lại trong `sessionStorage['af.srs.outbox']` và thử lại 1 s, 2 s, 5 s, 10 s, rồi mỗi 30 s.
// Phần này THUẦN (không React) để test được; hook `useReviewOutbox` lo timer/sự kiện `online`.
import { isApiError } from '@af/api'
import type { ReviewResponse, SrsRating } from '../types'

export interface OutboxItem {
  /** UUID sinh một lần khi chấm (`crypto.randomUUID()`), GIỮ NGUYÊN mọi lần gửi lại (server idempotent — R7-8). */
  clientReviewId: string
  cardId: string
  rating: SrsRating
  durationMs: number
  /** Số lần gửi thất bại (để tính khoảng chờ). */
  attempts: number
}

/** Tối thiểu của `Storage` — test truyền bộ nhớ giả. */
export interface StorageLike {
  getItem(key: string): string | null
  setItem(key: string, value: string): void
  removeItem(key: string): void
}

export const OUTBOX_STORAGE_KEY = 'af.srs.outbox'

/** Khoảng chờ trước lần gửi lại thứ n (n = số lần đã thất bại). Hết bậc thang ⇒ mỗi 30 s. */
export const RETRY_DELAYS_MS: readonly number[] = [1_000, 2_000, 5_000, 10_000]
export const RETRY_STEADY_MS = 30_000

export function retryDelayMs(attempts: number): number {
  const n = Math.max(1, Math.floor(attempts))
  return RETRY_DELAYS_MS[n - 1] ?? RETRY_STEADY_MS
}

const VALID_RATINGS = new Set<string>(['again', 'hard', 'good', 'easy'])

function isOutboxItem(v: unknown): v is OutboxItem {
  if (!v || typeof v !== 'object') return false
  const o = v as Record<string, unknown>
  return (
    typeof o.clientReviewId === 'string' &&
    o.clientReviewId.length > 0 &&
    typeof o.cardId === 'string' &&
    o.cardId.length > 0 &&
    typeof o.rating === 'string' &&
    VALID_RATINGS.has(o.rating) &&
    typeof o.durationMs === 'number' &&
    typeof o.attempts === 'number'
  )
}

/** `sessionStorage` bọc try/catch (Safari riêng tư / SSR) — không có ⇒ `null` (hàng đợi chỉ sống trong bộ nhớ). */
export function defaultOutboxStorage(): StorageLike | null {
  try {
    return typeof sessionStorage === 'undefined' ? null : sessionStorage
  } catch {
    return null
  }
}

/** Đọc hàng đợi đã lưu; dữ liệu hỏng/khác dạng ⇒ mảng rỗng (không ném). */
export function readOutbox(storage: StorageLike | null = defaultOutboxStorage()): OutboxItem[] {
  if (!storage) return []
  try {
    const raw = storage.getItem(OUTBOX_STORAGE_KEY)
    if (!raw) return []
    const parsed: unknown = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.filter(isOutboxItem) : []
  } catch {
    return []
  }
}

/** Ghi hàng đợi; rỗng ⇒ xoá khoá. Lỗi lưu trữ bị nuốt (chỉ mất khả năng gửi lại sau khi tải lại trang). */
export function writeOutbox(items: readonly OutboxItem[], storage: StorageLike | null = defaultOutboxStorage()): void {
  if (!storage) return
  try {
    if (items.length === 0) storage.removeItem(OUTBOX_STORAGE_KEY)
    else storage.setItem(OUTBOX_STORAGE_KEY, JSON.stringify(items))
  } catch {
    /* bỏ qua */
  }
}

/** Thêm vào cuối hàng đợi (thuần, trả mảng mới). Trùng `clientReviewId` ⇒ giữ nguyên, không thêm lần hai. */
export function enqueue(items: readonly OutboxItem[], item: OutboxItem): OutboxItem[] {
  if (items.some((it) => it.clientReviewId === item.clientReviewId)) return [...items]
  return [...items, { ...item, attempts: item.attempts ?? 0 }]
}

/**
 * Lỗi đáng thử lại: không có phản hồi (mất mạng, timeout), 5xx, 408, 429. Các 4xx còn lại (400/404/409/422…)
 * là câu trả lời thật ⇒ bỏ phần tử và báo người dùng.
 */
export function isRetryableError(err: unknown): boolean {
  if (isApiError(err)) {
    const s = err.status
    return s === undefined || s >= 500 || s === 408 || s === 429
  }
  // Lỗi không phải ApiError (ném từ code) — không lặp vô hạn.
  return false
}

export type SendReview = (item: OutboxItem) => Promise<ReviewResponse>

export interface FlushOutcome {
  /** Phần tử còn lại (đã tăng `attempts` ở phần tử gây dừng). */
  remaining: OutboxItem[]
  sent: { item: OutboxItem; response: ReviewResponse }[]
  dropped: { item: OutboxItem; error: unknown }[]
  /** `null` ⇒ không cần hẹn gửi lại (hàng đợi trống hoặc chỉ toàn lỗi bị bỏ). */
  retryAfterMs: number | null
}

/**
 * Gửi TUẦN TỰ theo thứ tự chấm (lượt sau của cùng một thẻ phải tới sau lượt trước). Lỗi đáng thử lại ⇒ DỪNG tại
 * phần tử đó (giữ nó và mọi phần tử sau), tăng `attempts`, trả khoảng chờ. Lỗi 4xx khác ⇒ bỏ phần tử, đi tiếp.
 */
export async function flushOutbox(
  items: readonly OutboxItem[],
  send: SendReview,
  isRetryable: (err: unknown) => boolean = isRetryableError,
): Promise<FlushOutcome> {
  const sent: FlushOutcome['sent'] = []
  const dropped: FlushOutcome['dropped'] = []
  for (let i = 0; i < items.length; i++) {
    const item = items[i]!
    try {
      const response = await send(item)
      sent.push({ item, response })
    } catch (error) {
      if (isRetryable(error)) {
        const failed = { ...item, attempts: item.attempts + 1 }
        return { remaining: [failed, ...items.slice(i + 1)], sent, dropped, retryAfterMs: retryDelayMs(failed.attempts) }
      }
      dropped.push({ item, error })
    }
  }
  return { remaining: [], sent, dropped, retryAfterMs: null }
}
