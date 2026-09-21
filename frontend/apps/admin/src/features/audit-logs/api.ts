import { cmsApi } from '@/api/clients'
import type { AuditLogDto, AuditLogsPage, AuditLogsQuery } from './types'

const str = (v: unknown, fallback = ''): string => (typeof v === 'string' ? v : fallback)
const nullableStr = (v: unknown): string | null => (typeof v === 'string' && v ? v : null)

function normalizeLog(raw: Partial<AuditLogDto> | undefined): AuditLogDto {
  return {
    id: str(raw?.id),
    at: str(raw?.at),
    actorId: nullableStr(raw?.actorId),
    actorEmail: nullableStr(raw?.actorEmail),
    action: str(raw?.action),
    targetType: nullableStr(raw?.targetType),
    targetId: nullableStr(raw?.targetId),
    summary: str(raw?.summary),
    // Thiếu trường ⇒ coi là thành công (đa số bản ghi); BE luôn gửi bool.
    success: raw?.success !== false,
  }
}

/** `GET /cms/api/admin/audit-logs?targetType=&targetId=&page=&pageSize=` — cần `cms:users.manage` (403 ⇒ `/403`). */
export async function getAuditLogs(query: AuditLogsQuery, signal?: AbortSignal): Promise<AuditLogsPage> {
  const params: Record<string, string | number> = { page: query.page, pageSize: query.pageSize }
  if (query.targetType) params.targetType = query.targetType
  if (query.targetId) params.targetId = query.targetId
  const res = await cmsApi.get<Partial<AuditLogsPage>>('/admin/audit-logs', { params, signal })
  const data = res.data ?? {}
  return {
    items: (Array.isArray(data.items) ? data.items : []).map(normalizeLog),
    page: typeof data.page === 'number' ? data.page : query.page,
    pageSize: typeof data.pageSize === 'number' ? data.pageSize : query.pageSize,
    totalCount: typeof data.totalCount === 'number' ? data.totalCount : 0,
  }
}
