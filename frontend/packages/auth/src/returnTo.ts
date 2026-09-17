/**
 * `returnTo` chỉ được là đường dẫn NỘI BỘ (`/on-tap?x=1`) — chặn open redirect kiểu `//evil.com`, `http://...`,
 * `/\evil.com`. Không hợp lệ ⇒ `null` để bên gọi dùng đích mặc định.
 */
export function sanitizeReturnTo(value: string | null | undefined): string | null {
  if (!value) return null
  if (!value.startsWith('/')) return null
  if (value.startsWith('//') || value.startsWith('/\\')) return null
  return value
}

/** Dựng `/dang-nhap?returnTo=<đường dẫn hiện tại>&reason=expired`. Không đưa chính trang đăng nhập vào `returnTo`. */
export function buildLoginUrl(
  loginPath: string,
  current: { pathname: string; search: string },
  reason: 'expired' | null,
): string {
  const params = new URLSearchParams()
  const target = current.pathname + current.search
  if (target !== '/' && !target.startsWith(loginPath)) params.set('returnTo', target)
  if (reason) params.set('reason', reason)
  const qs = params.toString()
  return qs ? `${loginPath}?${qs}` : loginPath
}
