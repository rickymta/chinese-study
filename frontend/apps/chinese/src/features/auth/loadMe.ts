import { chineseApi } from '@/api/clients'
import type { ChineseMe } from './types'

/**
 * `GET /chinese/api/me` (hợp đồng §6.3) — nguồn sự thật DUY NHẤT về vai trò/quyền ở service tiếng Trung (F3).
 * Truyền vào `AuthProvider` (`loadMe`); được gọi sau mỗi lần có phiên và khi `reloadMe()`.
 *
 * `skipErrorRedirect`: lời gọi này chạy NỀN ngay khi mở app — chinese-backend chưa chạy (404/502) hay từ chối
 * (403) thì `RequireAuth` đã có trang "Không tải được hồ sơ" với nút Thử lại/Đăng xuất; không được kéo cả trang
 * sang `/404` rồi lại gọi `/me` thêm lần nữa. 401 vẫn đi qua làm mới token + `onAuthLost` như mọi request.
 */
export async function loadMe(): Promise<ChineseMe> {
  const res = await chineseApi.get<Partial<ChineseMe>>('/me', { skipErrorRedirect: true })
  const data = res.data
  // Fail-closed: backend trả thiếu/sai kiểu `permissions` ⇒ coi như 0 quyền (về /403), không nổ ở `new Set(...)`.
  return {
    id: typeof data?.id === 'string' ? data.id : '',
    email: typeof data?.email === 'string' ? data.email : '',
    displayName: typeof data?.displayName === 'string' ? data.displayName : '',
    timeZone: typeof data?.timeZone === 'string' && data.timeZone ? data.timeZone : 'Asia/Ho_Chi_Minh',
    roles: Array.isArray(data?.roles) ? data.roles.filter((r): r is string => typeof r === 'string') : [],
    permissions: Array.isArray(data?.permissions) ? data.permissions.filter((p): p is string => typeof p === 'string') : [],
    firstSeenAt: typeof data?.firstSeenAt === 'string' ? data.firstSeenAt : '',
  }
}
