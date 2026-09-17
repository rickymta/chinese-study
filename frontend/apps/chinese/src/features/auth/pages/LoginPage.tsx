import { LoginPage as SharedLoginPage } from '@af/auth'
import { APP_BRAND } from '@/constants'

/** Route `/dang-nhap` — vỏ mỏng của trang dùng chung `@af/auth`, chỉ truyền tên app. */
export function LoginPage() {
  return <SharedLoginPage brand={APP_BRAND} afterLogin="/" registerPath="/dang-ky" />
}
