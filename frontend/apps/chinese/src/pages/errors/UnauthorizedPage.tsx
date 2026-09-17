import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@af/auth'
import { ErrorPage } from '@af/ui'

/**
 * Route `/401` (hợp đồng §5.3.2) — nút "Đăng nhập lại" về `/dang-nhap`. Luồng thường (`@af/auth` có `onAuthLost`)
 * KHÔNG đi qua đây mà `RequireAuth` đưa thẳng tới `/dang-nhap?returnTo=...&reason=expired`; trang này dành cho
 * điều hướng trực tiếp hoặc client không quản lý phiên. Đang có phiên (token còn hạn mà service vẫn 401, vd sai
 * audience) ⇒ đăng xuất trước để đăng nhập lại thật sự chứ không quay vòng.
 */
export function UnauthorizedPage() {
  const navigate = useNavigate()
  const { status, logout } = useAuth()
  const [busy, setBusy] = useState(false)

  const handleLogin = async () => {
    setBusy(true)
    try {
      if (status === 'authenticated') await logout()
    } finally {
      setBusy(false)
      navigate('/dang-nhap', { replace: true })
    }
  }

  return (
    <ErrorPage
      code={401}
      loginLabel={busy ? 'Đang chuyển…' : 'Đăng nhập lại'}
      onLogin={() => {
        if (!busy) void handleLogin()
      }}
    />
  )
}
