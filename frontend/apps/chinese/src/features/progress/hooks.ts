import { useQuery } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { getProgressOverview } from './api'

/**
 * Khoá query F11. Các feature ghi (F7 ôn thẻ, F8 viết, F9 quiz, F5 luyện thanh) invalidate `PROGRESS_KEYS.overview`
 * sau thao tác thành công — `features/lessons/hooks.ts` re-export `PROGRESS_OVERVIEW_KEY` để giữ import cũ.
 */
export const PROGRESS_KEYS = {
  all: ['progress'] as const,
  overview: ['progress', 'overview'] as const,
}

const THIRTY_SECONDS = 30 * 1000

/** 4xx và 503 là câu trả lời thật — không retry; lỗi mạng/5xx khác thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && (err.status === 503 || err.status < 500)) return false
  return failureCount < 1
}

/**
 * Tổng quan tiến độ (trang chủ). `refetchOnWindowFocus`: người học mở lại tab sau khi ôn ở máy khác hoặc qua
 * nửa đêm (múi giờ hồ sơ) ⇒ "hôm nay" và chuỗi ngày đã khác. `enabled` do trang quyết định theo quyền `study.use`.
 */
export function useProgressOverview(enabled = true) {
  return useQuery({
    queryKey: PROGRESS_KEYS.overview,
    queryFn: ({ signal }) => getProgressOverview(signal),
    enabled,
    staleTime: THIRTY_SECONDS,
    refetchOnWindowFocus: true,
    retry: retryUnlessFinal,
  })
}
