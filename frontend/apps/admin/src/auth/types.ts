import type { MeInfo } from '@af/auth'

/**
 * Phản hồi `GET /<service>/api/me` — cùng shape ở cms-backend (§6.1) và chinese-backend (§6.3 hợp đồng gốc):
 * hồ sơ đã provision + vai trò/quyền CỤC BỘ của service đó. Thiếu trường nào thì `loadMe` bù giá trị an toàn.
 */
export interface ServiceMe {
  /** = `sub` của JWT (id tài khoản identity). */
  id: string
  email: string
  displayName: string
  /** Mã vai trò cục bộ của service (`admin`, `editor`... / `admin`, `learner`...). */
  roles: string[]
  /** Mã quyền cục bộ KHÔNG tiền tố (`site.manage`, `content.manage`...). */
  permissions: string[]
}

/** Trạng thái một service khi mở admin: trả lời được, hoặc không phản hồi (mạng/5xx/404 — KHÔNG phải 401). */
export type ServiceState = { status: 'ok'; me: ServiceMe } | { status: 'unavailable'; error: unknown }

/**
 * Kết quả gộp `/me` của mọi service (hợp đồng W2 §5.3.1). `permissions` ĐÃ gắn tiền tố (`cms:site.manage`,
 * `chinese:content.manage`) — `useAuth().can('cms:site.manage')` dùng thẳng. `services` để Dashboard báo
 * "Tiếng Trung: không phản hồi" và `RequireAuth` không đẩy `/403` oan khi có service chết.
 */
export interface AdminMeInfo extends MeInfo {
  permissions: string[]
  services: Record<string, ServiceState>
}
