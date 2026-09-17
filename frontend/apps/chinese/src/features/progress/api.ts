import { chineseApi } from '@/api/clients'
import type { ProgressOverview } from './types'

/**
 * `GET /api/progress/overview` (§6.4, cần `study.use`) — tổng quan tiến độ theo múi giờ hồ sơ.
 * Trang chủ là điểm vào của app: `skipErrorRedirect` để lỗi 403 (tài khoản chỉ có quyền quản trị) không đẩy người
 * dùng sang `/403` mà trang tự hiện Alert giải thích; trường mảng/khối vắng được chuẩn hoá để component không phải
 * phòng `undefined` ở mọi chỗ.
 */
export async function getProgressOverview(signal?: AbortSignal): Promise<ProgressOverview> {
  const res = await chineseApi.get<ProgressOverview>('/progress/overview', { signal, skipErrorRedirect: true })
  const data = res.data
  return {
    ...data,
    activity: data.activity ?? [],
    tone: data.tone ? { ...data.tone, recommendedFocus: data.tone.recommendedFocus ?? [] } : data.tone,
  }
}
