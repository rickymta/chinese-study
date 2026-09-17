import type { AxiosInstance } from 'axios'
import type { SystemInfo } from './types'

/**
 * `GET {baseURL}/system/info`. Truyền `skipErrorRedirect` vì đây là lời gọi KIỂM TRA TRẠNG THÁI nền — service
 * chưa chạy (502 từ gateway) hay 404 không được kéo cả trang sang trang lỗi.
 */
export async function getSystemInfo(client: AxiosInstance, signal?: AbortSignal): Promise<SystemInfo> {
  const res = await client.get<SystemInfo>('/system/info', { signal, skipErrorRedirect: true })
  return res.data
}
