import { cmsApi } from '@/api/clients'
import { LANGUAGE_STATUSES, type LanguageDto, type LanguageInput, type LanguageStatus } from './types'

// GET danh sách KHÔNG `skipErrorRedirect` (403 ⇒ `/403`). Lời gọi ghi giữ lỗi để drawer/trang báo tại chỗ
// (409 CODE_TAKEN / CONCURRENCY_CONFLICT, 422 APP_URL_REQUIRED / ORDER_MISMATCH, 400 VALIDATION).

const str = (v: unknown, fallback = ''): string => (typeof v === 'string' ? v : fallback)

/** Phản hồi thô có thể thiếu trường ⇒ bù giá trị an toàn. */
export function normalizeLanguage(raw: Partial<LanguageDto> | undefined): LanguageDto {
  const status = str(raw?.status) as LanguageStatus
  return {
    id: str(raw?.id),
    code: str(raw?.code),
    name: str(raw?.name),
    nativeName: str(raw?.nativeName),
    tagline: str(raw?.tagline),
    descriptionMarkdown: str(raw?.descriptionMarkdown),
    status: LANGUAGE_STATUSES.includes(status) ? status : 'hidden',
    appUrl: typeof raw?.appUrl === 'string' && raw.appUrl ? raw.appUrl : null,
    accentColor: typeof raw?.accentColor === 'string' && raw.accentColor ? raw.accentColor : null,
    coverMedia: raw?.coverMedia ?? null,
    sortOrder: typeof raw?.sortOrder === 'number' ? raw.sortOrder : 0,
    // `version` có thể về dạng số nếu BE serialize xmin khác — ép chuỗi để gửi lại nguyên.
    version: raw?.version == null ? '' : String(raw.version),
    updatedAt: str(raw?.updatedAt),
  }
}

/** `GET /cms/api/admin/languages` → mảng theo `sortOrder`. */
export async function getLanguages(signal?: AbortSignal): Promise<LanguageDto[]> {
  const res = await cmsApi.get<Partial<LanguageDto>[]>('/admin/languages', { signal })
  return (Array.isArray(res.data) ? res.data : []).map(normalizeLanguage).sort((a, b) => a.sortOrder - b.sortOrder)
}

/** `POST /cms/api/admin/languages` → 201 · 409 CODE_TAKEN · 422 APP_URL_REQUIRED · 400 VALIDATION. */
export async function createLanguage(body: LanguageInput & { code: string }): Promise<LanguageDto> {
  const res = await cmsApi.post<Partial<LanguageDto>>('/admin/languages', body)
  return normalizeLanguage(res.data)
}

/** `PUT /cms/api/admin/languages/{id}` (không `code`, có `version`) → 200 · 409 CONCURRENCY_CONFLICT · 422 · 404. */
export async function updateLanguage(id: string, body: LanguageInput & { version: string }): Promise<LanguageDto> {
  const res = await cmsApi.put<Partial<LanguageDto>>(`/admin/languages/${encodeURIComponent(id)}`, body)
  return normalizeLanguage(res.data)
}

/** `DELETE /cms/api/admin/languages/{id}` → 204 · 404. */
export async function deleteLanguage(id: string): Promise<void> {
  await cmsApi.delete(`/admin/languages/${encodeURIComponent(id)}`)
}

/** `PUT /cms/api/admin/languages/order { ids }` (đủ mọi id) → 204 · 422 ORDER_MISMATCH. */
export async function reorderLanguages(ids: string[]): Promise<void> {
  await cmsApi.put('/admin/languages/order', { ids })
}
