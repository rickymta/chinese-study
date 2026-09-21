import { useCallback, useEffect, useRef, useState } from 'react'
import { useChineseSpeech } from '../speech/ChineseSpeech'

export interface SpeakQueue {
  /** Đang đọc cả đoạn. */
  playing: boolean
  /** Chỉ số câu đang đọc (`null` khi không đọc) — để tô nền dòng. */
  activeIndex: number | null
  /** Đọc lần lượt; PHẢI gọi trong handler thao tác người dùng (iOS). Bấm khi đang đọc ⇒ bên gọi dùng `stop`. */
  playAll: (texts: readonly string[]) => void
  stop: () => void
  canSpeak: boolean
}

/**
 * Đọc lần lượt nhiều câu ("Nghe cả đoạn" ở khối hội thoại): gọi `speakZh` từng câu, chờ xong mới câu kế.
 * Dừng khi bấm "Dừng" hoặc rời trang (cleanup unmount). Lần gọi đầu nằm trong handler nút (iOS mở khoá TTS);
 * các câu sau phát tiếp trong cùng phiên đã mở khoá. `runId` tăng mỗi lượt để lượt cũ (nếu còn chờ) tự thoát.
 */
export function useSpeakQueue(): SpeakQueue {
  const { canSpeak, speakZh, cancel } = useChineseSpeech()
  const [playing, setPlaying] = useState(false)
  const [activeIndex, setActiveIndex] = useState<number | null>(null)
  const runIdRef = useRef(0)

  const stop = useCallback(() => {
    runIdRef.current++
    cancel()
    setPlaying(false)
    setActiveIndex(null)
  }, [cancel])

  const playAll = useCallback(
    (texts: readonly string[]) => {
      if (!canSpeak || texts.length === 0) return
      const runId = ++runIdRef.current
      setPlaying(true)
      void (async () => {
        for (let i = 0; i < texts.length; i++) {
          if (runIdRef.current !== runId) return
          setActiveIndex(i)
          // Lỗi phát một câu (not-allowed, interrupted) không chặn câu kế.
          await speakZh(texts[i]!).catch(() => undefined)
        }
        if (runIdRef.current === runId) {
          setPlaying(false)
          setActiveIndex(null)
        }
      })()
    },
    [canSpeak, speakZh],
  )

  useEffect(() => () => stop(), [stop])

  return { playing, activeIndex, playAll, stop, canSpeak }
}
