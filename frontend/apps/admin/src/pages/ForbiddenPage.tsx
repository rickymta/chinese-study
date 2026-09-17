import { useEffect, useState } from 'react'
import { Button } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@af/auth'
import { ErrorPage } from '@af/ui'

/**
 * Route `/403` (R-W2, hợp đồng W2 §5.3.1) — hai tình huống, lời giải thích khác nhau để người dùng không tưởng app hỏng:
 * 1. Đã đăng nhập nhưng KHÔNG có quyền quản trị nào (cms-backend fail-closed: tài khoản mới 0 vai trò; hoặc chỉ là
 *    học viên ở service ngôn ngữ): `RequireAuth` đưa về đây; có nút "Kiểm tra lại quyền" + "Đăng xuất".
 * 2. Có quyền nhưng thiếu quyền của trang/API vừa gọi (`RequirePermission` hoặc GET 403 qua `createApiClient`).
 * Trang nằm NGOÀI `RequireAuth` nên phải tự xử lý `status`.
 */
export function ForbiddenPage() {
  const { status, account, permissions, meLoading, logout, reloadMe } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [checked, setChecked] = useState(false)

  const authenticated = status === 'authenticated'
  const noRole = authenticated && permissions.size === 0

  // Sau khi bấm "Kiểm tra lại quyền" mà đã có quyền ⇒ tự về Tổng quan.
  useEffect(() => {
    if (checked && !meLoading && permissions.size > 0) navigate('/', { replace: true })
  }, [checked, meLoading, permissions, navigate])

  // Trang nằm NGOÀI `RequireAuth` ⇒ đăng xuất xong không tự rời trang — chủ động xoá dữ liệu rồi về /dang-nhap.
  const handleLogout = async () => {
    await logout()
    queryClient.clear()
    navigate('/dang-nhap', { replace: true })
  }

  const recheck = async () => {
    await reloadMe()
    setChecked(true)
  }

  const who = account?.email ? `Tài khoản ${account.email}` : 'Tài khoản của bạn'
  const description = noRole
    ? `${who} đã đăng nhập nhưng chưa được gán vai trò quản trị nào (tài khoản mới không có quyền gì ở trang Admin cho tới khi quản trị viên gán vai trò). Liên hệ quản trị viên, sau đó bấm "Kiểm tra lại quyền".`
    : authenticated
      ? `${who} không có quyền vào trang này hoặc thực hiện thao tác vừa rồi. Nếu bạn vừa được cấp quyền, hãy tải lại trang.`
      : undefined

  return (
    <ErrorPage
      code={403}
      title={noRole ? 'Chưa được cấp vai trò quản trị' : undefined}
      description={description}
      onLogout={authenticated ? () => void handleLogout() : undefined}
    >
      {noRole && (
        <Button variant="outlined" onClick={() => void recheck()} disabled={meLoading}>
          {meLoading ? 'Đang kiểm tra…' : 'Kiểm tra lại quyền'}
        </Button>
      )}
    </ErrorPage>
  )
}
