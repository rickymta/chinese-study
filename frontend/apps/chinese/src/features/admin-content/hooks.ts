import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { LESSON_KEYS, PROGRESS_OVERVIEW_KEY } from '@/features/lessons/hooks'
import { DICTIONARY_KEYS } from '@/features/dictionary/hooks'
import {
  bulkReviewWords,
  createLesson,
  deleteLesson,
  getAdminLesson,
  getAdminLessons,
  getAdminWord,
  getAdminWords,
  publishLesson,
  replaceLessonBlocks,
  replaceLessonQuiz,
  replaceLessonWords,
  restoreLesson,
  reviewLesson,
  unpublishLesson,
  updateLessonMeta,
  updateWord,
} from './api'
import type {
  AdminLesson,
  AdminLessonsQuery,
  AdminWord,
  AdminWordsPage,
  AdminWordsQuery,
  BulkReviewRequest,
  CreateLessonRequest,
  ReplaceBlocksRequest,
  ReplaceQuizRequest,
  ReplaceWordsRequest,
  UpdateLessonMetaRequest,
  UpdateWordRequest,
  VersionRequest,
} from './types'

export const ADMIN_CONTENT_KEYS = {
  lessons: ['admin-lessons'] as const,
  lessonList: (q: AdminLessonsQuery) => ['admin-lessons', 'list', q.status ?? '', q.q ?? '', q.page ?? 1] as const,
  lesson: (id: string) => ['admin-lessons', 'detail', id] as const,
  words: ['admin-words'] as const,
  wordList: (q: AdminWordsQuery) =>
    ['admin-words', 'list', q.meaningViStatus ?? '', q.hanVietStatus ?? '', q.hsk ?? 0, q.q ?? '', q.page ?? 1] as const,
  word: (id: string) => ['admin-words', 'detail', id] as const,
}

export const ADMIN_LESSONS_PAGE_SIZE = 20
export const ADMIN_WORDS_PAGE_SIZE = 20

/** 4xx là câu trả lời thật — không retry; lỗi mạng/5xx thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && err.status < 500) return false
  return failureCount < 1
}

// ─── Bài học ───

export function useAdminLessons(query: AdminLessonsQuery) {
  return useQuery({
    queryKey: ADMIN_CONTENT_KEYS.lessonList(query),
    queryFn: ({ signal }) => getAdminLessons({ ...query, pageSize: ADMIN_LESSONS_PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
    staleTime: 0,
    retry: retryUnlessFinal,
  })
}

/**
 * Chi tiết bài để soạn. `refetchOnWindowFocus: false` — trang soạn có bản nháp cục bộ; tự tải lại lúc quay lại tab sẽ
 * đổi `version` bất ngờ (bản nháp vẫn giữ nhưng gây rối). Người dùng bấm "Tải lại" khi cần.
 * `staleTime: 0` — MỞ LẠI trang soạn (mount) luôn lấy bản mới: bản trong cache có thể đã cũ vì người khác/tab khác vừa
 * lưu, dùng lại nó ⇒ lần ghi đầu tiên dính 409 oan (kiểm tích hợp F10).
 */
export function useAdminLesson(id: string | undefined) {
  return useQuery({
    queryKey: ADMIN_CONTENT_KEYS.lesson(id ?? ''),
    queryFn: ({ signal }) => getAdminLesson(id!, signal),
    enabled: !!id,
    staleTime: 0,
    refetchOnWindowFocus: false,
    retry: retryUnlessFinal,
  })
}

/**
 * Mọi mutation ghi bài dùng chung `onSuccess`: ghi bài trả về (version mới) vào cache chi tiết, làm mới danh sách admin
 * và cache học viên (`['lessons']` — bài xuất bản/gỡ/sửa phải hiện đúng ở `/bai-hoc`). Lỗi giữ nguyên cho trang xử lý.
 */
function useLessonWriteCache() {
  const queryClient = useQueryClient()
  return (lesson: AdminLesson) => {
    queryClient.setQueryData(ADMIN_CONTENT_KEYS.lesson(lesson.id), lesson)
    void queryClient.invalidateQueries({ queryKey: ['admin-lessons', 'list'] })
    void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.all })
    void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY }) // F11: số bài published / bài tiếp theo
  }
}

export function useCreateLesson() {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (body: CreateLessonRequest) => createLesson(body),
    onSuccess: commit,
  })
}

export function useUpdateLessonMeta(id: string) {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (body: UpdateLessonMetaRequest) => updateLessonMeta(id, body),
    onSuccess: commit,
  })
}

export function useReplaceLessonBlocks(id: string) {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (body: ReplaceBlocksRequest) => replaceLessonBlocks(id, body),
    onSuccess: commit,
  })
}

export function useReplaceLessonWords(id: string) {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (body: ReplaceWordsRequest) => replaceLessonWords(id, body),
    onSuccess: commit,
  })
}

export function useReplaceLessonQuiz(id: string) {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (body: ReplaceQuizRequest) => replaceLessonQuiz(id, body),
    onSuccess: commit,
  })
}

export type LessonAction = 'publish' | 'unpublish' | 'review' | 'restore'

const ACTION_FN: Record<LessonAction, (id: string, body: VersionRequest) => Promise<AdminLesson>> = {
  publish: publishLesson,
  unpublish: unpublishLesson,
  review: reviewLesson,
  restore: restoreLesson,
}

/** Xuất bản / gỡ xuất bản / duyệt / khôi phục — cùng shape `{ version }`, cùng cách cập nhật cache. */
export function useLessonAction(id: string) {
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: ({ action, version }: { action: LessonAction; version: number }) => ACTION_FN[action](id, { version }),
    onSuccess: commit,
  })
}

/**
 * Xoá bài: 200 archived ⇒ ghi bài lưu trữ vào cache; 204 ⇒ chỉ làm mới danh sách. Cả hai làm mới danh sách.
 * 204 KHÔNG `removeQueries` chi tiết: trang soạn còn gắn observer tới lúc `navigate` xong — gỡ cache lúc đó khiến lần
 * render kế tiếp tạo lại query rỗng và GET bài vừa xoá ⇒ 404 ⇒ `createApiClient` đẩy sang `/404` (kiểm tích hợp F10).
 * Bản cache cũ tự bị dọn sau `gcTime` khi không còn ai xem; mở lại id đó thì `staleTime: 0` tải lại ⇒ 404 đúng.
 */
export function useDeleteLesson(id: string) {
  const queryClient = useQueryClient()
  const commit = useLessonWriteCache()
  return useMutation({
    mutationFn: (version: number) => deleteLesson(id, version),
    onSuccess: (result) => {
      if (result.result === 'archived') {
        commit(result.lesson)
        return
      }
      void queryClient.invalidateQueries({ queryKey: ['admin-lessons', 'list'] })
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.all })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY }) // F11: số bài published / bài tiếp theo
    },
  })
}

// ─── Từ vựng ───

export function useAdminWords(query: AdminWordsQuery) {
  return useQuery({
    queryKey: ADMIN_CONTENT_KEYS.wordList(query),
    queryFn: ({ signal }) => getAdminWords({ ...query, pageSize: ADMIN_WORDS_PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
    staleTime: 0,
    retry: retryUnlessFinal,
  })
}

/** Bản mới nhất của một từ (drawer sửa). `initialData` từ danh sách để mở drawer không chờ; vẫn gọi API lấy version mới. */
export function useAdminWord(id: string | null, initialData?: AdminWord) {
  return useQuery({
    queryKey: ADMIN_CONTENT_KEYS.word(id ?? ''),
    queryFn: ({ signal }) => getAdminWord(id!, signal),
    enabled: !!id,
    initialData,
    initialDataUpdatedAt: 0,
    staleTime: 0,
    retry: false,
  })
}

/**
 * Lưu nghĩa/Hán Việt một từ. Thành công ⇒ thay dòng TẠI CHỖ trong mọi trang danh sách đang cache (dòng không còn khớp
 * bộ lọc vẫn ở lại tới lần refetch — tránh nhảy danh sách khi duyệt liên tục), cập nhật chi tiết, và làm mới cache
 * từ điển/bài học của học viên (nghĩa mới phải hiện ở `/tu-dien/:id`, tab Từ vựng của bài).
 */
export function useUpdateWord() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateWordRequest }) => updateWord(id, body),
    onSuccess: (word) => {
      queryClient.setQueryData(ADMIN_CONTENT_KEYS.word(word.id), word)
      queryClient.setQueriesData<AdminWordsPage>({ queryKey: ['admin-words', 'list'] }, (prev) =>
        prev ? { ...prev, items: prev.items.map((w) => (w.id === word.id ? word : w)) } : prev,
      )
      void queryClient.invalidateQueries({ queryKey: DICTIONARY_KEYS.all })
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.all })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY }) // F11: nhất quán với LESSON_KEYS.all (tổng quan có tên bài)
    },
  })
}

/** Duyệt hàng loạt — trang tự refetch danh sách sau khi xong (kể cả khi có `conflicts`). */
export function useBulkReviewWords() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: BulkReviewRequest) => bulkReviewWords(body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_CONTENT_KEYS.words })
      void queryClient.invalidateQueries({ queryKey: DICTIONARY_KEYS.all })
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.all })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY }) // F11: nhất quán với LESSON_KEYS.all (tổng quan có tên bài)
    },
  })
}
