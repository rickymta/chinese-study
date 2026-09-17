import { useState, type MouseEvent } from 'react'
import { Avatar, Box, ButtonBase, Divider, ListItemIcon, ListItemText, Menu, MenuItem, Typography } from '@mui/material'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@af/auth'

/** Chữ cái đầu của tên hiển thị (hoặc email) làm avatar — không tải ảnh. */
function initialOf(name: string, email: string): string {
  const source = name.trim() || email.trim()
  return source ? source[0]!.toUpperCase() : '?'
}

/**
 * Menu người dùng của admin: avatar + tên, bấm mở menu có email và "Đăng xuất". KHÔNG có "Hồ sơ" — đổi tên/múi
 * giờ/mật khẩu làm ở app học (R-W17: admin không tự thao tác tài khoản của mình qua trang này).
 * Đặt vào `userMenu` của `AppLayout`: cuối Drawer (md+) hoặc góc phải AppBar (xs–sm, chỉ hiện avatar cho gọn 375px).
 */
export function UserMenu() {
  const { account, logout } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const [busy, setBusy] = useState(false)

  if (!account) return null

  const open = (e: MouseEvent<HTMLElement>) => setAnchor(e.currentTarget)
  const close = () => setAnchor(null)

  const handleLogout = async () => {
    setBusy(true)
    try {
      await logout()
      // Dữ liệu đã tải thuộc tài khoản cũ — xoá để tài khoản đăng nhập sau không thấy nhầm.
      queryClient.clear()
      navigate('/dang-nhap', { replace: true })
    } finally {
      setBusy(false)
      close()
    }
  }

  const name = account.displayName || account.email

  return (
    <>
      <ButtonBase
        onClick={open}
        aria-label={`Tài khoản: ${name}`}
        aria-haspopup="menu"
        aria-expanded={anchor ? 'true' : undefined}
        sx={{ borderRadius: 5, p: 0.5, pr: { xs: 0.5, md: 1.5 }, gap: 1, minWidth: 0, maxWidth: '100%' }}
      >
        <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main', fontSize: 15, fontWeight: 700 }}>
          {initialOf(account.displayName, account.email)}
        </Avatar>
        <Typography variant="body2" noWrap sx={{ display: { xs: 'none', md: 'block' }, fontWeight: 600, maxWidth: 120 }}>
          {name}
        </Typography>
      </ButtonBase>
      <Menu
        anchorEl={anchor}
        open={!!anchor}
        onClose={close}
        anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
        transformOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        slotProps={{ paper: { sx: { minWidth: 220 } } }}
      >
        <Box sx={{ px: 2, py: 1 }}>
          <Typography variant="subtitle2" noWrap sx={{ fontWeight: 700 }}>
            {name}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {account.email}
          </Typography>
        </Box>
        <Divider />
        <MenuItem onClick={() => void handleLogout()} disabled={busy}>
          <ListItemIcon>
            <LogoutOutlinedIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText primary={busy ? 'Đang đăng xuất…' : 'Đăng xuất'} />
        </MenuItem>
      </Menu>
    </>
  )
}
