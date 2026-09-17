import { chineseApi } from '@/api/clients'
import type {
  LessonDetail,
  LessonListResponse,
  LessonProgress,
  QuizAttemptsResponse,
  QuizResult,
  SubmitQuizRequest,
} from '@af/chinese-kit'

// Hợp đồng F8–F11 §6.1 (trình duyệt gọi `/chinese/api/lessons/...`). Lời gọi GET không đặt `skipErrorRedirect`:
// 403 (mất `study.use`) và 404 (bài không published — R-LS1) phải đi tới trang lỗi dùng chung.
// Lời gọi GHI giữ lỗi để màn hình báo tại chỗ (createApiClient chỉ điều hướng GET).

/** `GET /api/lessons` — danh sách bài `published` sắp theo `orderIndex`, kèm tiến độ + `nextLessonSlug`. */
export async function getLessons(signal?: AbortSignal): Promise<LessonListResponse> {
  const res = await chineseApi.get<LessonListResponse>('/lessons', { signal })
  return { items: res.data.items ?? [], nextLessonSlug: res.data.nextLessonSlug ?? null }
}

/** `GET /api/lessons/{slug}` — chi tiết bài (không có đáp án quiz, R-LS10). 404 khi không published. */
export async function getLesson(slug: string, signal?: AbortSignal): Promise<LessonDetail> {
  const res = await chineseApi.get<LessonDetail>(`/lessons/${encodeURIComponent(slug)}`, { signal })
  const d = res.data
  return {
    ...d,
    objectives: d.objectives ?? [],
    glossary: d.glossary ?? [],
    blocks: d.blocks ?? [],
    words: d.words ?? [],
    quiz: (d.quiz ?? []).map((q) => ({ ...q, options: q.options ?? [] })),
  }
}

/** `POST /api/lessons/{id}/start` — tạo `lesson_progress(in_progress)` nếu chưa có; idempotent (R-LS12). */
export async function startLesson(id: string): Promise<LessonProgress> {
  const res = await chineseApi.post<LessonProgress>(`/lessons/${encodeURIComponent(id)}/start`)
  return res.data
}

/**
 * `POST /api/lessons/{id}/quiz-attempts` — chấm ở server; 201 lần đầu, 200 khi gửi lại cùng `clientAttemptId`.
 * Lỗi: 400 VALIDATION · 404 · 409 DUPLICATE_ATTEMPT_ID · 422 QUIZ_EMPTY | QUIZ_CHANGED (R-LS8).
 */
export async function submitQuiz(id: string, body: SubmitQuizRequest): Promise<QuizResult> {
  const res = await chineseApi.post<QuizResult>(`/lessons/${encodeURIComponent(id)}/quiz-attempts`, body)
  return { ...res.data, results: res.data.results ?? [] }
}

/** `GET /api/lessons/{id}/quiz-attempts?limit=` (1–20) — lịch sử lần làm, mới nhất trước, không kèm chi tiết câu. */
export async function getQuizAttempts(id: string, limit = 5, signal?: AbortSignal): Promise<QuizAttemptsResponse> {
  const res = await chineseApi.get<QuizAttemptsResponse>(`/lessons/${encodeURIComponent(id)}/quiz-attempts`, {
    params: { limit },
    signal,
  })
  return { items: res.data.items ?? [] }
}
