import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { PROGRESS_KEYS } from '@/features/progress/hooks'
import { getChart, getGuide, getToneStats, submitToneDrill } from './api'
import type { SubmitToneDrillRequest } from './types'

export const PINYIN_KEYS = {
  chart: ['pinyin', 'chart'] as const,
  guide: ['pinyin', 'guide'] as const,
  toneStats: ['pinyin', 'tone-stats'] as const,
}

/** 503 (học liệu chưa sẵn sàng) là câu trả lời thật, không phải lỗi thoáng qua — không retry. */
const retryUnlessUnavailable = (failureCount: number, err: unknown) =>
  !(isApiError(err) && err.status === 503) && failureCount < 1

/** Bảng pinyin: học liệu tĩnh (ETag phía server) ⇒ giữ vô hạn trong phiên. */
export function usePinyinChart() {
  return useQuery({
    queryKey: PINYIN_KEYS.chart,
    queryFn: ({ signal }) => getChart(signal),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: retryUnlessUnavailable,
  })
}

export function usePinyinGuide() {
  return useQuery({
    queryKey: PINYIN_KEYS.guide,
    queryFn: ({ signal }) => getGuide(signal),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: retryUnlessUnavailable,
  })
}

export function useToneStats() {
  return useQuery({
    queryKey: PINYIN_KEYS.toneStats,
    queryFn: ({ signal }) => getToneStats(signal),
    staleTime: 0,
    retry: retryUnlessUnavailable,
  })
}

/** Nộp phiên luyện thanh; thành công ⇒ thống kê phải tải lại + tổng quan trang chủ (F11: chuỗi ngày, thanh yếu). */
export function useSubmitToneDrill() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SubmitToneDrillRequest) => submitToneDrill(body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PINYIN_KEYS.toneStats })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_KEYS.overview })
    },
  })
}
