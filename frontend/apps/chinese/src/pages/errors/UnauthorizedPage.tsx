import { useNavigate } from 'react-router-dom'
import { ErrorPage } from '@af/ui'

/** Route `/401` — nút "Đăng nhập" đưa về `/dang-nhap` (hợp đồng §5.3.2; tạo sớm ở F2 vì `RequireAuth` đã tồn tại). */
export function UnauthorizedPage() {
  const navigate = useNavigate()
  return <ErrorPage code={401} onLogin={() => navigate('/dang-nhap', { replace: true })} />
}
