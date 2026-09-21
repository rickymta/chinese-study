import { cmsApi } from '@/api/clients'
import type { CmsRole, CmsUser, CmsUsersPage, CmsUsersQuery, SetCmsUserRolesRequest } from './types'

// Lời gọi `GET` danh sách/vai trò KHÔNG đặt `skipErrorRedirect`: 403 (mất `cms:users.manage`) phải kéo sang `/403`
// theo quy tắc "Trang lỗi 4xx thống nhất". Lời gọi ghi (PUT) giữ lỗi để dialog báo tại chỗ (422 LAST_ADMIN...).

/** Phản hồi thô có thể thiếu trường ⇒ bù giá trị an toàn. */
function normalizeUser(raw: Partial<CmsUser> | undefined): CmsUser {
  return {
    id: typeof raw?.id === 'string' ? raw.id : '',
    email: typeof raw?.email === 'string' ? raw.email : '',
    displayName: typeof raw?.displayName === 'string' ? raw.displayName : '',
    roles: Array.isArray(raw?.roles) ? raw.roles.filter((r): r is string => typeof r === 'string') : [],
    isBootstrapAdmin: raw?.isBootstrapAdmin === true,
    firstSeenAt: typeof raw?.firstSeenAt === 'string' ? raw.firstSeenAt : '',
    lastSeenAt: typeof raw?.lastSeenAt === 'string' ? raw.lastSeenAt : '',
  }
}

/** `GET /cms/api/admin/users` — `q` trim ≤ 100, `page` ≥ 1, `pageSize` 1..100 (mặc định 20). */
export async function getCmsUsers(query: CmsUsersQuery, signal?: AbortSignal): Promise<CmsUsersPage> {
  const params: Record<string, string | number> = {}
  const q = query.q?.trim().slice(0, 100)
  if (q) params.q = q
  if (query.page && query.page > 1) params.page = query.page
  if (query.pageSize) params.pageSize = query.pageSize
  const res = await cmsApi.get<Partial<CmsUsersPage>>('/admin/users', { params, signal })
  const data = res.data
  return {
    items: Array.isArray(data?.items) ? data.items.map(normalizeUser) : [],
    page: typeof data?.page === 'number' ? data.page : (query.page ?? 1),
    pageSize: typeof data?.pageSize === 'number' ? data.pageSize : (query.pageSize ?? 20),
    totalCount: typeof data?.totalCount === 'number' ? data.totalCount : 0,
  }
}

/**
 * `GET /cms/api/admin/users/{id}` — dialog tải lại bản mới nhất trước khi sửa. `skipErrorRedirect`: 404 (người dùng
 * vừa biến mất) không được kéo cả trang danh sách sang `/404`; dialog tự báo.
 */
export async function getCmsUser(id: string, signal?: AbortSignal): Promise<CmsUser> {
  const res = await cmsApi.get<Partial<CmsUser>>(`/admin/users/${encodeURIComponent(id)}`, { signal, skipErrorRedirect: true })
  return normalizeUser(res.data)
}

/** `PUT /cms/api/admin/users/{id}/roles` → 200 item · 422 UNKNOWN_ROLE | LAST_ADMIN · 404. */
export async function setCmsUserRoles(id: string, body: SetCmsUserRolesRequest): Promise<CmsUser> {
  const res = await cmsApi.put<Partial<CmsUser>>(`/admin/users/${encodeURIComponent(id)}/roles`, body)
  return normalizeUser(res.data)
}

/** `GET /cms/api/admin/roles` → danh sách vai trò kèm tên tiếng Việt + quyền. */
export async function getCmsRoles(signal?: AbortSignal): Promise<CmsRole[]> {
  const res = await cmsApi.get<Partial<CmsRole>[]>('/admin/roles', { signal })
  return (Array.isArray(res.data) ? res.data : [])
    .filter((r): r is Partial<CmsRole> & { code: string } => typeof r?.code === 'string')
    .map((r) => ({
      code: r.code,
      name: typeof r.name === 'string' ? r.name : r.code,
      permissions: Array.isArray(r.permissions) ? r.permissions.filter((p): p is string => typeof p === 'string') : [],
    }))
}
