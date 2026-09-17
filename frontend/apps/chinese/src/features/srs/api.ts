import { chineseApi } from '@/api/clients'
import type {
  AddCardsResponse,
  LearningSettings,
  LearningSettingsResponse,
  ReviewRequest,
  ReviewResponse,
  SrsCardStatus,
  SrsQueueResponse,
  SrsSummary,
} from './types'

// Hợp đồng §6.2. Lời gọi GHI giữ lỗi để màn hình báo tại chỗ (createApiClient chỉ điều hướng GET 403/404).
// `summary` chạy NỀN ở mọi trang (huy hiệu menu) ⇒ `skipErrorRedirect` để lỗi không làm mất trang đang xem.

export const QUEUE_LIMIT_DEFAULT = 20

/** `GET /api/srs/summary` — số thẻ đến hạn/mới/đã ôn hôm nay theo múi giờ người học. */
export async function getSrsSummary(signal?: AbortSignal): Promise<SrsSummary> {
  const res = await chineseApi.get<SrsSummary>('/srs/summary', { signal, skipErrorRedirect: true })
  return res.data
}

/** `GET /api/srs/queue?limit=` (1..50) — hàng đợi theo R7-7 kèm `summary`. 403 (mất `study.use`) ⇒ `/403`. */
export async function getSrsQueue(limit = QUEUE_LIMIT_DEFAULT, signal?: AbortSignal): Promise<SrsQueueResponse> {
  const res = await chineseApi.get<SrsQueueResponse>('/srs/queue', { params: { limit }, signal })
  return { ...res.data, cards: res.data.cards ?? [] }
}

/**
 * `POST /api/srs/cards/{cardId}/reviews` — idempotent theo `clientReviewId` (R7-8).
 * Lỗi: 400 VALIDATION · 404 · 409 CLIENT_REVIEW_ID_CONFLICT · 422 CARD_SUSPENDED | NEW_CARD_LIMIT_REACHED.
 */
export async function postReview(cardId: string, body: ReviewRequest): Promise<ReviewResponse> {
  const res = await chineseApi.post<ReviewResponse>(`/srs/cards/${encodeURIComponent(cardId)}/reviews`, body)
  return res.data
}

/** `POST /api/srs/cards` — thêm thẻ `source='manual'` (201). 422 UNKNOWN_WORD (`details.wordIds`). */
export async function addSrsCards(wordIds: string[]): Promise<AddCardsResponse> {
  const res = await chineseApi.post<AddCardsResponse>('/srs/cards', { wordIds })
  return res.data
}

/** `PUT /api/srs/cards/{cardId}/suspension` `{ suspended }` ⇒ 200 thẻ trần (`SrsCardStatus`) sau cập nhật · 404. */
export async function setSrsCardSuspension(cardId: string, suspended: boolean): Promise<SrsCardStatus> {
  const res = await chineseApi.put<SrsCardStatus>(`/srs/cards/${encodeURIComponent(cardId)}/suspension`, { suspended })
  return res.data
}

/** `GET /api/me/learning-settings` — chưa lưu ⇒ mặc định + `isDefault: true`. Chạy nền ở nhiều trang ⇒ không điều hướng. */
export async function getLearningSettings(signal?: AbortSignal): Promise<LearningSettingsResponse> {
  const res = await chineseApi.get<LearningSettingsResponse>('/me/learning-settings', { signal, skipErrorRedirect: true })
  return res.data
}

/** `PUT /api/me/learning-settings` — body đủ 5 trường; 400 VALIDATION `details` theo tên trường. */
export async function putLearningSettings(body: LearningSettings): Promise<LearningSettingsResponse> {
  const res = await chineseApi.put<LearningSettingsResponse>('/me/learning-settings', body)
  return res.data
}
