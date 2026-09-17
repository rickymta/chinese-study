import type { SrsRating } from '../types'

export type RatingColor = 'error' | 'warning' | 'success' | 'info'

/** Nhãn Việt, màu và phím tắt của 4 mức chấm (hợp đồng §5.3.2). */
export const RATING_META: Record<SrsRating, { label: string; color: RatingColor; key: '1' | '2' | '3' | '4' }> = {
  again: { label: 'Quên', color: 'error', key: '1' },
  hard: { label: 'Khó', color: 'warning', key: '2' },
  good: { label: 'Được', color: 'success', key: '3' },
  easy: { label: 'Dễ', color: 'info', key: '4' },
}

/** Phím `1`–`4` ⇒ mức chấm; phím khác ⇒ `null`. */
export function ratingFromKey(key: string): SrsRating | null {
  switch (key) {
    case '1':
      return 'again'
    case '2':
      return 'hard'
    case '3':
      return 'good'
    case '4':
      return 'easy'
    default:
      return null
  }
}
