import type { ReactNode } from 'react'
import { Button } from '@mui/material'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { ErrorPage } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useAuth } from '../AuthProvider'
import { buildLoginUrl } from '../returnTo'
import type { MeInfo } from '../types'
import { FullScreenLoading } from './FullScreenLoading'

export interface RequireAuthProps {
  /** Đường dẫn trang đăng nhập. Mặc định `/dang-nhap`. */
  loginPath?: string
  /** Đích khi đã đăng nhập nhưng KHÔNG có quyền nào ở service này (tài khoản bị gỡ hết vai trò). Mặc định `/403`. */
  forbiddenPath?: string
  /**
   * W2 (apps/admin): cho phép đi tiếp dù `permissions` rỗng khi hàm trả `true` — admin gộp `/me` của nhiều service,
   * có service KHÔNG PHẢN HỒI thì 0 quyền chưa chắc là "chưa được cấp vai trò" (không đẩy `/403` oan khi service
   * chết; Dashboard tự hiện cảnh báo). Bỏ trống ⇒ hành vi cũ: 0 quyền luôn về `forbiddenPath` (apps/chinese).
   */
  allowNoPermissions?: (me: MeInfo) => boolean
  /** Bỏ trống ⇒ render `<Outlet />` (dùng làm layout route). */
  children?: ReactNode
}

/**
 * Chặn route cần đăng nhập (hợp đồng §5.3.1):
 * - `loading` ⇒ màn chờ; `anonymous` ⇒ `/dang-nhap?returnTo=<hiện tại>&reason=expired` (nếu vừa mất phiên);
 * - `authenticated` mà `loadMe` lỗi ⇒ trang lỗi có "Thử lại"/"Đăng xuất" (không đẩy sang /403 oan khi service ngôn ngữ chưa chạy);
 * - `authenticated` mà 0 quyền ⇒ `/403`.
 */
export function RequireAuth({ loginPath = '/dang-nhap', forbiddenPath = '/403', allowNoPermissions, children }: RequireAuthProps) {
  const { status, permissions, me, meLoading, meError, lostReason, reloadMe, logout } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <FullScreenLoading />
  if (status === 'anonymous') return <Navigate to={buildLoginUrl(loginPath, location, lostReason)} replace />

  if (meLoading && !me) return <FullScreenLoading label="Đang tải hồ sơ học tập…" />

  if (!me && meError) {
    const parsed = parseApiError(meError)
    return (
      <ErrorPage
        code={parsed.status && parsed.status >= 400 ? parsed.status : 503}
        title="Không tải được hồ sơ"
        description={`Đã đăng nhập nhưng chưa lấy được hồ sơ và quyền từ dịch vụ học tập: ${parsed.message}`}
        onLogout={() => void logout()}
      >
        <Button variant="outlined" onClick={() => void reloadMe()}>
          Thử lại
        </Button>
      </ErrorPage>
    )
  }

  if (permissions.size === 0 && !(me && allowNoPermissions?.(me) === true)) return <Navigate to={forbiddenPath} replace />

  return children ?? <Outlet />
}
