import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { useSpeech, type UseSpeechResult } from '@af/ui'
import { clampTtsRate, TTS_RATE_DEFAULT } from './ttsRate'

export interface ChineseSpeechValue extends UseSpeechResult {
  /** Tốc độ đọc đang áp dụng (0,5–1,2). */
  rate: number
  setRate: (rate: number) => void
  /** Cài đặt "Tự đọc khi hiện thẻ" (mặc định bật). */
  autoPlayAudio: boolean
  /**
   * Đọc chữ Hán với giọng + tốc độ đã chọn (`opts.rate` ghi đè — nút "Nghe thử" ở tab Học tập đọc theo giá trị
   * đang kéo, chưa lưu). ⚠️ Chỉ gọi trong handler thao tác người dùng (iOS Safari).
   */
  speakZh: (text: string, opts?: { rate?: number }) => Promise<void>
  /** `true` khi có thể phát (status `ready`). */
  canSpeak: boolean
}

const ChineseSpeechContext = createContext<ChineseSpeechValue | null>(null)

export interface ChineseSpeechProviderProps {
  /**
   * Tốc độ đọc do bên ngoài quản lý (W12: app học viên tiêm từ `useTtsRate` — nguồn sự thật
   * `learner_settings.tts_rate`). Bỏ trống ⇒ provider tự giữ state cục bộ, khởi đầu `TTS_RATE_DEFAULT`
   * (đủ cho admin/xem trước — nơi không có cài đặt người học).
   */
  rate?: number
  /** Nhận giá trị mới khi người dùng đổi tốc độ (chỉ có ý nghĩa khi `rate` được truyền). */
  onRateChange?: (rate: number) => void
  /** "Tự đọc khi hiện thẻ"; bỏ trống ⇒ `true`. */
  autoPlayAudio?: boolean
  children: ReactNode
}

/**
 * Một `useSpeech('zh')` dùng chung cho cả trang (hợp đồng MVP §5.3.D: "truyền xuống") — đặt ở ngữ cảnh để các nút
 * nghe/drawer/bài luyện không phải khoan prop. Kit KHÔNG biết API cài đặt của app nào: tốc độ đọc đi vào bằng props
 * (điều khiển được) hoặc state cục bộ (không điều khiển) — quyết định W12 để `apps/chinese` và admin dùng chung.
 */
export function ChineseSpeechProvider({
  rate: rateProp,
  onRateChange,
  autoPlayAudio = true,
  children,
}: ChineseSpeechProviderProps) {
  const speech = useSpeech('zh')
  const [localRate, setLocalRate] = useState<number>(TTS_RATE_DEFAULT)
  const controlled = rateProp !== undefined
  const rate = controlled ? clampTtsRate(rateProp) : localRate
  const { speak, status } = speech

  const setRate = useCallback(
    (next: number) => {
      const v = clampTtsRate(next)
      if (controlled) onRateChange?.(v)
      else setLocalRate(v)
    },
    [controlled, onRateChange],
  )

  const speakZh = useCallback(
    (text: string, opts?: { rate?: number }) => speak(text, { rate: opts?.rate ?? rate }),
    [speak, rate],
  )

  const value = useMemo<ChineseSpeechValue>(
    () => ({ ...speech, rate, setRate, autoPlayAudio, speakZh, canSpeak: status === 'ready' }),
    [speech, rate, setRate, autoPlayAudio, speakZh, status],
  )
  return <ChineseSpeechContext.Provider value={value}>{children}</ChineseSpeechContext.Provider>
}

export function useChineseSpeech(): ChineseSpeechValue {
  const ctx = useContext(ChineseSpeechContext)
  if (!ctx) throw new Error('useChineseSpeech phải nằm trong <ChineseSpeechProvider>.')
  return ctx
}
