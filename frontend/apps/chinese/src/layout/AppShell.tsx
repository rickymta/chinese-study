import { useMemo } from 'react'
import { matchPath, useLocation } from 'react-router-dom'
import { Badge } from '@mui/material'
import { AppLayout, type NavItem } from '@af/ui'
import { useAuth } from '@af/auth'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined'
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined'
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import StyleOutlinedIcon from '@mui/icons-material/StyleOutlined'
import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined'
import DrawOutlinedIcon from '@mui/icons-material/DrawOutlined'
import EditNoteOutlinedIcon from '@mui/icons-material/EditNoteOutlined'
import { APP_BRAND } from '@/constants'
import { UserMenu } from '@/features/auth/components/UserMenu'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useSrsSummary } from '@/features/srs/hooks'

// Thứ tự menu theo hợp đồng §5.3 (chốt ở F8): Trang chủ · Ôn tập · Bài học · Luyện viết · Pinyin · Từ điển — việc
// hằng ngày (ôn, học bài, viết) lên trước, tra cứu (Pinyin, Từ điển) sau. Mục quản trị luôn ở cuối. Ở điện thoại
// học viên có 6 mục ⇒ bottom nav hiện 4 mục đầu + "Thêm" (AppLayout gom phần thừa). F4 thêm /quan-tri/nguoi-dung,
// F10 thêm /quan-tri/bai-hoc, /quan-tri/tu-vung (requiredPermission: 'content.manage').
const buildNavItems = (dueBadge: number): NavItem[] => [
  { label: 'Trang chủ', to: '/', icon: <HomeOutlinedIcon />, end: true },
  // F7: ôn tập — huy hiệu = thẻ đến hạn lúc này + từ mới còn học được (tối đa "99+"), từ `GET /api/srs/summary`.
  {
    label: 'Ôn tập',
    to: '/on-tap',
    icon: (
      <Badge badgeContent={dueBadge} max={99} color="error" overlap="rectangular">
        <StyleOutlinedIcon />
      </Badge>
    ),
    requiredPermission: PERMISSIONS.STUDY_USE,
  },
  // F9: bài học chủ đề — không `end` để /bai-hoc/:slug vẫn sáng mục này.
  { label: 'Bài học', to: '/bai-hoc', icon: <AutoStoriesOutlinedIcon />, requiredPermission: PERMISSIONS.STUDY_USE },
  // F8: luyện viết — không `end` để /luyen-viet/:hanzi vẫn sáng mục này.
  { label: 'Luyện viết', to: '/luyen-viet', icon: <DrawOutlinedIcon />, requiredPermission: PERMISSIONS.STUDY_USE },
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
  // F10: quản trị nội dung (bài học + từ vựng, cùng tab liên kết) — quyền `content.manage` độc lập với `users.manage`
  // nên KHÔNG ẩn ở mobile: người chỉ có quyền soạn nội dung vẫn cần lối vào (bottom nav gom vào "Thêm").
  {
    label: 'Quản trị nội dung',
    to: '/quan-tri/bai-hoc',
    icon: <EditNoteOutlinedIcon />,
    requiredPermission: PERMISSIONS.CONTENT_MANAGE,
  },
]

/**
 * Khung app tiếng Trung — bọc `AppLayout` dùng chung; menu người dùng (F2) + lọc mục theo quyền từ `/api/me` (F3).
 * F7: huy hiệu số thẻ trên mục "Ôn tập" (chỉ hỏi server khi có `study.use`), ẩn bottom nav trong phiên ôn.
 */
export function AppShell() {
  const { hasPermission } = useAuth()
  const location = useLocation()
  const summary = useSrsSummary(hasPermission(PERMISSIONS.STUDY_USE))
  const dueBadge = summary.data ? summary.data.dueNow + summary.data.newAvailableToday : 0
  const navItems = useMemo(() => buildNavItems(dueBadge), [dueBadge])
  const hideBottomNav = !!matchPath('/on-tap/phien', location.pathname)
  return (
    <AppLayout
      title={APP_BRAND}
      navItems={navItems}
      userMenu={<UserMenu />}
      hasPermission={hasPermission}
      hideBottomNav={hideBottomNav}
    />
  )
}
