import { isApiError } from '@af/api'
import { perm } from './permissions'
import type { AdminMeInfo, ServiceMe, ServiceState } from './types'

/**
 * Hàm THUẦN gộp `/me` của nhiều service thành `AdminMeInfo` (hợp đồng W2 §5.3.1) — tách khỏi `loadMe` để test
 * bằng vitest không cần mạng. Luật:
 * - Mỗi service đóng góp quyền có TIỀN TỐ `<code>:<quyền>`; service ngôn ngữ chỉ giữ các quyền trong
 *   `adminPermissions` (vd `study.use` của học viên KHÔNG thành quyền admin — tiêu chí W2 #2).
 * - 401 ở bất kỳ service nào ⇒ NÉM LẠI nguyên lỗi (để `@af/api`/`@af/auth` làm mới hoặc đưa về đăng nhập).
 * - Lỗi khác (mạng, 5xx, 404 khi service chưa chạy) ⇒ `services[code] = unavailable`, KHÔNG ném — chỉ ẩn phần đó.
 * - TẤT CẢ service đều unavailable ⇒ ném (AuthProvider ghi `meError`, `RequireAuth` hiện trang lỗi có "Thử lại").
 */

export interface ServiceMeSource {
  /** Mã service: `cms`, `chinese`... */
  code: string
  /** Tên hiển thị để ghép thông điệp lỗi. */
  label: string
  /** Chỉ giữ các quyền này (không tiền tố). Bỏ trống ⇒ giữ mọi quyền service trả (cms). */
  adminPermissions?: readonly string[]
}

export interface ServiceMeResult extends ServiceMeSource {
  outcome: PromiseSettledResult<unknown>
}

const str = (v: unknown): string => (typeof v === 'string' ? v : '')
const strArray = (v: unknown): string[] => (Array.isArray(v) ? v.filter((x): x is string => typeof x === 'string') : [])

/** Fail-closed: thân phản hồi thiếu/sai kiểu ⇒ mảng rỗng, không nổ ở `new Set(...)`. */
export function normalizeServiceMe(raw: unknown): ServiceMe {
  const data = raw && typeof raw === 'object' ? (raw as Record<string, unknown>) : {}
  return {
    id: str(data.id),
    email: str(data.email),
    displayName: str(data.displayName),
    roles: strArray(data.roles),
    permissions: strArray(data.permissions),
  }
}

/** 401 thật từ service (không làm mới được / sai audience) — không được nuốt thành "unavailable". */
export const isUnauthorizedError = (err: unknown): boolean => isApiError(err) && err.status === 401

/** Thông điệp ngắn của một lỗi để ghép vào câu báo "không service nào phản hồi". */
const describe = (err: unknown): string =>
  isApiError(err) ? err.message : err instanceof Error ? err.message : 'lỗi không xác định'

export function mergeServiceResults(results: readonly ServiceMeResult[]): AdminMeInfo {
  const services: Record<string, ServiceState> = {}
  const permissions: string[] = []

  for (const r of results) {
    if (r.outcome.status === 'rejected') {
      if (isUnauthorizedError(r.outcome.reason)) throw r.outcome.reason
      services[r.code] = { status: 'unavailable', error: r.outcome.reason }
      continue
    }
    const me = normalizeServiceMe(r.outcome.value)
    services[r.code] = { status: 'ok', me }
    const allowed = r.adminPermissions ? new Set(r.adminPermissions) : null
    for (const code of me.permissions) {
      if (allowed && !allowed.has(code)) continue
      const prefixed = perm(r.code, code)
      if (!permissions.includes(prefixed)) permissions.push(prefixed)
    }
  }

  const okCount = Object.values(services).filter((s) => s.status === 'ok').length
  if (results.length > 0 && okCount === 0) {
    const detail = results
      .map((r) => `${r.label}: ${r.outcome.status === 'rejected' ? describe(r.outcome.reason) : '?'}`)
      .join('; ')
    throw new Error(`Không dịch vụ nào phản hồi (${detail}). Kiểm tra gateway và các backend đã chạy chưa.`)
  }

  return { permissions, services }
}

/** Có ít nhất một service không phản hồi — Dashboard cảnh báo, `RequireAuth` không đẩy `/403` khi 0 quyền. */
export const hasUnavailableService = (me: AdminMeInfo): boolean =>
  Object.values(me.services).some((s) => s.status === 'unavailable')
