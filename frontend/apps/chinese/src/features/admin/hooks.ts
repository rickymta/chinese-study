import { useQuery } from '@tanstack/react-query'
import { getAdminPing } from './api'

/** Kiểm tra kết nối vùng quản trị (`GET /admin/ping`). Không retry: 403 là câu trả lời thật, không phải lỗi thoáng qua. */
export function useAdminPing() {
  return useQuery({
    queryKey: ['admin', 'ping'],
    queryFn: ({ signal }) => getAdminPing(signal),
    retry: false,
    staleTime: 0,
  })
}
