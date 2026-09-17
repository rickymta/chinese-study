import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * Tab cấp trang lên URL (`?tab=...`) — quy tắc CLAUDE.md: tải lại trang/chia sẻ link giữ đúng tab, Back không
 * đi qua từng tab (dùng `replace`). Giá trị lạ ⇒ `defaultValue`; đặt về `defaultValue` ⇒ xoá khoá khỏi URL.
 * Các tham số khác trên URL được giữ nguyên. Dùng được cho mọi tham số enum (vd `che-do=mot|cap`) qua `key`.
 */
export function useTabParam<T extends string>(
  allowed: readonly T[],
  defaultValue: T,
  key = 'tab',
): [T, (value: T) => void] {
  const [params, setParams] = useSearchParams()
  const raw = params.get(key)
  const value: T = raw !== null && (allowed as readonly string[]).includes(raw) ? (raw as T) : defaultValue

  const setValue = useCallback(
    (next: T) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          if (next === defaultValue) out.delete(key)
          else out.set(key, next)
          return out
        },
        { replace: true },
      )
    },
    [setParams, defaultValue, key],
  )

  return [value, setValue]
}
