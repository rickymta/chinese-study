import { useAuth } from '@af/auth'
import type { AdminMeInfo, ServiceMe } from './types'

/**
 * `useAuth().me` đã gắn kiểu gộp của admin. `null` khi chưa đăng nhập, đang tải hoặc `loadMe` lỗi (`RequireAuth`
 * đã chặn hai trường hợp sau nên trong trang quản trị có thể coi là luôn có).
 */
export function useAdminMe(): AdminMeInfo | null {
  const { me } = useAuth()
  return (me as AdminMeInfo | null) ?? null
}

/** Hồ sơ của người đang đăng nhập Ở MỘT service (`cms`, `chinese`) — `null` nếu service đó không phản hồi. */
export function useServiceMe(code: string): ServiceMe | null {
  const me = useAdminMe()
  const state = me?.services[code]
  return state?.status === 'ok' ? state.me : null
}
