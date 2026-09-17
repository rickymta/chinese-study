import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { isApiError } from '@af/api'
import { getCharacter, getWord, searchWords, SEARCH_PAGE_SIZE } from './api'
import type { SearchParams } from './types'

export const DICTIONARY_KEYS = {
  all: ['dictionary'] as const,
  search: (p: Required<Pick<SearchParams, 'q' | 'page' | 'pageSize'>> & { hsk: number | null }) =>
    ['dictionary', 'search', p] as const,
  word: (id: string) => ['dictionary', 'word', id] as const,
  character: (hanzi: string) => ['dictionary', 'character', hanzi] as const,
}

const FIVE_MINUTES = 5 * 60 * 1000

/** 503 (học liệu chưa nạp) và 4xx là câu trả lời thật — không retry; lỗi mạng thử lại 1 lần. */
const retryUnlessFinal = (failureCount: number, err: unknown) => {
  if (isApiError(err) && err.status !== undefined && (err.status === 503 || err.status < 500)) return false
  return failureCount < 1
}

/**
 * Tìm từ theo URL (`q`, `hsk`, `page`). `keepPreviousData`: đổi trang/gõ tiếp vẫn giữ danh sách cũ mờ đi thay vì
 * nháy skeleton; học liệu tĩnh nên giữ 5 phút.
 */
export function useWordSearch(params: SearchParams) {
  const q = params.q?.trim() ?? ''
  const page = params.page ?? 1
  const pageSize = params.pageSize ?? SEARCH_PAGE_SIZE
  const hsk = params.hsk ?? null
  return useQuery({
    queryKey: DICTIONARY_KEYS.search({ q, page, pageSize, hsk }),
    queryFn: ({ signal }) => searchWords({ q, page, pageSize, hsk: hsk ?? undefined }, signal),
    placeholderData: keepPreviousData,
    staleTime: FIVE_MINUTES,
    retry: retryUnlessFinal,
  })
}

export function useWord(id: string | undefined) {
  return useQuery({
    queryKey: DICTIONARY_KEYS.word(id ?? ''),
    queryFn: ({ signal }) => getWord(id!, signal),
    enabled: !!id,
    staleTime: FIVE_MINUTES,
    retry: retryUnlessFinal,
  })
}

export function useCharacter(hanzi: string | undefined) {
  return useQuery({
    queryKey: DICTIONARY_KEYS.character(hanzi ?? ''),
    queryFn: ({ signal }) => getCharacter(hanzi!, signal),
    enabled: !!hanzi,
    staleTime: FIVE_MINUTES,
    retry: retryUnlessFinal,
  })
}
