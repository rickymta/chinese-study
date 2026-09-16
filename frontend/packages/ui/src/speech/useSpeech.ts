import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { cancelSpeech, isSpeechSupported, listVoices, pickVoice, speak as speakRaw } from './speech'

export type SpeechStatus = 'loading' | 'ready' | 'unsupported' | 'no-voice'

export interface UseSpeechOptions {
  /** Khoá localStorage lưu giọng đã chọn. Mặc định `af.speech.voice.<langPrefix>`. */
  storageKey?: string
}

export interface UseSpeechResult {
  status: SpeechStatus
  voices: SpeechSynthesisVoice[]
  /** Giọng đang dùng (`null` khi chưa có). */
  voice: SpeechSynthesisVoice | null
  setVoiceUri: (uri: string) => void
  /**
   * Đọc văn bản bằng giọng đang chọn. ⚠️ iOS Safari chỉ phát khi hàm này được gọi TRONG handler thao tác người dùng
   * (onClick/onKeyDown) — không gọi từ `useEffect`/`setTimeout`; ở đó phải để người dùng bấm nút "Nghe".
   */
  speak: (text: string, opts?: { rate?: number; pitch?: number }) => Promise<void>
  cancel: () => void
  speaking: boolean
}

const readStored = (key: string): string | null => {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}
const writeStored = (key: string, value: string) => {
  try {
    localStorage.setItem(key, value)
  } catch {
    /* Safari chế độ riêng tư / bộ nhớ đầy — bỏ qua, chỉ mất ghi nhớ giọng */
  }
}

/**
 * Hook TTS dùng chung (hợp đồng F5 §5.3.A): liệt kê giọng theo tiền tố ngôn ngữ, nhớ giọng người dùng chọn,
 * báo `unsupported`/`no-voice` để màn hình hiện hướng dẫn cài giọng. Huỷ đọc khi unmount.
 */
export function useSpeech(langPrefix: string, opts?: UseSpeechOptions): UseSpeechResult {
  const storageKey = opts?.storageKey ?? `af.speech.voice.${langPrefix}`
  const supported = isSpeechSupported()
  const [voices, setVoices] = useState<SpeechSynthesisVoice[]>([])
  const [loaded, setLoaded] = useState(false)
  const [voiceUri, setVoiceUriState] = useState<string | null>(() => readStored(storageKey))
  const [speaking, setSpeaking] = useState(false)
  const mounted = useRef(true)

  useEffect(() => {
    mounted.current = true
    if (!supported) {
      setLoaded(true)
      return
    }
    let cancelled = false
    const load = async () => {
      const list = await listVoices(langPrefix)
      if (cancelled || !mounted.current) return
      setVoices(list)
      setLoaded(true)
    }
    void load()
    // Một số trình duyệt bổ sung giọng sau (cài thêm gói giọng, Edge tải giọng online) ⇒ nghe tiếp sự kiện.
    const onChanged = () => void load()
    window.speechSynthesis.addEventListener('voiceschanged', onChanged)
    return () => {
      cancelled = true
      mounted.current = false
      window.speechSynthesis.removeEventListener('voiceschanged', onChanged)
      cancelSpeech()
    }
  }, [langPrefix, supported])

  const voice = useMemo(() => pickVoice(voices, voiceUri), [voices, voiceUri])

  const status: SpeechStatus = !supported ? 'unsupported' : !loaded ? 'loading' : voices.length === 0 ? 'no-voice' : 'ready'

  const setVoiceUri = useCallback(
    (uri: string) => {
      setVoiceUriState(uri)
      writeStored(storageKey, uri)
    },
    [storageKey],
  )

  const speak = useCallback(
    async (text: string, o?: { rate?: number; pitch?: number }) => {
      if (!supported) return
      setSpeaking(true)
      try {
        // Không có giọng khớp vẫn thử với `lang` — trình duyệt có thể tự chọn giọng phù hợp (Edge online).
        await speakRaw(text, { lang: voice?.lang ?? `${langPrefix}-CN`, rate: o?.rate, pitch: o?.pitch, voice })
      } finally {
        if (mounted.current) setSpeaking(false)
      }
    },
    [supported, voice, langPrefix],
  )

  const cancel = useCallback(() => {
    cancelSpeech()
    setSpeaking(false)
  }, [])

  return { status, voices, voice, setVoiceUri, speak, cancel, speaking }
}
