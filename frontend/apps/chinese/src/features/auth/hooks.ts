import { useAuth } from '@af/auth'
import type { ChineseMe } from './types'

/**
 * `useAuth().me` đã gắn kiểu của service tiếng Trung. `null` khi chưa đăng nhập, đang tải hoặc `loadMe` lỗi
 * (`RequireAuth` đã chặn hai trường hợp sau nên trong trang học có thể coi là luôn có).
 */
export function useMe(): ChineseMe | null {
  const { me } = useAuth()
  return (me as ChineseMe | null) ?? null
}
