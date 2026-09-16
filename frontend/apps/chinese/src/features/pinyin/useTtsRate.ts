import { useCallback, useState } from 'react'

const STORAGE_KEY = 'af.chinese.ttsRate'
export const TTS_RATE_DEFAULT = 0.8
export const TTS_RATE_MIN = 0.5
export const TTS_RATE_MAX = 1.2

const clamp = (v: number) => Math.min(TTS_RATE_MAX, Math.max(TTS_RATE_MIN, v))

function readRate(): number {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    const n = raw === null ? NaN : Number(raw)
    return Number.isFinite(n) ? clamp(n) : TTS_RATE_DEFAULT
  } catch {
    return TTS_RATE_DEFAULT
  }
}

/**
 * Tốc độ đọc TTS của người học (0,5–1,2; mặc định 0,8 — chậm hơn tự nhiên để nghe rõ thanh). F5 lưu
 * `localStorage` (D34); F7 chuyển sang `learner_settings`. Mọi thao tác localStorage bọc try/catch (Safari riêng tư).
 */
export function useTtsRate(): [number, (rate: number) => void] {
  const [rate, setRateState] = useState<number>(readRate)
  const setRate = useCallback((next: number) => {
    const v = clamp(Number.isFinite(next) ? next : TTS_RATE_DEFAULT)
    setRateState(v)
    try {
      localStorage.setItem(STORAGE_KEY, String(v))
    } catch {
      /* bỏ qua — chỉ mất ghi nhớ */
    }
  }, [])
  return [rate, setRate]
}
