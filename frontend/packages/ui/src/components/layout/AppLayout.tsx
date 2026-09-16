import { useMemo, useState, type ComponentType, type MouseEvent, type ReactNode } from 'react'
import { Link as RouterLink, Outlet, matchPath, useLocation, useNavigate } from 'react-router-dom'
import {
  AppBar,
  BottomNavigation,
  BottomNavigationAction,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Paper,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import type { ListItemButtonProps } from '@mui/material'
import DarkModeIcon from '@mui/icons-material/DarkMode'
import LightModeIcon from '@mui/icons-material/LightMode'
import MoreHorizIcon from '@mui/icons-material/MoreHoriz'
import { useThemeMode } from '../../theme/ThemeProvider'

export interface NavItem {
  label: string
  to: string
  icon: ReactNode
  /** Mã quyền cần có để hiện mục này (đọc từ `/api/me` của service ngôn ngữ — F3). Bỏ trống ⇒ luôn hiện. */
  requiredPermission?: string
  /** `true` ⇒ chỉ active khi khớp đúng path (mặc định cho `/`). */
  end?: boolean
}

export interface AppLayoutProps {
  /** Tên hiển thị ở đầu Drawer (md+) và AppBar (xs–sm), vd "AntFarm · Tiếng Trung". */
  title: string
  navItems: NavItem[]
  /** Khối menu người dùng (avatar/tên/Đăng xuất — F2). Đặt cuối Drawer hoặc bên phải AppBar. */
  userMenu?: ReactNode
  /**
   * Hàm kiểm quyền do app truyền (`useAuth().hasPermission` — F3). Mục có `requiredPermission` chỉ hiện khi hàm
   * trả `true`; không truyền hàm ⇒ mọi mục có yêu cầu quyền bị ẨN (fail-closed).
   */
  hasPermission?: (permission: string) => boolean
  /** Bề rộng Drawer ở md+. */
  drawerWidth?: number
}

const DRAWER_WIDTH = 240
const MOBILE_BAR_HEIGHT = 56
/** Bottom nav tối đa 5 mục (Material guideline); thừa gom vào "Thêm" (§5.3.0.5). */
const BOTTOM_NAV_MAX = 5

// ListItemButton + Link của react-router: ép kiểu một lần để không phải khai `component`/`to` thủ công từng chỗ.
type RouterListItemButtonProps = Omit<ListItemButtonProps, 'component'> & { component?: ComponentType<any>; to: string }
const RouterListItemButton = ListItemButton as ComponentType<RouterListItemButtonProps>

/**
 * Khung ứng dụng dùng chung (hợp đồng §5.3.0.1/§5.3.0.5):
 * - md trở lên: Drawer trái cố định (tiêu đề + menu + nút sáng/tối + menu người dùng).
 * - xs–sm (điện thoại ~375px): AppBar trên + BottomNavigation dưới (≤ 5 mục, thừa gom vào "Thêm").
 * Nội dung trang render qua `<Outlet />` (dùng làm layout route).
 */
export function AppLayout({ title, navItems, userMenu, hasPermission, drawerWidth = DRAWER_WIDTH }: AppLayoutProps) {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true }) // noSsr: tránh lần render đầu luôn ra false làm giao diện mobile nháy trên desktop
  const { mode, toggleMode } = useThemeMode()
  const location = useLocation()
  const navigate = useNavigate()

  const visibleItems = useMemo(
    () => navItems.filter((it) => !it.requiredPermission || hasPermission?.(it.requiredPermission) === true),
    [navItems, hasPermission],
  )

  const isActive = (item: NavItem) =>
    !!matchPath({ path: item.to, end: item.end ?? item.to === '/' }, location.pathname)

  const themeToggle = (
    <Tooltip title={mode === 'dark' ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'}>
      <IconButton onClick={toggleMode} aria-label="Đổi chế độ sáng/tối" color="inherit">
        {mode === 'dark' ? <LightModeIcon /> : <DarkModeIcon />}
      </IconButton>
    </Tooltip>
  )

  // ── Bottom nav (mobile): mục hiển thị + phần dồn vào "Thêm" ──
  const overflow = visibleItems.length > BOTTOM_NAV_MAX
  const bottomItems = overflow ? visibleItems.slice(0, BOTTOM_NAV_MAX - 1) : visibleItems
  const moreItems = overflow ? visibleItems.slice(BOTTOM_NAV_MAX - 1) : []
  const [moreAnchor, setMoreAnchor] = useState<HTMLElement | null>(null)
  const activeBottomValue =
    bottomItems.find(isActive)?.to ?? (moreItems.some(isActive) ? '__more__' : false)

  const openMore = (e: MouseEvent<HTMLElement>) => setMoreAnchor(e.currentTarget)
  const closeMore = () => setMoreAnchor(null)

  if (isDesktop) {
    return (
      <Box sx={{ display: 'flex', minHeight: '100dvh' }}>
        <Drawer
          variant="permanent"
          sx={{
            width: drawerWidth,
            flexShrink: 0,
            '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', borderRight: 1, borderColor: 'divider' },
          }}
        >
          <Toolbar sx={{ px: 2 }}>
            <Typography variant="h6" noWrap sx={{ fontWeight: 700, color: 'primary.main' }}>
              {title}
            </Typography>
          </Toolbar>
          <Divider />
          <List component="nav" sx={{ flex: 1, px: 1, py: 1 }}>
            {visibleItems.map((item) => (
              <RouterListItemButton
                key={item.to}
                component={RouterLink}
                to={item.to}
                selected={isActive(item)}
                sx={{ borderRadius: 2, mb: 0.5 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} slotProps={{ primary: { sx: { fontWeight: 500 } } }} />
              </RouterListItemButton>
            ))}
          </List>
          <Divider />
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, p: 1.5 }}>
            {themeToggle}
            {userMenu}
          </Box>
        </Drawer>
        <Box component="main" sx={{ flex: 1, minWidth: 0, p: 3 }}>
          <Outlet />
        </Box>
      </Box>
    )
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100dvh' }}>
      <AppBar position="sticky" color="default" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar sx={{ minHeight: MOBILE_BAR_HEIGHT, gap: 1 }}>
          <Typography variant="h6" noWrap sx={{ flex: 1, fontWeight: 700, color: 'primary.main' }}>
            {title}
          </Typography>
          {themeToggle}
          {userMenu}
        </Toolbar>
      </AppBar>

      <Box
        component="main"
        sx={{
          flex: 1,
          minWidth: 0,
          p: 2,
          // Chừa chỗ cho bottom nav + vùng an toàn của iPhone.
          pb: `calc(${MOBILE_BAR_HEIGHT}px + 16px + env(safe-area-inset-bottom))`,
        }}
      >
        <Outlet />
      </Box>

      {visibleItems.length > 0 && (
        <Paper
          elevation={3}
          square
          sx={{
            position: 'fixed',
            left: 0,
            right: 0,
            bottom: 0,
            zIndex: theme.zIndex.appBar,
            pb: 'env(safe-area-inset-bottom)',
            borderTop: 1,
            borderColor: 'divider',
          }}
        >
          <BottomNavigation
            showLabels
            value={activeBottomValue}
            onChange={(_e, value: string) => {
              if (value !== '__more__') navigate(value)
            }}
          >
            {bottomItems.map((item) => (
              <BottomNavigationAction key={item.to} label={item.label} value={item.to} icon={item.icon} />
            ))}
            {overflow && (
              <BottomNavigationAction label="Thêm" value="__more__" icon={<MoreHorizIcon />} onClick={openMore} />
            )}
          </BottomNavigation>
          <Menu
            anchorEl={moreAnchor}
            open={!!moreAnchor}
            onClose={closeMore}
            anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
            transformOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          >
            {moreItems.map((item) => (
              <MenuItem
                key={item.to}
                selected={isActive(item)}
                onClick={() => {
                  closeMore()
                  navigate(item.to)
                }}
              >
                <ListItemIcon>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </MenuItem>
            ))}
          </Menu>
        </Paper>
      )}
    </Box>
  )
}
