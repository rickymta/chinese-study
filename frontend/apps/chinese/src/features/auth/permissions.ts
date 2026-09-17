/**
 * Danh mục quyền/vai trò cục bộ của service tiếng Trung (hợp đồng §3.3 R-P2). Đây chỉ là HẰNG để tránh gõ sai
 * chuỗi — quyền hiệu lực luôn do `GET /chinese/api/me` trả, frontend KHÔNG suy quyền từ vai trò hay JWT.
 */
export const PERMISSIONS = {
  /** Dùng mọi chức năng học. */
  STUDY_USE: 'study.use',
  /** Soạn/sửa/xuất bản bài học, quiz, duyệt nghĩa từ vựng (F10). */
  CONTENT_MANAGE: 'content.manage',
  /** Xem người dùng của service, gán vai trò (F4) — cũng là quyền canh gác `GET /api/admin/ping`. */
  USERS_MANAGE: 'users.manage',
} as const

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS]

/** Nhãn tiếng Việt để hiển thị; mã lạ (backend thêm sau) thì hiện nguyên mã. */
export const PERMISSION_LABELS: Record<string, string> = {
  [PERMISSIONS.STUDY_USE]: 'Học tập',
  [PERMISSIONS.CONTENT_MANAGE]: 'Soạn nội dung',
  [PERMISSIONS.USERS_MANAGE]: 'Quản lý người dùng',
}

export const ROLE_LABELS: Record<string, string> = {
  admin: 'Quản trị viên',
  learner: 'Học viên',
}

export const labelOfPermission = (code: string): string => PERMISSION_LABELS[code] ?? code
export const labelOfRole = (code: string): string => ROLE_LABELS[code] ?? code
