import { AppLayout, type NavItem } from '@af/ui'
import { useAuth } from '@af/auth'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined'
import { APP_BRAND } from '@/constants'
import { UserMenu } from '@/features/auth/components/UserMenu'
import { PERMISSIONS } from '@/features/auth/permissions'

// Các feature sau chèn GIỮA Trang chủ và Quản trị: /pinyin (F5), /tu-dien (F6), /on-tap (F7), /luyen-viet (F8),
// /bai-hoc (F9). Mục quản trị luôn ở cuối (mobile: rơi vào "Thêm" khi quá 5 mục). F4 thêm /quan-tri/nguoi-dung,
// F10 thêm /quan-tri/bai-hoc, /quan-tri/tu-vung (requiredPermission: 'content.manage').
const NAV_ITEMS: NavItem[] = [
  { label: 'Trang chủ', to: '/', icon: <HomeOutlinedIcon />, end: true },
  // Ẩn với người không có `users.manage` — `AppLayout` lọc theo `hasPermission` (quyền từ GET /chinese/api/me).
  { label: 'Quản trị', to: '/quan-tri', icon: <AdminPanelSettingsOutlinedIcon />, requiredPermission: PERMISSIONS.USERS_MANAGE },
]

/** Khung app tiếng Trung — bọc `AppLayout` dùng chung; menu người dùng (F2) + lọc mục theo quyền từ `/api/me` (F3). */
export function AppShell() {
  const { hasPermission } = useAuth()
  return <AppLayout title={APP_BRAND} navItems={NAV_ITEMS} userMenu={<UserMenu />} hasPermission={hasPermission} />
}
