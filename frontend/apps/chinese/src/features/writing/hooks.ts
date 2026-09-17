import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { PROGRESS_OVERVIEW_KEY } from '@/features/lessons/hooks'
import { getWritingCharacter, getWritingCharacters, getWritingSummary, postWritingAttempt, WRITING_MAX_PAGE_SIZE, WRITING_PAGE_SIZE } from './api'
import { loadManifest } from './lib/charData'
import type { RecordWritingAttemptRequest, RecordWritingAttemptResponse, WritingCharacterDetail, WritingCharacterItem, WritingSet } from './types'

// Khoá query F8. F11 (tổng quan) invalidate `WRITING_KEYS.all` nếu cần làm mới số chữ đã luyện.
export const WRITING_KEYS = {
  all: ['writing'] as const,
  summary: ['writing', 'summary'] as const,
  characters: (set: string, page: number, pageSize: number) => ['writing', 'characters', set, page, pageSize] as const,
  /** Toàn bộ chữ của một bộ (gộp mọi trang) — dùng cho "Chữ tiếp" ở trang luyện. */
  setAll: (set: string) => ['writing', 'set-all', set] as const,
  character: (hanzi: string) => ['writing', 'character', hanzi] as const,
  /** Danh mục dữ liệu nét tĩnh — không thuộc `writing` để invalidate `WRITING_KEYS.all` không tải lại file. */
  manifest: ['hanzi-data', 'manifest'] as const,
}

const ONE_MINUTE = 60 * 1000

/** 4xx và 503 là câu trả lời thật — không retry; lỗi mạng/5xx khác thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && (err.status === 503 || err.status < 500)) return false
  return failureCount < 1
}

/** `index.json` của dữ liệu nét — file tĩnh, tải một lần rồi giữ suốt phiên (R-W9: biết chữ nào mờ). */
export function useHanziDataManifest() {
  return useQuery({
    queryKey: WRITING_KEYS.manifest,
    queryFn: () => loadManifest(),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: 1,
  })
}

/** Danh sách chữ theo bộ, phân trang; `set = null` (tab Bài học chưa chọn bài) ⇒ không gọi. Giữ trang cũ khi lật trang. */
export function useWritingCharacters(set: WritingSet | null, page: number, pageSize = WRITING_PAGE_SIZE) {
  return useQuery({
    queryKey: WRITING_KEYS.characters(set ?? '', page, pageSize),
    queryFn: ({ signal }) => getWritingCharacters(set!, page, pageSize, signal),
    enabled: !!set,
    staleTime: ONE_MINUTE,
    placeholderData: keepPreviousData,
    retry: retryUnlessFinal,
  })
}

/** Trần số trang khi gộp cả bộ (200 × 10 = 2 000 chữ — vượt xa HSK 1–3). */
const SET_ALL_MAX_PAGES = 10

/**
 * Toàn bộ chữ của bộ (gộp trang, `pageSize` tối đa) — trang luyện dùng để tìm "Chữ tiếp" và vị trí `i/n`.
 * Bộ HSK 1 ~300 chữ ⇒ 2 lời gọi; các bộ khác thường 1.
 */
export function useWritingSetAll(set: WritingSet | null) {
  return useQuery({
    queryKey: WRITING_KEYS.setAll(set ?? ''),
    queryFn: async ({ signal }): Promise<WritingCharacterItem[]> => {
      const items: WritingCharacterItem[] = []
      for (let page = 1; page <= SET_ALL_MAX_PAGES; page++) {
        const res = await getWritingCharacters(set!, page, WRITING_MAX_PAGE_SIZE, signal)
        items.push(...res.items)
        if (items.length >= res.totalCount || res.items.length === 0) break
      }
      return items
    },
    enabled: !!set,
    staleTime: ONE_MINUTE,
    retry: retryUnlessFinal,
  })
}

export function useWritingCharacter(hanzi: string | undefined) {
  return useQuery({
    queryKey: WRITING_KEYS.character(hanzi ?? ''),
    queryFn: ({ signal }) => getWritingCharacter(hanzi!, signal),
    enabled: !!hanzi,
    staleTime: ONE_MINUTE,
    retry: retryUnlessFinal,
  })
}

/** Tóm tắt đầu trang `/luyen-viet` (đã luyện / đã thuộc / cần luyện). */
export function useWritingSummary(enabled = true) {
  return useQuery({
    queryKey: WRITING_KEYS.summary,
    queryFn: ({ signal }) => getWritingSummary(signal),
    enabled,
    staleTime: ONE_MINUTE,
    refetchOnWindowFocus: true,
    retry: retryUnlessFinal,
  })
}

/**
 * Ghi một lần viết hoàn tất. Thành công ⇒ ghi `stats` mới thẳng vào cache chi tiết chữ (kết quả hiện ngay), rồi
 * invalidate danh sách bộ/tóm tắt (chấm màu trạng thái đổi) và tổng quan tiến độ (F11). Lỗi giữ nguyên cho trang
 * xử lý ("Gửi lại" cùng `clientAttemptId`).
 */
export function useRecordWritingAttempt() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: RecordWritingAttemptRequest) => postWritingAttempt(body),
    onSuccess: (result: RecordWritingAttemptResponse, body) => {
      queryClient.setQueryData<WritingCharacterDetail>(WRITING_KEYS.character(body.hanzi), (prev) =>
        prev ? { ...prev, stats: result.stats } : prev,
      )
      void queryClient.invalidateQueries({ queryKey: WRITING_KEYS.all, refetchType: 'active' })
      void queryClient.invalidateQueries({ queryKey: PROGRESS_OVERVIEW_KEY })
    },
  })
}
