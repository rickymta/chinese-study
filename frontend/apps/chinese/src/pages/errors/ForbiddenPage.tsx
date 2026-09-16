import { useAuth } from '@af/auth'
import { ErrorPage } from '@af/ui'

/**
 * Route `/403` — `RequireAuth` đưa về đây khi tài khoản đã đăng nhập nhưng KHÔNG có quyền nào ở service tiếng Trung
 * (bị gỡ hết vai trò). Nút "Đăng xuất" chỉ hiện khi đang đăng nhập (hợp đồng §5.3.2; tạo sớm ở F2).
 */
export function ForbiddenPage() {
  const { status, logout } = useAuth()
  return <ErrorPage code={403} onLogout={status === 'authenticated' ? () => void logout() : undefined} />
}
