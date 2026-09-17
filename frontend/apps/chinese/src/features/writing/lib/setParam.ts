import type { WritingSet } from '../types'

/** Tab của `/luyen-viet?tab=` (§5.3.2). */
export const HOME_TABS = ['hsk1', 'bai-hoc', 'can-luyen', 'da-luyen'] as const
export type HomeTab = (typeof HOME_TABS)[number]

/** Bước của `/luyen-viet/:hanzi?tab=` (R-W2). */
export const STEP_TABS = ['xem', 'to-theo', 'tu-viet'] as const
export type StepTab = (typeof STEP_TABS)[number]

/** Slug bài hợp lệ: chữ thường/số/gạch nối (khớp quy ước `content/chinese/data/lessons/*.json`). */
const SLUG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

export function isLessonSlug(value: string | null | undefined): value is string {
  return typeof value === 'string' && value.length > 0 && value.length <= 64 && SLUG_RE.test(value)
}

/**
 * Tab trang chủ ⇒ `set` gửi API (R-W6). `bai-hoc` cần slug bài — thiếu/không hợp lệ ⇒ `null` (trang hiện ô chọn bài,
 * không gọi API).
 */
export function tabToSet(tab: HomeTab, bai?: string | null): WritingSet | null {
  switch (tab) {
    case 'hsk1':
      return 'hsk1'
    case 'can-luyen':
      return 'weak'
    case 'da-luyen':
      return 'practiced'
    case 'bai-hoc':
      return isLessonSlug(bai) ? `lesson:${bai}` : null
  }
}

/**
 * `set` API ⇒ tham số `tu=` của trang luyện (`hsk1 | bai:<slug> | can-luyen | da-luyen`) để "Chữ tiếp" biết
 * đang đi trong bộ nào và "Quay lại" về đúng tab.
 */
export function setToTuParam(set: WritingSet): string {
  if (set === 'hsk1') return 'hsk1'
  if (set === 'weak') return 'can-luyen'
  if (set === 'practiced') return 'da-luyen'
  return `bai:${set.slice('lesson:'.length)}`
}

/** Ngược lại của `setToTuParam`; giá trị lạ/slug sai ⇒ `null` (không có "Chữ tiếp"). */
export function tuParamToSet(tu: string | null | undefined): WritingSet | null {
  if (!tu) return null
  if (tu === 'hsk1') return 'hsk1'
  if (tu === 'can-luyen') return 'weak'
  if (tu === 'da-luyen') return 'practiced'
  if (tu.startsWith('bai:')) {
    const slug = tu.slice(4)
    return isLessonSlug(slug) ? `lesson:${slug}` : null
  }
  return null
}

/** `set` ⇒ query string của trang chủ luyện viết (`?tab=bai-hoc&bai=<slug>`; `hsk1` là tab mặc định ⇒ rỗng). */
export function setToHomeSearch(set: WritingSet | null): string {
  if (!set || set === 'hsk1') return ''
  if (set === 'weak') return '?tab=can-luyen'
  if (set === 'practiced') return '?tab=da-luyen'
  const slug = set.slice('lesson:'.length)
  return `?tab=bai-hoc&bai=${encodeURIComponent(slug)}`
}

/** Đường dẫn trang luyện một chữ (`:hanzi` encode; `tu` gắn khi biết bộ). */
export function practicePath(hanzi: string, set?: WritingSet | null): string {
  const base = `/luyen-viet/${encodeURIComponent(hanzi)}`
  return set ? `${base}?tu=${encodeURIComponent(setToTuParam(set))}` : base
}

/** Đọc `page` từ URL: số nguyên ≥ 1, sai ⇒ 1. */
export function parsePageParam(raw: string | null): number {
  const n = Number(raw)
  return Number.isInteger(n) && n >= 1 ? n : 1
}
