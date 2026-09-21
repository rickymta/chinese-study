import { LoginPage as SharedLoginPage } from '@af/auth'
import { APP_BRAND } from '@/constants'

/**
 * Route `/dang-nhap` — vỏ mỏng của trang dùng chung `@af/auth`. `registerPath={null}`: admin KHÔNG cho đăng ký tại
 * đây (hợp đồng W2 §5.3.1) — tài khoản nền tảng tạo ở app học; vào admin cần được gán vai trò CMS.
 */
export function LoginPage() {
  return <SharedLoginPage brand={APP_BRAND} afterLogin="/" registerPath={null} />
}
