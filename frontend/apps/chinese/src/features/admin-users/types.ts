// Kiểu dữ liệu quản trị người dùng (F4, hợp đồng §6.3 — backend làm song song, bám shape hợp đồng).

/** Một người dùng đã từng vào service tiếng Trung (`access.users`). */
export interface AdminUser {
  /** = `sub` của JWT (id tài khoản identity). */
  id: string
  email: string
  displayName: string
  /** Mã vai trò cục bộ (`admin`, `learner`), có thể rỗng. */
  roles: string[]
  /** Email thuộc `ChineseAdmin:BootstrapEmails` — khởi động lại dịch vụ sẽ gán lại `admin` (R4-10). */
  isBootstrapAdmin: boolean
  firstSeenAt: string
  lastSeenAt: string
}

/** `GET /api/admin/users?q=&page=&pageSize=` → 200. */
export interface AdminUsersPage {
  items: AdminUser[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AdminUsersQuery {
  q?: string
  page?: number
  pageSize?: number
}

/** `GET /api/admin/roles` → 200 (sắp `admin`, `learner`). */
export interface RoleInfo {
  code: string
  name: string
  description: string
  permissions: string[]
}

/** `PUT /api/admin/users/{id}/roles` — thay TOÀN BỘ tập vai trò; `[]` hợp lệ (R4-7). */
export interface SetUserRolesRequest {
  roles: string[]
}
