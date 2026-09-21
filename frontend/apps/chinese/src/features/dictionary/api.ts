import { chineseApi } from '@/api/clients'
import type { CharacterDetail, SearchParams, SearchResponse, WordDetail } from '@af/chinese-kit'

// Lời gọi GET ở đây KHÔNG đặt `skipErrorRedirect`: 403 (mất `study.use`) ⇒ `/403`, 404 (id/chữ không tồn tại) ⇒ `/404`
// theo quy tắc "Trang lỗi 4xx thống nhất". 503 `CONTENT_UNAVAILABLE` không bị điều hướng ⇒ màn hình tự hiện Alert.

export const SEARCH_PAGE_SIZE = 20

/** `GET /api/dictionary/search?q=&hsk=&page=&pageSize=` — `q` rỗng ⇒ liệt kê theo lộ trình (`browse`). */
export async function searchWords(params: SearchParams, signal?: AbortSignal): Promise<SearchResponse> {
  const query: Record<string, string | number> = {
    page: params.page ?? 1,
    pageSize: params.pageSize ?? SEARCH_PAGE_SIZE,
  }
  const q = params.q?.trim()
  if (q) query.q = q
  if (params.hsk) query.hsk = params.hsk
  const res = await chineseApi.get<SearchResponse>('/dictionary/search', { params: query, signal })
  return { ...res.data, items: res.data.items ?? [], totalCount: res.data.totalCount ?? 0 }
}

/** `GET /api/dictionary/words/{id}` — 404 ⇒ client tự điều hướng `/404`. */
export async function getWord(id: string, signal?: AbortSignal): Promise<WordDetail> {
  const res = await chineseApi.get<WordDetail>(`/dictionary/words/${encodeURIComponent(id)}`, { signal })
  return res.data
}

/** `GET /api/dictionary/characters/{hanzi}` — chữ Hán phải URL-encode (`爱` ⇒ `%E7%88%B1`). */
export async function getCharacter(hanzi: string, signal?: AbortSignal): Promise<CharacterDetail> {
  const res = await chineseApi.get<CharacterDetail>(`/dictionary/characters/${encodeURIComponent(hanzi)}`, { signal })
  return res.data
}
