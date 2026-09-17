import { createContext, useCallback, useContext, useMemo, type ReactNode } from 'react'
import { useSpeech, type UseSpeechResult } from '@af/ui'
import { useTtsRate } from './useTtsRate'

export interface ChineseSpeechValue extends UseSpeechResult {
  /** Tốc độ đọc người học chọn (0,5–1,2) — F7: `learner_settings.tts_rate`, cache localStorage. */
  rate: number
  setRate: (rate: number) => void
  /** Cài đặt "Tự đọc khi hiện thẻ" (F7, mặc định bật). */
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

/**
 * Một `useSpeech('zh')` dùng chung cho cả trang (hợp đồng §5.3.D: "truyền xuống") — đặt ở ngữ cảnh để các nút
 * nghe/drawer/bài luyện không phải khoan prop. F6/F7 bọc trang của mình bằng provider này.
 */
export function ChineseSpeechProvider({ children }: { children: ReactNode }) {
  const speech = useSpeech('zh')
  const { rate, setRate, autoPlayAudio } = useTtsRate()
  const { speak, status } = speech

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
