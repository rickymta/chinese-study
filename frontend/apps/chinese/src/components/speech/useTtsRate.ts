import { useCallback, useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { SRS_KEYS, useLearningSettings } from '@/features/srs/hooks'
import { putLearningSettings } from '@/features/srs/api'
import type { LearningSettingsResponse } from '@/features/srs/types'
// W12: giới hạn + hàm kẹp tốc độ dời sang @af/chinese-kit (provider của kit cũng kẹp); hook này chỉ còn phần gọi API.
import { TTS_RATE_DEFAULT, clampTtsRate } from '@af/chinese-kit'

const STORAGE_KEY = 'af.chinese.ttsRate'
/** Gom nhiều lần kéo thanh trượt thành một lần ghi máy chủ. */
const SAVE_DEBOUNCE_MS = 600

function readCachedRate(): number {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    const n = raw === null ? NaN : Number(raw)
    return Number.isFinite(n) ? clampTtsRate(n) : TTS_RATE_DEFAULT
  } catch {
    return TTS_RATE_DEFAULT
  }
}

function writeCachedRate(v: number): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(v))
  } catch {
    /* Safari riêng tư / bộ nhớ đầy — chỉ mất cache, server vẫn giữ */
  }
}

export interface UseTtsRateResult {
  rate: number
  setRate: (rate: number) => void
  /** Tự đọc khi thẻ hiện (cài đặt học, mặc định bật). */
  autoPlayAudio: boolean
  /** Cài đặt học đã tải (`undefined` khi chưa/không tải được — lúc đó dùng cache + mặc định). */
  settings: LearningSettingsResponse | undefined
}

/**
 * Tốc độ đọc TTS của người học (0,5–1,2; mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh).
 * F7: nguồn sự thật là `learner_settings.tts_rate` (`GET/PUT /api/me/learning-settings`); `localStorage` chỉ là
 * cache để lần mở sau có ngay giá trị trước khi server trả lời. `setRate` áp dụng tức thì, ghi máy chủ sau 600 ms
 * (gom thao tác kéo thanh trượt); ghi hỏng thì im lặng — lần lưu sau ở tab "Học tập" sẽ đồng bộ lại.
 */
export function useTtsRate(): UseTtsRateResult {
  const queryClient = useQueryClient()
  const { data: settings } = useLearningSettings()
  const [rate, setRateState] = useState<number>(readCachedRate)
  // Giá trị người dùng vừa chọn mà chưa ghi được lên máy chủ (server chưa tải xong lúc chọn).
  const pendingRef = useRef<number | null>(null)
  const lastServerRateRef = useRef<number | null>(null)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const settingsRef = useRef(settings)
  settingsRef.current = settings

  const persist = useCallback(
    (value: number) => {
      const current = settingsRef.current
      if (!current) {
        pendingRef.current = value
        return
      }
      pendingRef.current = null
      const body = {
        dailyNewCards: current.dailyNewCards,
        dailyReviewLimit: current.dailyReviewLimit,
        desiredRetention: current.desiredRetention,
        ttsRate: value,
        autoPlayAudio: current.autoPlayAudio,
      }
      // Ghi cache query trước để các nơi khác (tab Học tập) thấy ngay; lỗi ⇒ trả lại giá trị máy chủ khi refetch.
      queryClient.setQueryData<LearningSettingsResponse>(SRS_KEYS.learningSettings, { ...current, ttsRate: value, isDefault: false })
      lastServerRateRef.current = value
      putLearningSettings(body)
        .then((saved) => queryClient.setQueryData(SRS_KEYS.learningSettings, saved))
        .catch(() => undefined)
    },
    [queryClient],
  )

  // Server trả lời ⇒ đồng bộ (trừ khi người dùng đã chọn giá trị mới trước đó ⇒ đẩy giá trị đó lên).
  useEffect(() => {
    if (!settings) return
    if (pendingRef.current !== null) {
      persist(pendingRef.current)
      return
    }
    const serverRate = clampTtsRate(settings.ttsRate)
    if (lastServerRateRef.current === serverRate) return
    lastServerRateRef.current = serverRate
    setRateState(serverRate)
    writeCachedRate(serverRate)
  }, [settings, persist])

  useEffect(
    () => () => {
      // Rời trang khi còn hẹn ghi ⇒ ghi ngay (không để mất lựa chọn).
      if (timerRef.current) {
        clearTimeout(timerRef.current)
        timerRef.current = null
        if (pendingRef.current !== null) persist(pendingRef.current)
      }
    },
    [persist],
  )

  const setRate = useCallback(
    (next: number) => {
      const v = clampTtsRate(next)
      setRateState(v)
      writeCachedRate(v)
      pendingRef.current = v
      if (timerRef.current) clearTimeout(timerRef.current)
      timerRef.current = setTimeout(() => {
        timerRef.current = null
        if (pendingRef.current !== null) persist(pendingRef.current)
      }, SAVE_DEBOUNCE_MS)
    },
    [persist],
  )

  return { rate, setRate, autoPlayAudio: settings?.autoPlayAudio ?? true, settings }
}
