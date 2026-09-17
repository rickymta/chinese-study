import { useEffect, useState } from 'react'

/**
 * Trả về `value` sau khi nó đứng yên `delayMs` mili giây (mặc định 300) — dùng cho ô tìm kiếm gõ tới đâu tìm tới
 * đó mà không bắn request mỗi phím. Giá trị khởi tạo trả ngay (không chờ) để tải lại trang có kết quả tức thì.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delayMs)
    return () => window.clearTimeout(id)
  }, [value, delayMs])
  return debounced
}
