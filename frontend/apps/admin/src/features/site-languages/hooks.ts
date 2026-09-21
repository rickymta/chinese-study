import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createLanguage, deleteLanguage, getLanguages, reorderLanguages, updateLanguage } from './api'
import type { LanguageDto, LanguageInput } from './types'

export const LANGUAGES_KEY = ['cms', 'languages'] as const

export function useLanguages() {
  return useQuery({
    queryKey: LANGUAGES_KEY,
    queryFn: ({ signal }) => getLanguages(signal),
    staleTime: 0,
  })
}

function useInvalidateLanguages() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: LANGUAGES_KEY })
}

export function useCreateLanguage() {
  const invalidate = useInvalidateLanguages()
  return useMutation({
    mutationFn: (body: LanguageInput & { code: string }) => createLanguage(body),
    onSuccess: () => void invalidate(),
  })
}

export function useUpdateLanguage() {
  const invalidate = useInvalidateLanguages()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: LanguageInput & { version: string } }) => updateLanguage(id, body),
    onSuccess: () => void invalidate(),
  })
}

export function useDeleteLanguage() {
  const invalidate = useInvalidateLanguages()
  return useMutation({
    mutationFn: (id: string) => deleteLanguage(id),
    onSuccess: () => void invalidate(),
  })
}

/**
 * Sắp thứ tự: cập nhật lạc quan cache để nút lên/xuống phản hồi tức thì; lỗi (422 ORDER_MISMATCH — có người vừa
 * thêm/xoá) ⇒ hoàn cache + invalidate để lấy danh sách thật.
 */
export function useReorderLanguages() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (ordered: LanguageDto[]) => reorderLanguages(ordered.map((l) => l.id)),
    onMutate: async (ordered) => {
      await queryClient.cancelQueries({ queryKey: LANGUAGES_KEY })
      const previous = queryClient.getQueryData<LanguageDto[]>(LANGUAGES_KEY)
      queryClient.setQueryData<LanguageDto[]>(
        LANGUAGES_KEY,
        ordered.map((l, i) => ({ ...l, sortOrder: i + 1 })),
      )
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) queryClient.setQueryData(LANGUAGES_KEY, ctx.previous)
    },
    onSettled: () => void queryClient.invalidateQueries({ queryKey: LANGUAGES_KEY }),
  })
}
