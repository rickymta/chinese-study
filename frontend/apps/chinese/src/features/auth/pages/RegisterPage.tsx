import { RegisterPage as SharedRegisterPage } from '@af/auth'
import { APP_BRAND } from '@/constants'

/** Route `/dang-ky` — vỏ mỏng của trang dùng chung `@af/auth`. */
export function RegisterPage() {
  return <SharedRegisterPage brand={APP_BRAND} afterLogin="/" loginPath="/dang-nhap" />
}
