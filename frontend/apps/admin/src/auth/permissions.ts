/**
 * Quyền trong admin GỘP từ nhiều service, luôn mang TIỀN TỐ service để không đụng tên (R-W11: `users.manage` có ở
 * cả cms lẫn chinese). Đây chỉ là HẰNG để tránh gõ sai chuỗi — quyền hiệu lực do `GET /cms/api/me` và
 * `GET /<ngôn-ngữ>/api/me` trả, frontend KHÔNG suy quyền từ vai trò hay JWT.
 */
export const perm = (service: string, code: string): string => `${service}:${code}`

/** Tách `cms:site.manage` → `{ service: 'cms', code: 'site.manage' }`; chuỗi không có tiền tố ⇒ `service` rỗng. */
export function splitPerm(prefixed: string): { service: string; code: string } {
  const i = prefixed.indexOf(':')
  return i < 0 ? { service: '', code: prefixed } : { service: prefixed.slice(0, i), code: prefixed.slice(i + 1) }
}

export const CMS_SERVICE = 'cms'

/** Quyền cms-backend (R-W9) đã gắn tiền tố. */
export const CMS_PERMS = {
  SITE_MANAGE: perm(CMS_SERVICE, 'site.manage'),
  POSTS_MANAGE: perm(CMS_SERVICE, 'posts.manage'),
  MEDIA_MANAGE: perm(CMS_SERVICE, 'media.manage'),
  INBOX_MANAGE: perm(CMS_SERVICE, 'inbox.manage'),
  ACCOUNTS_MANAGE: perm(CMS_SERVICE, 'accounts.manage'),
  USERS_MANAGE: perm(CMS_SERVICE, 'users.manage'),
} as const

/** Nhãn tiếng Việt theo MÃ KHÔNG tiền tố (dùng chung cho cms và service ngôn ngữ); mã lạ hiện nguyên mã. */
const PERMISSION_LABELS: Record<string, string> = {
  'site.manage': 'Cấu hình website',
  'posts.manage': 'Bài viết',
  'media.manage': 'Thư viện ảnh',
  'inbox.manage': 'Hộp thư',
  'accounts.manage': 'Tài khoản nền tảng',
  'users.manage': 'Quản lý người dùng',
  'content.manage': 'Soạn nội dung',
  'study.use': 'Học tập',
}

export const CMS_ROLE_LABELS: Record<string, string> = {
  admin: 'Quản trị viên',
  editor: 'Biên tập website',
  support: 'Hỗ trợ người học',
}

/** Nhãn của một mã quyền (có hoặc không có tiền tố). */
export const labelOfPermission = (code: string): string => {
  const { code: bare } = splitPerm(code)
  return PERMISSION_LABELS[bare] ?? bare
}

export const labelOfCmsRole = (code: string): string => CMS_ROLE_LABELS[code] ?? code
