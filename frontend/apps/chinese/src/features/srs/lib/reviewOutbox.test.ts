import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '@af/api'
import {
  OUTBOX_STORAGE_KEY,
  enqueue,
  flushOutbox,
  isRetryableError,
  readOutbox,
  retryDelayMs,
  writeOutbox,
  type OutboxItem,
  type StorageLike,
} from './reviewOutbox'
import type { ReviewResponse } from '../types'

function memoryStorage(initial: Record<string, string> = {}): StorageLike & { data: Record<string, string> } {
  const data = { ...initial }
  return {
    data,
    getItem: (k) => (k in data ? data[k]! : null),
    setItem: (k, v) => {
      data[k] = v
    },
    removeItem: (k) => {
      delete data[k]
    },
  }
}

const item = (id: string, cardId = 'card-' + id, attempts = 0): OutboxItem => ({
  clientReviewId: id,
  cardId,
  rating: 'good',
  durationMs: 1200,
  attempts,
})

const okResponse = (cardId: string): ReviewResponse =>
  ({ reviewId: 'r', duplicate: false, card: { cardId }, summary: {} }) as unknown as ReviewResponse

describe('retryDelayMs', () => {
  it('bậc thang 1 s, 2 s, 5 s, 10 s rồi mỗi 30 s', () => {
    expect(retryDelayMs(1)).toBe(1_000)
    expect(retryDelayMs(2)).toBe(2_000)
    expect(retryDelayMs(3)).toBe(5_000)
    expect(retryDelayMs(4)).toBe(10_000)
    expect(retryDelayMs(5)).toBe(30_000)
    expect(retryDelayMs(99)).toBe(30_000)
    expect(retryDelayMs(0)).toBe(1_000)
  })
})

describe('enqueue', () => {
  it('thêm vào cuối, không sửa mảng gốc', () => {
    const a = [item('a')]
    const b = enqueue(a, item('b'))
    expect(b.map((x) => x.clientReviewId)).toEqual(['a', 'b'])
    expect(a).toHaveLength(1)
  })

  it('trùng clientReviewId không thêm lần hai', () => {
    const a = enqueue([item('a')], item('a'))
    expect(a).toHaveLength(1)
  })
})

describe('read/write outbox', () => {
  it('lưu và đọc lại qua storage', () => {
    const storage = memoryStorage()
    writeOutbox([item('a'), item('b')], storage)
    expect(readOutbox(storage).map((x) => x.clientReviewId)).toEqual(['a', 'b'])
  })

  it('rỗng ⇒ xoá khoá; dữ liệu hỏng ⇒ mảng rỗng, không ném', () => {
    const storage = memoryStorage({ [OUTBOX_STORAGE_KEY]: '{not json' })
    expect(readOutbox(storage)).toEqual([])
    writeOutbox([], storage)
    expect(storage.getItem(OUTBOX_STORAGE_KEY)).toBeNull()
  })

  it('lọc phần tử sai dạng', () => {
    const storage = memoryStorage({
      [OUTBOX_STORAGE_KEY]: JSON.stringify([item('a'), { clientReviewId: 'x' }, { ...item('b'), rating: 'meh' }]),
    })
    expect(readOutbox(storage).map((x) => x.clientReviewId)).toEqual(['a'])
  })

  it('storage ném lỗi (Safari riêng tư) ⇒ bỏ qua', () => {
    const broken: StorageLike = {
      getItem: () => {
        throw new Error('quota')
      },
      setItem: () => {
        throw new Error('quota')
      },
      removeItem: () => {
        throw new Error('quota')
      },
    }
    expect(readOutbox(broken)).toEqual([])
    expect(() => writeOutbox([item('a')], broken)).not.toThrow()
  })
})

describe('isRetryableError', () => {
  it('mạng/5xx/408/429 thử lại; 4xx khác bỏ', () => {
    expect(isRetryableError(new ApiError('mạng'))).toBe(true)
    expect(isRetryableError(new ApiError('x', { status: 503 }))).toBe(true)
    expect(isRetryableError(new ApiError('x', { status: 408 }))).toBe(true)
    expect(isRetryableError(new ApiError('x', { status: 429 }))).toBe(true)
    expect(isRetryableError(new ApiError('x', { status: 409 }))).toBe(false)
    expect(isRetryableError(new ApiError('x', { status: 422 }))).toBe(false)
    expect(isRetryableError(new ApiError('x', { status: 400 }))).toBe(false)
    expect(isRetryableError(new Error('bug'))).toBe(false)
  })
})

describe('flushOutbox', () => {
  it('gửi tuần tự, thành công hết ⇒ hàng đợi trống', async () => {
    const order: string[] = []
    const send = vi.fn(async (it: OutboxItem) => {
      order.push(it.clientReviewId)
      return okResponse(it.cardId)
    })
    const out = await flushOutbox([item('a'), item('b'), item('c')], send)
    expect(order).toEqual(['a', 'b', 'c'])
    expect(out.remaining).toEqual([])
    expect(out.sent).toHaveLength(3)
    expect(out.dropped).toEqual([])
    expect(out.retryAfterMs).toBeNull()
  })

  it('lỗi mạng ⇒ dừng tại phần tử đó, giữ phần sau, tăng attempts, hẹn gửi lại', async () => {
    const send = vi.fn(async (it: OutboxItem) => {
      if (it.clientReviewId === 'b') throw new ApiError('Không kết nối được máy chủ.')
      return okResponse(it.cardId)
    })
    const out = await flushOutbox([item('a'), item('b'), item('c')], send)
    expect(send).toHaveBeenCalledTimes(2)
    expect(out.sent.map((s) => s.item.clientReviewId)).toEqual(['a'])
    expect(out.remaining.map((x) => [x.clientReviewId, x.attempts])).toEqual([
      ['b', 1],
      ['c', 0],
    ])
    expect(out.retryAfterMs).toBe(1_000)
  })

  it('attempts tích luỹ qua nhiều lần ⇒ khoảng chờ tăng dần rồi giữ 30 s', async () => {
    const send = vi.fn(async () => {
      throw new ApiError('x', { status: 502 })
    })
    let items = [item('a')]
    const delays: number[] = []
    for (let i = 0; i < 6; i++) {
      const out = await flushOutbox(items, send)
      delays.push(out.retryAfterMs!)
      items = out.remaining
    }
    expect(delays).toEqual([1_000, 2_000, 5_000, 10_000, 30_000, 30_000])
    expect(items[0]!.attempts).toBe(6)
  })

  it('409/422 ⇒ bỏ phần tử, đi tiếp, không hẹn gửi lại', async () => {
    const send = vi.fn(async (it: OutboxItem) => {
      if (it.clientReviewId === 'a') throw new ApiError('Mã đánh giá đã dùng cho thẻ khác', { status: 409, code: 'CLIENT_REVIEW_ID_CONFLICT' })
      if (it.clientReviewId === 'b') throw new ApiError('Thẻ đang tạm dừng', { status: 422, code: 'CARD_SUSPENDED' })
      return okResponse(it.cardId)
    })
    const out = await flushOutbox([item('a'), item('b'), item('c')], send)
    expect(out.dropped.map((d) => d.item.clientReviewId)).toEqual(['a', 'b'])
    expect(out.sent.map((s) => s.item.clientReviewId)).toEqual(['c'])
    expect(out.remaining).toEqual([])
    expect(out.retryAfterMs).toBeNull()
  })

  it('clientReviewId không đổi giữa các lần gửi lại', async () => {
    const seen: string[] = []
    let fail = true
    const send = vi.fn(async (it: OutboxItem) => {
      seen.push(it.clientReviewId)
      if (fail) throw new ApiError('mạng')
      return okResponse(it.cardId)
    })
    const first = await flushOutbox([item('a')], send)
    fail = false
    const second = await flushOutbox(first.remaining, send)
    expect(seen).toEqual(['a', 'a'])
    expect(second.remaining).toEqual([])
  })
})
