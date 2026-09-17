import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined'
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined'
import type { NavItem } from '@af/ui'
import { CMS_PERMS, perm } from '@/auth/permissions'
import { LANGUAGE_MODULES } from '@/modules/registry'

/**
 * Nhóm điều hướng của admin (hợp đồng W2 §5.3.1 "Nav"): "Website" (W3–W6), "Hộp thư" (W9), "Tài khoản nền tảng"
 * (W11), "Ngôn ngữ › <tên>" (W13), "Hệ thống › Người dùng CMS, Nhật ký". W2 chỉ có Tổng quan + Người dùng CMS;
 * các nhóm còn lại khai sẵn KHÔNG có mục để Dashboard nói rõ "bạn có quyền X nhưng màn chưa làm" / "bạn chưa có
 * quyền X — liên hệ quản trị viên" (quy tắc nút ẩn phải kèm lời giải thích).
 */
export interface NavGroupDef {
  key: string
  label: string
  /** Có BẤT KỲ quyền nào trong danh sách ⇒ người dùng "thuộc" nhóm này. Rỗng ⇒ ai đăng nhập cũng thấy. */
  permissions: readonly string[]
  /** Mục menu đã có màn. `AppLayout` tự ẩn mục theo `requiredPermission`. */
  items: NavItem[]
  /** Màn dự kiến của nhóm (hiện trên Dashboard khi có quyền mà `items` rỗng). */
  planned?: string
}

export function buildNavGroups(perms: ReadonlySet<string>): NavGroupDef[] {
  return [
    {
      key: 'tong-quan',
      label: 'Tổng quan',
      permissions: [],
      items: [{ label: 'Tổng quan', to: '/', icon: <DashboardOutlinedIcon />, end: true }],
    },
    {
      key: 'website',
      label: 'Website',
      permissions: [CMS_PERMS.SITE_MANAGE, CMS_PERMS.POSTS_MANAGE, CMS_PERMS.MEDIA_MANAGE],
      items: [],
      planned: 'Cấu hình site/SEO, ngôn ngữ, FAQ, trang tĩnh, banner, bài viết, thư viện ảnh (đợt W3–W6).',
    },
    {
      key: 'hop-thu',
      label: 'Hộp thư',
      permissions: [CMS_PERMS.INBOX_MANAGE],
      items: [],
      planned: 'Liên hệ và đăng ký nhận tin từ website, xuất CSV (đợt W9).',
    },
    {
      key: 'tai-khoan',
      label: 'Tài khoản nền tảng',
      permissions: [CMS_PERMS.ACCOUNTS_MANAGE],
      items: [],
      planned: 'Khoá/mở khoá tài khoản, đặt lại mật khẩu, thu hồi phiên, công tắc đăng ký, thống kê (đợt W11).',
    },
    ...LANGUAGE_MODULES.map<NavGroupDef>((m) => ({
      key: `ngon-ngu-${m.code}`,
      label: `Ngôn ngữ › ${m.label}`,
      permissions: m.adminPermissions.map((p) => perm(m.code, p)),
      items: m.nav(perms),
      planned: `Bài học, duyệt nghĩa từ vựng, vai trò của ${m.label} (đợt W13) — tạm thời vẫn dùng trang quản trị trong app ${m.label}.`,
    })),
    {
      key: 'he-thong',
      label: 'Hệ thống',
      permissions: [CMS_PERMS.USERS_MANAGE],
      items: [
        {
          label: 'Người dùng CMS',
          to: '/nguoi-dung-cms',
          icon: <ManageAccountsOutlinedIcon />,
          requiredPermission: CMS_PERMS.USERS_MANAGE,
        },
      ],
      planned: 'Nhật ký thao tác (đợt W3).',
    },
  ]
}

/** Danh sách phẳng cho `AppLayout` (tự lọc theo `requiredPermission`). */
export const buildNavItems = (perms: ReadonlySet<string>): NavItem[] => buildNavGroups(perms).flatMap((g) => g.items)

/** Người dùng có "thuộc" nhóm không (rỗng ⇒ luôn có). */
export const canAccessGroup = (group: NavGroupDef, perms: ReadonlySet<string>): boolean =>
  group.permissions.length === 0 || group.permissions.some((p) => perms.has(p))
