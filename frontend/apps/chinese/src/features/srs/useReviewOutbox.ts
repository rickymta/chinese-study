import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { postReview } from './api'
import {
  enqueue,
  flushOutbox,
  readOutbox,
  writeOutbox,
  type OutboxItem,
} from './lib/reviewOutbox'
import type { ReviewResponse } from './types'

export interface UseReviewOutboxOptions {
  /** Server nhận một lượt (kể cả `duplicate: true`). */
  onSent?: (item: OutboxItem, response: ReviewResponse) => void
  /** Lượt bị bỏ vì 4xx thật (409/422/400/404) — báo người dùng. */
  onDropped?: (item: OutboxItem, error: unknown) => void
}

export interface ReviewOutboxApi {
  /** Số đánh giá đang trong hàng đợi (kể cả lượt vừa chấm đang gửi lần đầu). */
  pendingCount: number
  /**
   * Số đánh giá đang KẸT: `0` khi mọi phần tử còn đang gửi lần đầu (banner không nháy mỗi lần chấm); khi có phần tử
   * `attempts > 0` (gửi hỏng, hàng đợi dừng tại đó) ⇒ bằng cả hàng đợi, vì các lượt sau cũng chờ theo (offline chấm
   * 3 thẻ ⇒ "Đang chờ gửi 3"). Dùng cho banner và hộp xác nhận khi đóng phiên.
   */
  unsentCount: number
  /** `cardId` của các lượt đang chờ — bộ bài bỏ thẻ này khi tải thêm (server chưa biết lượt vừa chấm). */
  pendingCardIds: ReadonlySet<string>
  /** Thêm một lượt chấm và gửi ngay (nền). */
  submit: (item: Omit<OutboxItem, 'attempts'>) => void
  /** Gửi ngay những gì đang chờ (vd khi có mạng lại). */
  flushNow: () => void
}

/**
 * Hook bọc `lib/reviewOutbox.ts`: giữ hàng đợi trong `sessionStorage` (mở lại phiên vẫn gửi tiếp), gửi tuần tự,
 * single-flight, hẹn gửi lại theo bậc thang 1/2/5/10 s rồi 30 s, gửi ngay khi trình duyệt báo `online`.
 * Callback `onSent`/`onDropped` đọc qua ref ⇒ không cần ổn định tham chiếu.
 */
export function useReviewOutbox(options: UseReviewOutboxOptions = {}): ReviewOutboxApi {
  const [items, setItems] = useState<OutboxItem[]>(() => readOutbox())
  const itemsRef = useRef(items)
  const flushingRef = useRef(false)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const mountedRef = useRef(true)
  const optionsRef = useRef(options)
  optionsRef.current = options

  const commit = useCallback((next: OutboxItem[]) => {
    itemsRef.current = next
    writeOutbox(next)
    if (mountedRef.current) setItems(next)
  }, [])

  const clearTimer = () => {
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }

  const flush = useCallback(async () => {
    if (flushingRef.current) return
    flushingRef.current = true
    clearTimer()
    try {
      // Lặp vì trong lúc await có thể có lượt mới được thêm vào cuối (enqueue chỉ nối đuôi).
      for (;;) {
        const snapshot = itemsRef.current
        if (snapshot.length === 0) break
        const outcome = await flushOutbox(snapshot, (it) =>
          postReview(it.cardId, { clientReviewId: it.clientReviewId, rating: it.rating, durationMs: it.durationMs }),
        )
        const appended = itemsRef.current.slice(snapshot.length)
        commit([...outcome.remaining, ...appended])
        for (const s of outcome.sent) optionsRef.current.onSent?.(s.item, s.response)
        for (const d of outcome.dropped) optionsRef.current.onDropped?.(d.item, d.error)
        if (outcome.retryAfterMs !== null) {
          if (mountedRef.current) {
            timerRef.current = setTimeout(() => {
              timerRef.current = null
              void flush()
            }, outcome.retryAfterMs)
          }
          break
        }
      }
    } finally {
      flushingRef.current = false
    }
  }, [commit])

  const submit = useCallback(
    (item: Omit<OutboxItem, 'attempts'>) => {
      commit(enqueue(itemsRef.current, { ...item, attempts: 0 }))
      void flush()
    },
    [commit, flush],
  )

  const flushNow = useCallback(() => {
    // Đang chờ bậc thang ⇒ huỷ hẹn, gửi ngay; đang gửi ⇒ vòng lặp trong flush sẽ tự lấy phần còn lại.
    clearTimer()
    void flush()
  }, [flush])

  useEffect(() => {
    mountedRef.current = true
    // Mở lại phiên với hàng đợi còn dở (sessionStorage) ⇒ gửi tiếp ngay.
    if (itemsRef.current.length > 0) void flush()
    const onOnline = () => flushNow()
    window.addEventListener('online', onOnline)
    return () => {
      mountedRef.current = false
      window.removeEventListener('online', onOnline)
      clearTimer()
    }
  }, [flush, flushNow])

  const pendingCardIds = useMemo(() => new Set(items.map((it) => it.cardId)), [items])
  const unsentCount = useMemo(() => (items.some((it) => it.attempts > 0) ? items.length : 0), [items])

  return { pendingCount: items.length, unsentCount, pendingCardIds, submit, flushNow }
}
