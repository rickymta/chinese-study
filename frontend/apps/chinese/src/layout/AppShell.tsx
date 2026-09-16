import { AppLayout, type NavItem } from '@af/ui'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'

// F1 chỉ có Trang chủ; các feature sau bổ sung: /pinyin (F5), /tu-dien (F6), /on-tap (F7), /luyen-viet (F8),
// /bai-hoc (F9), /quan-tri/... (F10, requiredPermission: 'content.manage' | 'users.manage').
const NAV_ITEMS: NavItem[] = [{ label: 'Trang chủ', to: '/', icon: <HomeOutlinedIcon />, end: true }]

/** Khung app tiếng Trung — bọc `AppLayout` dùng chung; F2 truyền `userMenu`, F3 truyền `hasPermission`. */
export function AppShell() {
  return <AppLayout title="AntFarm · Tiếng Trung" navItems={NAV_ITEMS} />
}
