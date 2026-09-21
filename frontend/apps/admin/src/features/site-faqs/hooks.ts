import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFaq, deleteFaq, getFaqs, reorderFaqs, updateFaq } from './api'
import type { FaqDto, FaqInput } from './types'

export const FAQS_KEY = ['cms', 'faqs'] as const

export function useFaqs() {
  return useQuery({
    queryKey: FAQS_KEY,
    queryFn: ({ signal }) => getFaqs(signal),
    staleTime: 0,
  })
}

function useInvalidateFaqs() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: FAQS_KEY })
}

export function useCreateFaq() {
  const invalidate = useInvalidateFaqs()
  return useMutation({
    mutationFn: (body: FaqInput) => createFaq(body),
    onSuccess: () => void invalidate(),
  })
}

export function useUpdateFaq() {
  const invalidate = useInvalidateFaqs()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: FaqInput & { version: string } }) => updateFaq(id, body),
    onSuccess: () => void invalidate(),
  })
}

export function useDeleteFaq() {
  const invalidate = useInvalidateFaqs()
  return useMutation({
    mutationFn: (id: string) => deleteFaq(id),
    onSuccess: () => void invalidate(),
  })
}

/**
 * Sắp thứ tự TRONG một nhóm: cập nhật lạc quan cache; lỗi (422 ORDER_MISMATCH — có người vừa thêm/xoá trong nhóm)
 * ⇒ hoàn cache + invalidate.
 */
export function useReorderFaqs() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ groupKey, ordered }: { groupKey: string; ordered: FaqDto[] }) => reorderFaqs(groupKey, ordered.map((f) => f.id)),
    onMutate: async ({ groupKey, ordered }) => {
      await queryClient.cancelQueries({ queryKey: FAQS_KEY })
      const previous = queryClient.getQueryData<FaqDto[]>(FAQS_KEY)
      if (previous) {
        const reindexed = new Map(ordered.map((f, i) => [f.id, i + 1]))
        queryClient.setQueryData<FaqDto[]>(
          FAQS_KEY,
          previous
            .map((f) => (f.groupKey === groupKey && reindexed.has(f.id) ? { ...f, sortOrder: reindexed.get(f.id)! } : f))
            .sort((a, b) => a.groupKey.localeCompare(b.groupKey) || a.sortOrder - b.sortOrder),
        )
      }
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) queryClient.setQueryData(FAQS_KEY, ctx.previous)
    },
    onSettled: () => void queryClient.invalidateQueries({ queryKey: FAQS_KEY }),
  })
}
