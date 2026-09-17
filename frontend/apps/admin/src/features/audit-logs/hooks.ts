import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { getAuditLogs } from './api'
import type { AuditLogsQuery } from './types'

export const AUDIT_LOGS_PAGE_SIZE = 50

export const AUDIT_LOGS_KEY = (q: AuditLogsQuery) => ['cms', 'audit-logs', q.targetType ?? '', q.targetId ?? '', q.page] as const

/** Danh sách nhật ký theo bộ lọc/trang trên URL; giữ dữ liệu cũ khi chuyển trang để bảng không nháy trắng. */
export function useAuditLogs(query: Omit<AuditLogsQuery, 'pageSize'>) {
  const q: AuditLogsQuery = { ...query, pageSize: AUDIT_LOGS_PAGE_SIZE }
  return useQuery({
    queryKey: AUDIT_LOGS_KEY(q),
    queryFn: ({ signal }) => getAuditLogs(q, signal),
    placeholderData: keepPreviousData,
    staleTime: 0,
  })
}
