import type { ReactNode } from 'react'
import type { RouteObject } from 'react-router-dom'
import type { NavItem } from '@af/ui'

/**
 * Danh bạ module NGÔN NGỮ của admin (hợp đồng W2 §5.3.2). Mỗi service ngôn ngữ có API quản trị riêng, quyền kiểm
 * CỤC BỘ ở chính service đó (R-W5) — admin chỉ gọi `GET /<code>/api/me` để biết người này được làm gì ở đó.
 *
 * Thêm ngôn ngữ = thêm một mục ở đây + thư mục `src/modules/<code>/` + dòng proxy trong `vite.config.ts`
 * + location nginx `/<code>/` trên `admin.antfarms.xyz`. File này KHÔNG import client axios (tránh vòng
 * `clients.ts` ↔ `registry.ts`) — `api/clients.ts` dựng `languageApis` từ `apiBase` ở đây.
 */
export interface LanguageModule {
  /** Mã ngôn ngữ = tiền tố route gateway (`chinese`) = tiền tố quyền trong admin (`chinese:content.manage`). */
  code: string
  /** Tên hiển thị tiếng Việt. */
  label: string
  /** Gốc API cùng origin, vd `/chinese/api`. */
  apiBase: string
  /**
   * Chỉ những quyền này của service ngôn ngữ mới được coi là QUYỀN ADMIN (tiêu chí W2 #2): `study.use` của học viên
   * không tính — tài khoản thường (chỉ `learner` ở tiếng Trung) mở admin vẫn về `/403`.
   */
  adminPermissions: readonly string[]
  /** Mục menu của module theo quyền đã gộp (có tiền tố). W2: rỗng; W13 thêm màn. */
  nav: (perms: ReadonlySet<string>) => NavItem[]
  /** Route con dưới `/ngon-ngu/<code>/`. W2: rỗng; W13 thêm màn. */
  routes: RouteObject[]
  icon?: ReactNode
}

/**
 * Quyền của service ngôn ngữ được tính là quyền quản trị (hằng dùng chung cho mọi module; module có thể ghi đè
 * qua `adminPermissions`). Hợp đồng W2 §5.3.1 tiêu chí #2.
 */
export const ADMIN_RELEVANT_PERMISSIONS = ['users.manage', 'content.manage'] as const

export const LANGUAGE_MODULES: readonly LanguageModule[] = [
  {
    code: 'chinese',
    label: 'Tiếng Trung',
    apiBase: '/chinese/api',
    adminPermissions: ADMIN_RELEVANT_PERMISSIONS,
    // W13 mới có màn (bài học, duyệt nghĩa, vai trò) — W2 chỉ khai để Dashboard báo trạng thái + gộp quyền.
    nav: () => [],
    routes: [],
  },
]

export const findLanguageModule = (code: string): LanguageModule | undefined =>
  LANGUAGE_MODULES.find((m) => m.code === code)
