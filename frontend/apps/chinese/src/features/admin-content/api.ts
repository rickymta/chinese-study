import { chineseApi } from '@/api/clients'
import type {
  AdminLesson,
  AdminLessonListItem,
  AdminLessonsPage,
  AdminLessonsQuery,
  AdminWord,
  AdminWordsPage,
  AdminWordsQuery,
  BulkReviewRequest,
  BulkReviewResult,
  CreateLessonRequest,
  DeleteLessonResult,
  ReplaceBlocksRequest,
  ReplaceQuizRequest,
  ReplaceWordsRequest,
  UpdateLessonMetaRequest,
  UpdateWordRequest,
  VersionRequest,
} from './types'

// Hợp đồng F8–F11 §6.3 — trình duyệt gọi `/chinese/api/admin/...`, cần `content.manage`.
// Lời gọi GET danh sách/chi tiết KHÔNG đặt `skipErrorRedirect`: 403 (mất quyền) ⇒ `/403`, 404 (bài không tồn tại)
// ⇒ `/404` theo quy tắc "Trang lỗi 4xx thống nhất". Mọi lời gọi GHI giữ lỗi để màn hình báo tại chỗ
// (409 CONCURRENCY_CONFLICT | SLUG_TAKEN, 422 LESSON_NOT_PUBLISHABLE | SLUG_LOCKED | LESSON_ARCHIVED..., 400 VALIDATION).

const lessonPath = (id: string) => `/admin/lessons/${encodeURIComponent(id)}`
const wordPath = (id: string) => `/admin/words/${encodeURIComponent(id)}`

/** Bù trường mảng có thể bị lược (serializer `WhenWritingNull`, RK41) để màn hình không phải kiểm null khắp nơi. */
export function normalizeAdminLesson(raw: AdminLesson): AdminLesson {
  return {
    ...raw,
    objectives: raw.objectives ?? [],
    glossary: raw.glossary ?? [],
    blocks: raw.blocks ?? [],
    words: raw.words ?? [],
    quiz: (raw.quiz ?? []).map((q) => ({ ...q, options: q.options ?? [] })),
    warnings: raw.warnings ?? [],
    hasAttempts: raw.hasAttempts === true,
  }
}

function normalizeListItem(raw: Partial<AdminLessonListItem>): AdminLessonListItem {
  return {
    id: raw.id ?? '',
    slug: raw.slug ?? '',
    title: raw.title ?? '',
    orderIndex: typeof raw.orderIndex === 'number' ? raw.orderIndex : 0,
    status: raw.status ?? 'draft',
    reviewStatus: raw.reviewStatus ?? 'machine',
    source: raw.source ?? 'admin',
    wordCount: typeof raw.wordCount === 'number' ? raw.wordCount : 0,
    questionCount: typeof raw.questionCount === 'number' ? raw.questionCount : 0,
    editedAt: raw.editedAt ?? null,
    publishedAt: raw.publishedAt ?? null,
  }
}

/** `GET /api/admin/lessons?status=&q=&page=&pageSize=` — mọi trạng thái, mặc định ẩn `archived`. */
export async function getAdminLessons(query: AdminLessonsQuery, signal?: AbortSignal): Promise<AdminLessonsPage> {
  const params: Record<string, string | number> = {}
  if (query.status) params.status = query.status
  const q = query.q?.trim().slice(0, 100)
  if (q) params.q = q
  if (query.page && query.page > 1) params.page = query.page
  if (query.pageSize) params.pageSize = query.pageSize
  const res = await chineseApi.get<Partial<AdminLessonsPage>>('/admin/lessons', { params, signal })
  const data = res.data
  return {
    items: Array.isArray(data?.items) ? data.items.map(normalizeListItem) : [],
    page: typeof data?.page === 'number' ? data.page : (query.page ?? 1),
    pageSize: typeof data?.pageSize === 'number' ? data.pageSize : (query.pageSize ?? 20),
    totalCount: typeof data?.totalCount === 'number' ? data.totalCount : 0,
  }
}

/** `GET /api/admin/lessons/{id}` — 404 ⇒ `createApiClient` tự điều hướng `/404`. */
export async function getAdminLesson(id: string, signal?: AbortSignal): Promise<AdminLesson> {
  const res = await chineseApi.get<AdminLesson>(lessonPath(id), { signal })
  return normalizeAdminLesson(res.data)
}

/** `POST /api/admin/lessons` → 201 `draft` · 400 · 409 `SLUG_TAKEN`. */
export async function createLesson(body: CreateLessonRequest): Promise<AdminLesson> {
  const res = await chineseApi.post<AdminLesson>('/admin/lessons', body)
  return normalizeAdminLesson(res.data)
}

/** `PUT /api/admin/lessons/{id}` — thông tin chung. 409 CONFLICT|SLUG_TAKEN · 422 SLUG_LOCKED|LESSON_ARCHIVED. */
export async function updateLessonMeta(id: string, body: UpdateLessonMetaRequest): Promise<AdminLesson> {
  const res = await chineseApi.put<AdminLesson>(lessonPath(id), body)
  return normalizeAdminLesson(res.data)
}

/** `PUT /api/admin/lessons/{id}/blocks` — thay TOÀN BỘ khối. 400 có `details` theo đường dẫn payload. */
export async function replaceLessonBlocks(id: string, body: ReplaceBlocksRequest): Promise<AdminLesson> {
  const res = await chineseApi.put<AdminLesson>(`${lessonPath(id)}/blocks`, body)
  return normalizeAdminLesson(res.data)
}

/** `PUT /api/admin/lessons/{id}/words` — thay TOÀN BỘ từ của bài. 422 `UNKNOWN_WORD` (`details.wordIds`). */
export async function replaceLessonWords(id: string, body: ReplaceWordsRequest): Promise<AdminLesson> {
  const res = await chineseApi.put<AdminLesson>(`${lessonPath(id)}/words`, body)
  return normalizeAdminLesson(res.data)
}

/** `PUT /api/admin/lessons/{id}/quiz` — thay TOÀN BỘ quiz (`correctIndex` 0-based). 422 `UNKNOWN_QUESTION`. */
export async function replaceLessonQuiz(id: string, body: ReplaceQuizRequest): Promise<AdminLesson> {
  const res = await chineseApi.put<AdminLesson>(`${lessonPath(id)}/quiz`, body)
  return normalizeAdminLesson(res.data)
}

/** `POST .../publish` → 200 kèm `warnings` · 422 `LESSON_NOT_PUBLISHABLE` (`details.problems`) | `LESSON_ARCHIVED`. */
export async function publishLesson(id: string, body: VersionRequest): Promise<AdminLesson> {
  const res = await chineseApi.post<AdminLesson>(`${lessonPath(id)}/publish`, body)
  return normalizeAdminLesson(res.data)
}

/** `POST .../unpublish` → `draft`. */
export async function unpublishLesson(id: string, body: VersionRequest): Promise<AdminLesson> {
  const res = await chineseApi.post<AdminLesson>(`${lessonPath(id)}/unpublish`, body)
  return normalizeAdminLesson(res.data)
}

/** `POST .../review` → `reviewStatus = reviewed` (R-CA6) — bài `draft` cũng duyệt được. */
export async function reviewLesson(id: string, body: VersionRequest): Promise<AdminLesson> {
  const res = await chineseApi.post<AdminLesson>(`${lessonPath(id)}/review`, body)
  return normalizeAdminLesson(res.data)
}

/** `POST .../restore` → `archived` ⇒ `draft` · 422 `LESSON_NOT_ARCHIVED`. */
export async function restoreLesson(id: string, body: VersionRequest): Promise<AdminLesson> {
  const res = await chineseApi.post<AdminLesson>(`${lessonPath(id)}/restore`, body)
  return normalizeAdminLesson(res.data)
}

/**
 * `DELETE /api/admin/lessons/{id}?version=` — 204 xoá cứng (bài admin chưa ai làm) hoặc 200
 * `{ result: 'archived', lesson }` (R-CA7). Phân biệt bằng mã HTTP, không tin thân response 204 (rỗng).
 */
export async function deleteLesson(id: string, version: number): Promise<DeleteLessonResult> {
  const res = await chineseApi.delete<{ result?: string; lesson?: AdminLesson } | undefined>(lessonPath(id), { params: { version } })
  if (res.status === 204 || !res.data?.lesson) return { result: 'deleted' }
  return { result: 'archived', lesson: normalizeAdminLesson(res.data.lesson) }
}

// ─── Từ vựng ───

function normalizeWord(raw: AdminWord): AdminWord {
  return {
    ...raw,
    meaningsVi: raw.meaningsVi ?? [],
    meaningsEn: raw.meaningsEn ?? [],
    hanViet: raw.hanViet ?? null,
    hanVietStatus: raw.hanVietStatus ?? 'derived',
  }
}

/** `GET /api/admin/words?meaningViStatus=&hanVietStatus=&hsk=&q=&page=&pageSize=` — sắp `path_order` (R-CA11). */
export async function getAdminWords(query: AdminWordsQuery, signal?: AbortSignal): Promise<AdminWordsPage> {
  const params: Record<string, string | number> = {}
  if (query.meaningViStatus) params.meaningViStatus = query.meaningViStatus
  if (query.hanVietStatus) params.hanVietStatus = query.hanVietStatus
  if (query.hsk) params.hsk = query.hsk
  const q = query.q?.trim().slice(0, 64)
  if (q) params.q = q
  if (query.page && query.page > 1) params.page = query.page
  if (query.pageSize) params.pageSize = query.pageSize
  const res = await chineseApi.get<Partial<AdminWordsPage>>('/admin/words', { params, signal })
  const data = res.data
  return {
    items: Array.isArray(data?.items) ? data.items.map(normalizeWord) : [],
    page: typeof data?.page === 'number' ? data.page : (query.page ?? 1),
    pageSize: typeof data?.pageSize === 'number' ? data.pageSize : (query.pageSize ?? 20),
    totalCount: typeof data?.totalCount === 'number' ? data.totalCount : 0,
  }
}

/**
 * `GET /api/admin/words/{id}` — drawer tải bản mới nhất (version) trước khi sửa. `skipErrorRedirect`: 404 (từ vừa
 * biến mất) không được kéo cả trang danh sách sang `/404`; drawer tự báo.
 */
export async function getAdminWord(id: string, signal?: AbortSignal): Promise<AdminWord> {
  const res = await chineseApi.get<AdminWord>(wordPath(id), { signal, skipErrorRedirect: true })
  return normalizeWord(res.data)
}

/** `PUT /api/admin/words/{id}` → 200 `AdminWord` · 400 · 404 · 409 `CONCURRENCY_CONFLICT`. */
export async function updateWord(id: string, body: UpdateWordRequest): Promise<AdminWord> {
  const res = await chineseApi.put<AdminWord>(wordPath(id), body)
  return normalizeWord(res.data)
}

/** `POST /api/admin/words/review` — duyệt hàng loạt (chỉ đổi `meaning_vi_status`), mục lệch version vào `conflicts`. */
export async function bulkReviewWords(body: BulkReviewRequest): Promise<BulkReviewResult> {
  const res = await chineseApi.post<Partial<BulkReviewResult>>('/admin/words/review', body)
  return {
    updated: typeof res.data?.updated === 'number' ? res.data.updated : 0,
    conflicts: Array.isArray(res.data?.conflicts) ? res.data.conflicts : [],
    notFound: Array.isArray(res.data?.notFound) ? res.data.notFound : [],
  }
}
