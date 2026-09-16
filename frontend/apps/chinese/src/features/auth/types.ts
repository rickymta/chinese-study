import type { MeInfo } from '@af/auth'

/**
 * Phản hồi `GET /chinese/api/me` (hợp đồng §6.3): hồ sơ đã provision ở `access.users` + vai trò/quyền CỤC BỘ
 * của service tiếng Trung. `@af/auth` chỉ đọc `permissions`; phần còn lại app đọc qua `useMe()`.
 *
 * Giả định F3 (backend làm song song, bám hợp đồng): `roles`/`permissions` là mảng mã (`learner`, `study.use`...),
 * `firstSeenAt` ISO-8601 UTC. Thiếu trường nào thì `loadMe` bù mảng rỗng (fail-closed), không nổ.
 */
export interface ChineseMe extends MeInfo {
  /** = `sub` của JWT (id tài khoản identity). */
  id: string
  email: string
  displayName: string
  /** Múi giờ IANA đã đồng bộ từ claim `zoneinfo` — "hôm nay" của người học tính theo đây. */
  timeZone: string
  /** Mã vai trò cục bộ: `admin`, `learner`. */
  roles: string[]
  /** Mã quyền cục bộ: `study.use`, `content.manage`, `users.manage` (§3.3). */
  permissions: string[]
  /** Lần đầu vào service tiếng Trung (provision). */
  firstSeenAt: string
}
