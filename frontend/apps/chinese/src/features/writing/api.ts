import { chineseApi } from '@/api/clients'
import type {
  RecordWritingAttemptRequest,
  RecordWritingAttemptResponse,
  WritingCharacterDetail,
  WritingCharactersResponse,
  WritingSummary,
} from './types'

// Hợp đồng F8–F11 §6.2 (trình duyệt gọi `/chinese/api/writing/...`, cần `study.use`).
// Server CHỈ nhận kết quả — dữ liệu nét chữ đọc từ file tĩnh `/hanzi-data/*.json` (R-W1, `lib/charData.ts`).

/** Mặc định 60 chữ/trang (§5.3.2); server cho tối đa 200. */
export const WRITING_PAGE_SIZE = 60
export const WRITING_MAX_PAGE_SIZE = 200

/**
 * `GET /api/writing/characters?set=&page=&pageSize=` — danh sách chữ theo bộ (R-W6).
 * `skipErrorRedirect`: 400 (bộ lạ) / 404 (bài chưa xuất bản ở `set=lesson:<slug>`) báo tại chỗ trong trang
 * luyện viết thay vì nhảy sang trang lỗi chung — người học chỉ cần chọn bộ/bài khác.
 */
export async function getWritingCharacters(
  set: string,
  page = 1,
  pageSize = WRITING_PAGE_SIZE,
  signal?: AbortSignal,
): Promise<WritingCharactersResponse> {
  const res = await chineseApi.get<WritingCharactersResponse>('/writing/characters', {
    params: { set, page, pageSize },
    signal,
    skipErrorRedirect: true,
  })
  return { ...res.data, items: res.data.items ?? [] }
}

/** `GET /api/writing/characters/{hanzi}` — chi tiết chữ + ≤ 5 từ chứa chữ + thống kê của người học. 404 ⇒ trang lỗi chung. */
export async function getWritingCharacter(hanzi: string, signal?: AbortSignal): Promise<WritingCharacterDetail> {
  const res = await chineseApi.get<WritingCharacterDetail>(`/writing/characters/${encodeURIComponent(hanzi)}`, { signal })
  return { ...res.data, words: res.data.words ?? [] }
}

/**
 * `POST /api/writing/attempts` — ghi một lần viết hoàn tất (R-W3). 201 lần đầu, 200 khi gửi lại cùng
 * `clientAttemptId`. Lỗi: 400 · 409 `DUPLICATE_ATTEMPT_ID` · 422 `UNKNOWN_CHARACTER`.
 */
export async function postWritingAttempt(body: RecordWritingAttemptRequest): Promise<RecordWritingAttemptResponse> {
  const res = await chineseApi.post<RecordWritingAttemptResponse>('/writing/attempts', body)
  return res.data
}

/** `GET /api/writing/summary` — đã luyện / đã thuộc / cần luyện / hôm nay. */
export async function getWritingSummary(signal?: AbortSignal): Promise<WritingSummary> {
  const res = await chineseApi.get<WritingSummary>('/writing/summary', { signal })
  return res.data
}
