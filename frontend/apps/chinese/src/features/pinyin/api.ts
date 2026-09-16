import { chineseApi } from '@/api/clients'
import {
  TONE_KEYS,
  type PinyinChart,
  type PinyinGuide,
  type SubmitToneDrillRequest,
  type SubmitToneDrillResponse,
  type ToneAccuracy,
  type ToneKey,
  type ToneStats,
} from './types'

// Lời gọi `GET` ở đây KHÔNG đặt `skipErrorRedirect`: 403 (mất `study.use`) phải kéo sang `/403` theo quy tắc
// "Trang lỗi 4xx thống nhất". 503 `CONTENT_UNAVAILABLE` không bị điều hướng ⇒ màn hình tự hiện Alert.

/** `GET /api/pinyin/chart` — có ETag + `Cache-Control: private, max-age=3600`; trình duyệt tự gửi `If-None-Match`. */
export async function getChart(signal?: AbortSignal): Promise<PinyinChart> {
  const res = await chineseApi.get<PinyinChart>('/pinyin/chart', { signal })
  return res.data
}

/** `GET /api/pinyin/guide` — chủ đề đã sắp theo `order`. */
export async function getGuide(signal?: AbortSignal): Promise<PinyinGuide> {
  const res = await chineseApi.get<PinyinGuide>('/pinyin/guide', { signal })
  return { ...res.data, topics: [...(res.data.topics ?? [])].sort((a, b) => a.order - b.order) }
}

/** `POST /api/pinyin/tone-drills` — 201 tạo mới, 200 khi `clientSessionId` đã nộp (cùng body). */
export async function submitToneDrill(body: SubmitToneDrillRequest): Promise<SubmitToneDrillResponse> {
  const res = await chineseApi.post<SubmitToneDrillResponse>('/pinyin/tone-drills', body)
  return res.data
}

/** Phản hồi thô có thể THIẾU khoá `null` (RK41) ⇒ bù về `null` cho TS nhất quán. */
type RawToneStats = Partial<Omit<ToneStats, 'byTone'>> & { byTone?: Partial<Record<ToneKey, Partial<ToneAccuracy>>> }

export function normalizeToneStats(raw: RawToneStats | undefined): ToneStats {
  const byTone = {} as Record<ToneKey, ToneAccuracy>
  for (const k of TONE_KEYS) {
    const t = raw?.byTone?.[k]
    byTone[k] = { total: t?.total ?? 0, correct: t?.correct ?? 0, accuracy: typeof t?.accuracy === 'number' ? t.accuracy : null }
  }
  return {
    totalAnswered: raw?.totalAnswered ?? 0,
    sessionsCount: raw?.sessionsCount ?? 0,
    lastSessionAt: raw?.lastSessionAt ?? null,
    windowSize: raw?.windowSize ?? 200,
    accuracy: typeof raw?.accuracy === 'number' ? raw.accuracy : null,
    byTone,
    confusions: Array.isArray(raw?.confusions) ? raw.confusions : [],
    recommendedFocus: Array.isArray(raw?.recommendedFocus) ? raw.recommendedFocus : [],
    g0Reached: raw?.g0Reached === true,
  }
}

/** `GET /api/pinyin/tone-stats` — không cache. */
export async function getToneStats(signal?: AbortSignal): Promise<ToneStats> {
  const res = await chineseApi.get<RawToneStats>('/pinyin/tone-stats', { signal })
  return normalizeToneStats(res.data)
}
