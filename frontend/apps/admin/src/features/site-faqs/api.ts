import { cmsApi } from '@/api/clients'
import { DEFAULT_GROUP_KEY, type FaqDto, type FaqInput } from './types'

// GET danh sách KHÔNG `skipErrorRedirect` (403 ⇒ `/403`). Lời gọi ghi giữ lỗi để dialog/trang báo tại chỗ
// (409 CONCURRENCY_CONFLICT, 422 ORDER_MISMATCH, 400 VALIDATION, 404).

const str = (v: unknown, fallback = ''): string => (typeof v === 'string' ? v : fallback)

/** Phản hồi thô có thể thiếu trường ⇒ bù giá trị an toàn. */
export function normalizeFaq(raw: Partial<FaqDto> | undefined): FaqDto {
  return {
    id: str(raw?.id),
    question: str(raw?.question),
    answerMarkdown: str(raw?.answerMarkdown),
    groupKey: str(raw?.groupKey) || DEFAULT_GROUP_KEY,
    sortOrder: typeof raw?.sortOrder === 'number' ? raw.sortOrder : 0,
    isPublished: raw?.isPublished === true,
    version: raw?.version == null ? '' : String(raw.version),
    updatedAt: str(raw?.updatedAt),
  }
}

/** `GET /cms/api/admin/faqs` → mảng sắp theo `groupKey`, `sortOrder`. */
export async function getFaqs(signal?: AbortSignal): Promise<FaqDto[]> {
  const res = await cmsApi.get<Partial<FaqDto>[]>('/admin/faqs', { signal })
  return (Array.isArray(res.data) ? res.data : [])
    .map(normalizeFaq)
    .sort((a, b) => a.groupKey.localeCompare(b.groupKey) || a.sortOrder - b.sortOrder)
}

/** `POST /cms/api/admin/faqs` → 201 · 400 VALIDATION. */
export async function createFaq(body: FaqInput): Promise<FaqDto> {
  const res = await cmsApi.post<Partial<FaqDto>>('/admin/faqs', body)
  return normalizeFaq(res.data)
}

/** `PUT /cms/api/admin/faqs/{id}` (+ `version`) → 200 · 409 CONCURRENCY_CONFLICT · 404 · 400. */
export async function updateFaq(id: string, body: FaqInput & { version: string }): Promise<FaqDto> {
  const res = await cmsApi.put<Partial<FaqDto>>(`/admin/faqs/${encodeURIComponent(id)}`, body)
  return normalizeFaq(res.data)
}

/** `DELETE /cms/api/admin/faqs/{id}` → 204 · 404. */
export async function deleteFaq(id: string): Promise<void> {
  await cmsApi.delete(`/admin/faqs/${encodeURIComponent(id)}`)
}

/** `PUT /cms/api/admin/faqs/order { groupKey, ids }` (đủ id của nhóm) → 204 · 422 ORDER_MISMATCH. */
export async function reorderFaqs(groupKey: string, ids: string[]): Promise<void> {
  await cmsApi.put('/admin/faqs/order', { groupKey, ids })
}
