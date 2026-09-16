import { chineseApi } from '@/api/clients'

/** `GET /chinese/api/admin/ping` → 200 (hợp đồng §6.3). */
export interface AdminPing {
  ok: boolean
}

/**
 * Lời gọi canh gác phân quyền: cần `users.manage`. CỐ Ý không đặt `skipErrorRedirect` — đây là GET nên 403
 * (quyền vừa bị thu hồi) phải kéo sang `/403` theo quy tắc "Trang lỗi 4xx thống nhất", đúng như tiêu chí F3.
 */
export async function getAdminPing(signal?: AbortSignal): Promise<AdminPing> {
  const res = await chineseApi.get<AdminPing>('/admin/ping', { signal })
  return res.data
}
