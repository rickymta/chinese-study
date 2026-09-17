/** Hợp đồng §6.2 W3b — nhật ký thao tác CMS. */
export interface AuditLogDto {
  id: string
  at: string
  actorId: string | null
  actorEmail: string | null
  action: string
  targetType: string | null
  targetId: string | null
  summary: string
  success: boolean
}

export interface AuditLogsQuery {
  targetType?: string
  targetId?: string
  page: number
  pageSize: number
}

export interface AuditLogsPage {
  items: AuditLogDto[]
  page: number
  pageSize: number
  totalCount: number
}

/** Bộ lọc đối tượng (hợp đồng: Tất cả / site_settings / language / faq). */
export const AUDIT_TARGET_TYPES: readonly { value: string; label: string }[] = [
  { value: '', label: 'Tất cả' },
  { value: 'site_settings', label: 'Cấu hình website' },
  { value: 'language', label: 'Ngôn ngữ' },
  { value: 'faq', label: 'Câu hỏi thường gặp' },
]
