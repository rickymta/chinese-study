import { useCallback } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { DICTIONARY_KEYS } from '@/features/dictionary/hooks'
import { addSrsCards, getLearningSettings, getSrsSummary, putLearningSettings, setSrsCardSuspension } from './api'
import type { LearningSettings, LearningSettingsResponse, SrsSummary } from './types'

// Khoá query dùng chung: F9/F11 invalidate `['srs', 'summary']` sau khi hoàn thành bài/nộp quiz (tài liệu F8–F11 §5.3).
export const SRS_KEYS = {
  all: ['srs'] as const,
  summary: ['srs', 'summary'] as const,
  learningSettings: ['me', 'learning-settings'] as const,
}

const THIRTY_SECONDS = 30 * 1000
const FIVE_MINUTES = 5 * 60 * 1000

/** 4xx và 503 là câu trả lời thật — không retry; lỗi mạng/5xx khác thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && (err.status === 503 || err.status < 500)) return false
  return failureCount < 1
}

/**
 * Tóm tắt SRS (`/on-tap`, huy hiệu menu). `refetchOnWindowFocus`: người học quay lại tab sau vài giờ thì số đến hạn
 * đã khác. `enabled` do bên gọi quyết định (menu chỉ hỏi khi có `study.use`).
 */
export function useSrsSummary(enabled = true) {
  return useQuery({
    queryKey: SRS_KEYS.summary,
    queryFn: ({ signal }) => getSrsSummary(signal),
    enabled,
    staleTime: THIRTY_SECONDS,
    refetchOnWindowFocus: true,
    retry: retryUnlessFinal,
  })
}

/** Cài đặt học (`ttsRate`, `autoPlayAudio`, hạn mức...) — ít đổi, giữ 5 phút; mọi trang có TTS đều đọc. */
export function useLearningSettings(enabled = true) {
  return useQuery({
    queryKey: SRS_KEYS.learningSettings,
    queryFn: ({ signal }) => getLearningSettings(signal),
    enabled,
    staleTime: FIVE_MINUTES,
    retry: retryUnlessFinal,
  })
}

/** Lưu cài đặt học ⇒ ghi thẳng cache + invalidate `summary` (hạn mức mới đổi số "từ mới còn học được"). */
export function useUpdateLearningSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: LearningSettings) => putLearningSettings(body),
    onSuccess: (saved: LearningSettingsResponse) => {
      queryClient.setQueryData(SRS_KEYS.learningSettings, saved)
      void queryClient.invalidateQueries({ queryKey: SRS_KEYS.summary })
    },
  })
}

/** Thêm thẻ tự chọn (chi tiết từ) ⇒ invalidate `summary` + chi tiết từ (để `word.srs` đổi thành "Chờ học"). */
export function useAddCards() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (wordIds: string[]) => addSrsCards(wordIds),
    onSuccess: (_res, wordIds) => {
      void queryClient.invalidateQueries({ queryKey: SRS_KEYS.summary })
      for (const id of wordIds) void queryClient.invalidateQueries({ queryKey: DICTIONARY_KEYS.word(id) })
    },
  })
}

/** Tạm dừng / tiếp tục một thẻ (chi tiết từ). `wordId` để làm mới đúng trang từ. */
export function useSetCardSuspension() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ cardId, suspended }: { cardId: string; suspended: boolean; wordId: string }) =>
      setSrsCardSuspension(cardId, suspended),
    onSuccess: (_card, vars) => {
      void queryClient.invalidateQueries({ queryKey: SRS_KEYS.summary })
      void queryClient.invalidateQueries({ queryKey: DICTIONARY_KEYS.word(vars.wordId) })
    },
  })
}

/**
 * Ghi `summary` mới nhận từ response chấm thẻ/hàng đợi vào cache (không cần gọi lại GET).
 * Tham chiếu PHẢI ổn định (`useCallback`): `ReviewSessionPage` đặt hàm này vào deps của `loadInitial` ⇒ effect tải
 * hàng đợi — hàm mới mỗi render từng gây vòng lặp gọi `/srs/queue` vô hạn ("Maximum update depth exceeded").
 */
export function useApplySrsSummary() {
  const queryClient = useQueryClient()
  return useCallback(
    (summary: SrsSummary | undefined | null) => {
      if (summary) queryClient.setQueryData(SRS_KEYS.summary, summary)
    },
    [queryClient],
  )
}
