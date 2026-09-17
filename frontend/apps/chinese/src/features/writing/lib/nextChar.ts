/**
 * "Chữ tiếp" trong cùng bộ (§5.3.2): chữ đứng SAU chữ hiện tại (theo thứ tự bộ) có dữ liệu nét. Không vòng lại
 * đầu danh sách — hết ⇒ `null` (trang đưa về `/luyen-viet?tab=…`). Chữ hiện tại không có trong danh sách
 * (đổi bộ, chữ bị gỡ) ⇒ lấy chữ đầu tiên có dữ liệu nét.
 */
export function findNextPracticable(
  items: ReadonlyArray<{ hanzi: string }>,
  current: string,
  hasData: (hanzi: string) => boolean,
): string | null {
  const idx = items.findIndex((it) => it.hanzi === current)
  for (let i = idx + 1; i < items.length; i++) {
    const h = items[i]!.hanzi
    if (h !== current && hasData(h)) return h
  }
  return null
}

/** Vị trí `1-based/n` của chữ trong bộ để hiện "Chữ 3/25"; không có ⇒ `null`. */
export function positionInSet(items: ReadonlyArray<{ hanzi: string }>, current: string): { index: number; total: number } | null {
  const idx = items.findIndex((it) => it.hanzi === current)
  return idx < 0 ? null : { index: idx + 1, total: items.length }
}
