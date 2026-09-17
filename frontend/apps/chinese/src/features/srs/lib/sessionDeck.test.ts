import { describe, expect, it } from 'vitest'
import { clampDurationMs, countRatings, formatSessionDuration, mergeIncoming, shouldLoadMore } from './sessionDeck'
import type { SrsCardState, SrsQueueCard } from '../types'

const card = (cardId: string, state: SrsCardState = 'review'): SrsQueueCard => ({
  cardId,
  state,
  queue: state === 'new' ? 'new' : state === 'review' ? 'review' : 'learning',
  dueAt: '2026-09-18T00:00:00Z',
  word: { id: 'w-' + cardId, simplified: '爱', pinyin: 'ai4', meaningViStatus: 'machine' },
  intervals: { again: 'PT1M', hard: 'PT5M30S', good: 'PT10M', easy: 'P8D' },
})

describe('mergeIncoming', () => {
  it('bỏ thẻ đã có trong bộ (chưa chấm)', () => {
    const deck = [card('a'), card('b')]
    const out = mergeIncoming(deck, new Set(), new Set(), [card('b'), card('c')])
    expect(out.map((c) => c.cardId)).toEqual(['c'])
  })

  it('thẻ đã chấm chỉ quay lại khi server trả learning/relearning', () => {
    // Cả a lẫn b đã chấm ⇒ phần chưa chấm rỗng.
    const rated = new Set(['a', 'b'])
    const out = mergeIncoming([], rated, new Set(), [card('a', 'learning'), card('b', 'review'), card('c', 'relearning')])
    expect(out.map((c) => c.cardId)).toEqual(['a', 'c'])
  })

  it('bỏ thẻ đang chờ gửi trong outbox', () => {
    const out = mergeIncoming([], new Set(['a']), new Set(['a']), [card('a', 'learning'), card('b')])
    expect(out.map((c) => c.cardId)).toEqual(['b'])
  })

  it('bản quay lại của thẻ đã chấm đang nằm trong phần chưa chấm ⇒ không nối thêm lần nữa (review F7)', () => {
    // Bộ bài [a, b, a(learning)], index = 1 ⇒ phần chưa chấm = [b, a(learning)], rated = {a}.
    const deck = [card('a', 'new'), card('b'), card('a', 'learning')]
    const unrated = deck.slice(1)
    const out = mergeIncoming(unrated, new Set(['a']), new Set(), [card('a', 'learning')])
    expect(out).toEqual([])
  })

  it('skipNew ⇒ bỏ thẻ new (server đếm từ mới chưa kịp khi còn lượt chấm đang bay) — tích hợp F7', () => {
    const incoming = [card('n1', 'new'), card('r1', 'review'), card('l1', 'learning')]
    expect(mergeIncoming([], new Set(['p']), new Set(['p']), incoming, true).map((c) => c.cardId)).toEqual(['r1', 'l1'])
    // Không bật cờ (tải lần đầu / không có lượt đang bay) ⇒ không lọc.
    expect(mergeIncoming([], new Set(), new Set(['p']), incoming).map((c) => c.cardId)).toEqual(['n1', 'r1', 'l1'])
  })

  it('không thêm trùng trong cùng một lô tải', () => {
    const out = mergeIncoming([], new Set(), new Set(), [card('x'), card('x')])
    expect(out).toHaveLength(1)
  })
})

describe('shouldLoadMore', () => {
  it('còn ≤ 5 thẻ, không đang tải, chưa hết ⇒ tải', () => {
    expect(shouldLoadMore(5, false, false)).toBe(true)
    expect(shouldLoadMore(0, false, false)).toBe(true)
    expect(shouldLoadMore(6, false, false)).toBe(false)
    expect(shouldLoadMore(2, true, false)).toBe(false)
    expect(shouldLoadMore(2, false, true)).toBe(false)
  })
})

describe('countRatings / formatSessionDuration / clampDurationMs', () => {
  it('đếm theo 4 mức', () => {
    expect(countRatings(['good', 'again', 'good', 'easy'])).toEqual({ again: 1, hard: 0, good: 2, easy: 1 })
  })

  it('định dạng thời gian phiên', () => {
    expect(formatSessionDuration(45_000)).toBe('45 giây')
    expect(formatSessionDuration(200_000)).toBe('3 phút 20 giây')
    expect(formatSessionDuration(120_000)).toBe('2 phút')
    expect(formatSessionDuration(3_720_000)).toBe('1 giờ 2 phút')
    expect(formatSessionDuration(-5)).toBe('0 giây')
  })

  it('kẹp durationMs 0..600000', () => {
    expect(clampDurationMs(-10)).toBe(0)
    expect(clampDurationMs(4200.6)).toBe(4201)
    expect(clampDurationMs(999_999)).toBe(600_000)
    expect(clampDurationMs(Number.NaN)).toBe(0)
  })
})
