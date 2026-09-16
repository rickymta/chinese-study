import { AppLayout, type NavItem } from '@af/ui'
import { useAuth } from '@af/auth'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined'
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined'
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import { APP_BRAND } from '@/constants'
import { UserMenu } from '@/features/auth/components/UserMenu'
import { PERMISSIONS } from '@/features/auth/permissions'

// Các feature sau chèn GIỮA Trang chủ và Quản trị: /pinyin (F5), /tu-dien (F6), /on-tap (F7), /luyen-viet (F8),
// /bai-hoc (F9). Mục quản trị luôn ở cuối (mobile: rơi vào "Thêm" khi quá 5 mục). F4 thêm /quan-tri/nguoi-dung,
// F10 thêm /quan-tri/bai-hoc, /quan-tri/tu-vung (requiredPermission: 'content.manage').
const NAV_ITEMS: NavItem[] = [
  { label: 'Trang chủ', to: '/', icon: <HomeOutlinedIcon />, end: true },
  { label: 'Pinyin', to: '/pinyin', icon: <RecordVoiceOverOutlinedIcon />, requiredPermission: PERMISSIONS.STUDY_USE },
  // F6: từ điển — mục không `end` để /tu-dien/:id và /tu-dien/chu/:hanzi vẫn sáng mục này.
  { label: 'Từ điển', to: '/tu-dien', icon: <MenuBookOutlinedIcon />, requiredPermission: PERMISSIONS.STUDY_USE },
  // Ẩn với người không có `users.manage` — `AppLayout` lọc theo `hasPermission` (quyền từ GET /chinese/api/me).
  { label: 'Quản trị', to: '/quan-tri', icon: <AdminPanelSettingsOutlinedIcon />, requiredPermission: PERMISSIONS.USERS_MANAGE },
  // F4: trang con của Quản trị — ở điện thoại KHÔNG chiếm thêm ô trên bottom nav (vào qua thẻ trong /quan-tri);
  // Drawer md+ vẫn hiện thành mục riêng. Mục "Quản trị" (không `end`) vẫn sáng khi đang ở trang này.
  {
    label: 'Người dùng',
    to: '/quan-tri/nguoi-dung',
    icon: <ManageAccountsOutlinedIcon />,
    requiredPermission: PERMISSIONS.USERS_MANAGE,
    hideOnMobile: true,
  },
]

/** Khung app tiếng Trung — bọc `AppLayout` dùng chung; menu người dùng (F2) + lọc mục theo quyền từ `/api/me` (F3). */
export function AppShell() {
  const { hasPermission } = useAuth()
  return <AppLayout title={APP_BRAND} navItems={NAV_ITEMS} userMenu={<UserMenu />} hasPermission={hasPermission} />
}
