import { useQuery } from '@tanstack/react-query'
import { chineseApi, identityApi } from '@/api/clients'
import { getSystemInfo } from './api'
import type { SystemService } from './types'

const CLIENTS = { chinese: chineseApi, identity: identityApi } as const

/**
 * Trạng thái một service qua gateway. Tự hỏi lại mỗi 30 giây để chip đổi màu khi backend được bật/tắt mà
 * không cần F5 (RK16: dev chạy 4 tiến trình, dễ quên một cái).
 */
export function useSystemInfo(service: SystemService) {
  return useQuery({
    queryKey: ['system', 'info', service],
    queryFn: ({ signal }) => getSystemInfo(CLIENTS[service], signal),
    retry: false,
    refetchInterval: 30_000,
    staleTime: 10_000,
  })
}
