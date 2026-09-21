// Kiểu dữ liệu màn Người dùng CMS (hợp đồng W2 §6.1) — khớp DTO thật của cms-backend
// (`AntFarm.Cms.Application.Access.Dtos.AdminUserDto`, `RoleDto`).

/** Một người dùng đã từng mở trang Admin (`access.users` của cms-backend). */
export interface CmsUser {
  /** = `sub` của JWT (id tài khoản identity). */
  id: string
  email: string
  displayName: string
  /** Mã vai trò CMS (`admin`, `editor`, `support`), có thể rỗng (fail-closed R-W2). */
  roles: string[]
  /** Email thuộc `CmsAdmin:BootstrapEmails` — khởi động lại dịch vụ sẽ gán lại `admin`. */
  isBootstrapAdmin: boolean
  firstSeenAt: string
  lastSeenAt: string
}

/** `GET /api/admin/users?q=&page=&pageSize=` → 200. */
export interface CmsUsersPage {
  items: CmsUser[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CmsUsersQuery {
  q?: string
  page?: number
  pageSize?: number
}

/** `GET /api/admin/roles` → 200 (sắp `admin`, `editor`, `support`). KHÔNG có `description` (khác chinese-backend). */
export interface CmsRole {
  code: string
  name: string
  permissions: string[]
}

/** `PUT /api/admin/users/{id}/roles` — thay TOÀN BỘ tập vai trò; `[]` hợp lệ (gỡ hết). */
export interface SetCmsUserRolesRequest {
  roles: string[]
}
