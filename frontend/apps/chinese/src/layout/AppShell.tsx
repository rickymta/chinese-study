import { AppLayout, type NavItem } from '@af/ui'
import { useAuth } from '@af/auth'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'
import { APP_BRAND } from '@/constants'
import { UserMenu } from '@/features/auth/components/UserMenu'

// F1 chỉ có Trang chủ; các feature sau bổ sung: /pinyin (F5), /tu-dien (F6), /on-tap (F7), /luyen-viet (F8),
// /bai-hoc (F9), /quan-tri/... (F10, requiredPermission: 'content.manage' | 'users.manage').
const NAV_ITEMS: NavItem[] = [{ label: 'Trang chủ', to: '/', icon: <HomeOutlinedIcon />, end: true }]

/** Khung app tiếng Trung — bọc `AppLayout` dùng chung; menu người dùng (F2) + lọc mục theo quyền từ `/api/me` (F3). */
export function AppShell() {
  const { hasPermission } = useAuth()
  return <AppLayout title={APP_BRAND} navItems={NAV_ITEMS} userMenu={<UserMenu />} hasPermission={hasPermission} />
}
