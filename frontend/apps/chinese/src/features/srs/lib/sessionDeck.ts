// Quy tắc bộ bài của một phiên ôn (hợp đồng F6/F7 §5.3.2 "Bộ bài") — thuần để test.
import type { SrsCardState, SrsQueueCard, SrsRating } from '../types'

/** Còn ≤ 5 thẻ chưa chấm ⇒ tải thêm. */
export const LOAD_MORE_THRESHOLD = 5

/** Thẻ đang học dở (`learning`/`relearning`) được phép quay lại phiên sau khi đã chấm (nhóm "học trước" của server). */
export function isRelearnableState(state: SrsCardState): boolean {
  return state === 'learning' || state === 'relearning'
}

/**
 * Lọc thẻ tải thêm trước khi nối vào bộ bài:
 * - bỏ `cardId` đang có trong phần bộ bài CHƯA chấm (`unrated` = `deck.slice(index)`, kể cả thẻ hiện tại và bản
 *   quay lại của thẻ đã chấm trước đó — không lọc theo `ratedIds`, nếu không thẻ learning bị nối thêm lần 3),
 * - bỏ thẻ đang chờ gửi trong outbox (server chưa biết lượt vừa chấm ⇒ trạng thái trả về đã cũ),
 * - thẻ đã chấm trong phiên chỉ được quay lại khi server trả `learning`/`relearning` (thẻ `again` học lại sau 1 phút),
 * - `skipNew` (trang bật khi TẢI THÊM mà có lượt chấm chưa tới server trong lúc gọi): bỏ thẻ `new`. Server chưa đếm
 *   các lượt đó vào "từ mới hôm nay" nên cấp dư thẻ mới ⇒ lượt chấm thẻ dư bị 422 NEW_CARD_LIMIT_REACHED (tích hợp
 *   F7: chấm offline rồi bật mạng). Trang tự tải lại khi outbox trống, lúc đó số đếm của server đã đúng.
 */
export function mergeIncoming(
  unrated: readonly SrsQueueCard[],
  ratedIds: ReadonlySet<string>,
  pendingIds: ReadonlySet<string>,
  incoming: readonly SrsQueueCard[],
  skipNew = false,
): SrsQueueCard[] {
  const inDeck = new Set(unrated.map((c) => c.cardId))
  const out: SrsQueueCard[] = []
  for (const card of incoming) {
    if (inDeck.has(card.cardId)) continue
    if (skipNew && card.state === 'new') continue
    if (pendingIds.has(card.cardId)) continue
    if (ratedIds.has(card.cardId) && !isRelearnableState(card.state)) continue
    inDeck.add(card.cardId)
    out.push(card)
  }
  return out
}

/** Có nên gọi tải thêm không: còn ít thẻ, không đang tải, server chưa báo hết. */
export function shouldLoadMore(remaining: number, loading: boolean, exhausted: boolean): boolean {
  return !loading && !exhausted && remaining <= LOAD_MORE_THRESHOLD
}

export type RatingCounts = Record<SrsRating, number>

export function countRatings(ratings: readonly SrsRating[]): RatingCounts {
  const counts: RatingCounts = { again: 0, hard: 0, good: 0, easy: 0 }
  for (const r of ratings) counts[r]++
  return counts
}

/** "3 phút 20 giây" / "45 giây" / "1 giờ 2 phút" — thời gian phiên ở màn tổng kết. */
export function formatSessionDuration(ms: number): string {
  const total = Math.max(0, Math.round(ms / 1000))
  const h = Math.floor(total / 3600)
  const m = Math.floor((total % 3600) / 60)
  const s = total % 60
  if (h > 0) return m > 0 ? `${h} giờ ${m} phút` : `${h} giờ`
  if (m > 0) return s > 0 ? `${m} phút ${s} giây` : `${m} phút`
  return `${s} giây`
}

/** Kẹp `durationMs` theo luật server (0..600000) để 400 không xảy ra vì người học để thẻ mở quá lâu. */
export const DURATION_MS_MAX = 600_000
export function clampDurationMs(ms: number): number {
  if (!Number.isFinite(ms)) return 0
  return Math.min(DURATION_MS_MAX, Math.max(0, Math.round(ms)))
}
