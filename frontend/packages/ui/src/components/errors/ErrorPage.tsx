import type { ReactNode } from 'react'
import { Box, Button, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import LockClockOutlinedIcon from '@mui/icons-material/LockClockOutlined'
import GppBadOutlinedIcon from '@mui/icons-material/GppBadOutlined'
import SearchOffOutlinedIcon from '@mui/icons-material/SearchOffOutlined'
import ReportGmailerrorredOutlinedIcon from '@mui/icons-material/ReportGmailerrorredOutlined'

/**
 * Trang lỗi 4xx/5xx DÙNG CHUNG (quy tắc CLAUDE.md "Trang lỗi 4xx thống nhất"): mọi trường hợp đều có nút
 * "Về trang chủ". `NotFoundPage` (F1) và các vỏ `/401`, `/403` (F3) chỉ là lớp mỏng gọi component này.
 *
 * Phải nằm trong Router (dùng `RouterLink`) — đúng cách dùng hiện tại: khai làm route element.
 */
export interface ErrorPageProps {
  /** Mã lỗi HTTP — quyết định icon + tiêu đề/mô tả mặc định. */
  code: number
  title?: string
  description?: string
  /** Đường dẫn nút "Về trang chủ" — luôn hiển thị. */
  homeHref?: string
  /** Có ⇒ hiện nút "Đăng nhập" (F3: trang /401). */
  onLogin?: () => void
  /** Có ⇒ hiện nút "Đăng xuất" (F3: trang /403 cho tài khoản 0 quyền). */
  onLogout?: () => void
  /** Có ⇒ hiện nút "Quay lại". */
  onBack?: () => void
  /** Nội dung bổ sung dưới mô tả (vd chi tiết kỹ thuật). */
  children?: ReactNode
}

interface ErrorDefaults {
  title: string
  description: string
  icon: ReactNode
}

const ICON_SX = { fontSize: 56, mt: -1 } as const

function getErrorDefaults(code: number): ErrorDefaults {
  switch (code) {
    case 401:
      return {
        title: 'Cần đăng nhập',
        description: 'Phiên đăng nhập đã hết hạn hoặc bạn chưa đăng nhập. Vui lòng đăng nhập để tiếp tục.',
        icon: <LockClockOutlinedIcon sx={{ ...ICON_SX, color: 'warning.main' }} />,
      }
    case 403:
      return {
        title: 'Không có quyền truy cập',
        description:
          'Tài khoản của bạn chưa được cấp quyền cho chức năng này. Liên hệ quản trị viên để được cấp quyền.',
        icon: <GppBadOutlinedIcon sx={{ ...ICON_SX, color: 'error.main' }} />,
      }
    case 404:
      return {
        title: 'Trang không tồn tại',
        description: 'Địa chỉ bạn truy cập không hợp lệ hoặc đã bị xoá.',
        icon: <SearchOffOutlinedIcon sx={{ ...ICON_SX, color: 'text.disabled' }} />,
      }
    default:
      if (code >= 500) {
        return {
          title: 'Hệ thống gặp sự cố',
          description: 'Đã xảy ra lỗi phía máy chủ. Vui lòng thử lại sau.',
          icon: <ReportGmailerrorredOutlinedIcon sx={{ ...ICON_SX, color: 'error.main' }} />,
        }
      }
      return {
        title: 'Đã xảy ra lỗi',
        description: 'Vui lòng thử lại hoặc quay về trang chủ.',
        icon: <ErrorOutlineOutlinedIcon sx={{ ...ICON_SX, color: 'text.disabled' }} />,
      }
  }
}

export function ErrorPage({
  code,
  title,
  description,
  homeHref = '/',
  onLogin,
  onLogout,
  onBack,
  children,
}: ErrorPageProps) {
  const defaults = getErrorDefaults(code)

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100dvh',
        bgcolor: 'background.default',
        gap: 2,
        p: 3,
        textAlign: 'center',
      }}
    >
      <Typography
        component="p"
        variant="h1"
        sx={{ fontWeight: 700, fontSize: { xs: '5rem', sm: '7rem' }, lineHeight: 1, color: 'text.disabled' }}
      >
        {code}
      </Typography>
      {defaults.icon}
      <Typography component="h1" variant="h5" sx={{ fontWeight: 600 }}>
        {title ?? defaults.title}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 420 }}>
        {description ?? defaults.description}
      </Typography>
      {children}
      <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap', justifyContent: 'center' }}>
        {onBack && (
          <Button variant="outlined" onClick={onBack}>
            Quay lại
          </Button>
        )}
        {onLogin && (
          <Button variant="contained" onClick={onLogin}>
            Đăng nhập
          </Button>
        )}
        {onLogout && (
          <Button variant="outlined" color="error" onClick={onLogout}>
            Đăng xuất
          </Button>
        )}
        <Button variant="contained" component={RouterLink} to={homeHref}>
          Về trang chủ
        </Button>
      </Box>
    </Box>
  )
}
