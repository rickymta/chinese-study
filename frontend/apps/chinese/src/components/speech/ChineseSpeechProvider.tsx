import type { ReactNode } from 'react'
import { ChineseSpeechProvider as KitChineseSpeechProvider } from '@af/chinese-kit'
import { useTtsRate } from './useTtsRate'

/**
 * Provider đọc tiếng Trung CỦA APP HỌC VIÊN: bọc provider của `@af/chinese-kit` và tiêm tốc độ đọc + "tự đọc khi
 * hiện thẻ" từ cài đặt người học (`useTtsRate` — `learner_settings.tts_rate`, cache localStorage, ghi máy chủ sau
 * 600 ms). Kit không biết API cài đặt (W12), nên phần gọi máy chủ nằm ở đây. Các trang vẫn bọc
 * `<ChineseSpeechProvider>` như trước; hook `useChineseSpeech`/nút `SpeakButton` import từ `@af/chinese-kit`.
 */
export function ChineseSpeechProvider({ children }: { children: ReactNode }) {
  const { rate, setRate, autoPlayAudio } = useTtsRate()
  return (
    <KitChineseSpeechProvider rate={rate} onRateChange={setRate} autoPlayAudio={autoPlayAudio}>
      {children}
    </KitChineseSpeechProvider>
  )
}
