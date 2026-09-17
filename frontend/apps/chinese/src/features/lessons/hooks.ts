import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { SRS_KEYS } from '@/features/srs/hooks'
import { getLesson, getLessons, getQuizAttempts, startLesson, submitQuiz } from './api'
import type { LessonDetail, LessonProgress, QuizResult, SubmitQuizRequest } from './types'

// Khoá query F9. F10 (quản trị) và F11 (tổng quan) invalidate `LESSON_KEYS.all` sau khi sửa/xuất bản bài.
export const LESSON_KEYS = {
  all: ['lessons'] as const,
  list: ['lessons', 'list'] as const,
  detail: (slug: string) => ['lessons', 'detail', slug] as const,
  attempts: (id: string) => ['lessons', 'attempts', id] as const,
}

/** Khoá tổng quan tiến độ của F11 (chưa có hook, nhưng hợp đồng yêu cầu invalidate sẵn sau khi nộp quiz). */
export const PROGRESS_OVERVIEW_KEY = ['progress', 'overview'] as const

const ONE_MINUTE = 60 * 1000

/** 4xx và 503 là câu trả lời thật — không retry; lỗi mạng/5xx khác thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && (err.status === 503 || err.status < 500)) return false
  return failureCount < 1
}

/** Danh sách bài + tiến độ. Quay lại tab sau khi học ở máy khác ⇒ làm mới. */
export function useLessons(enabled = true) {
  return useQuery({
    queryKey: LESSON_KEYS.list,
    queryFn: ({ signal }) => getLessons(signal),
    enabled,
    staleTime: ONE_MINUTE,
    refetchOnWindowFocus: true,
    retry: retryUnlessFinal,
  })
}

export function useLesson(slug: string | undefined) {
  return useQuery({
    queryKey: LESSON_KEYS.detail(slug ?? ''),
    queryFn: ({ signal }) => getLesson(slug!, signal),
    enabled: !!slug,
    staleTime: ONE_MINUTE,
    retry: retryUnlessFinal,
  })
}

/** Lịch sử 5 lần làm gần nhất — chỉ tải khi mở tab Quiz (`enabled`). */
export function useQuizAttempts(lessonId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: LESSON_KEYS.attempts(lessonId ?? ''),
    queryFn: ({ signal }) => getQuizAttempts(lessonId!, 5, signal),
    enabled: !!lessonId && enabled,
    staleTime: ONE_MINUTE,
    retry: retryUnlessFinal,
  })
}

/**
 * "Bắt đầu bài" (R-LS12): ghi `progress` trả về thẳng vào cache chi tiết bài + làm mới danh sách (chip "Đang học").
 * Không có toast — thao tác ngầm khi mở bài lần đầu.
 */
export function useStartLesson(slug: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (lessonId: string) => startLesson(lessonId),
    onSuccess: (progress: LessonProgress) => {
      queryClient.setQueryData<LessonDetail>(LESSON_KEYS.detail(slug), (prev) => (prev ? { ...prev, progress } : prev))
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.list })
    },
  })
}

/**
 * Nộp quiz. Thành công ⇒ ghi `progress` mới vào cache bài, invalidate danh sách bài, lịch sử lần làm, tóm tắt SRS
 * (bài vừa thêm thẻ — huy hiệu "Ôn tập" đổi số) và tổng quan tiến độ (F11). Lỗi giữ nguyên cho `QuizRunner` xử lý
 * (thử lại cùng `clientAttemptId`, hoặc `QUIZ_CHANGED` ⇒ tải lại bài).
 */
export function useSubmitQuiz(slug: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ lessonId, body }: { lessonId: string; body: SubmitQuizRequest }) => submitQuiz(lessonId, body),
    onSuccess: (result: QuizResult, vars) => {
      queryClient.setQueryData<LessonDetail>(LESSON_KEYS.detail(slug), (prev) =>
        prev ? { ...prev, progress: result.progress } : prev,
      )
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.list })
      void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.attempts(vars.lessonId) })
      void queryClient.invalidateQueries({ queryKey: SRS_KEYS.summary })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY })
      // Hoàn thành lần đầu ⇒ `inSrs` của từ trong bài đổi — tải lại chi tiết để tab Từ vựng hiện chip "Đang ôn".
      if (result.firstCompletion) void queryClient.invalidateQueries({ queryKey: LESSON_KEYS.detail(slug) })
    },
  })
}
