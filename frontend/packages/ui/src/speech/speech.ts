// Tiện ích đọc văn bản bằng Web Speech API (SpeechSynthesis) — dùng chung mọi ngôn ngữ (hợp đồng F5 §5.3.A).
// MVP chỉ dùng giọng có sẵn trong trình duyệt/hệ điều hành; không có file âm thanh người thật.
//
// Lưu ý iOS Safari: `speechSynthesis.speak()` chỉ phát khi được gọi TRONG handler thao tác người dùng (bấm/chạm).
// Gọi từ `useEffect`/timer sẽ im lặng, không lỗi. Bên gọi phải giữ quy tắc này (xem `useSpeech`).

export function isSpeechSupported(): boolean {
  return typeof window !== 'undefined' && 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window
}

const normLang = (lang: string) => lang.replace('_', '-').toLowerCase()

/**
 * Giọng tiếng Quảng Đông (`zh-HK`, `zh-yue`, `yue-*`, tên có "Cantonese"/粤語) đọc chữ Hán bằng âm khác hẳn
 * phổ thông ⇒ loại khỏi danh sách chọn cho mọi tiền tố (người học pinyin nghe nhầm mà không biết).
 */
function isCantonese(v: SpeechSynthesisVoice): boolean {
  const lang = normLang(v.lang)
  return lang === 'zh-hk' || lang.startsWith('zh-yue') || lang.startsWith('yue') || /cantonese|粤語|粵語|廣東話|广东话/i.test(v.name)
}

/**
 * Danh sách giọng có `lang` bắt đầu bằng `langPrefix` (vd `'zh'` khớp `zh-CN`, `zh_CN`, `zh-TW`).
 * Chrome nạp giọng bất đồng bộ: lần đầu `getVoices()` rỗng ⇒ chờ `voiceschanged` tới hết `timeoutMs`.
 * Sắp xếp: khớp đúng `<langPrefix>-cn`... (vd `zh-cn`) trước, rồi giọng `Natural`/`Online`, rồi giọng cục bộ.
 */
export function listVoices(langPrefix: string, timeoutMs = 1500): Promise<SpeechSynthesisVoice[]> {
  if (!isSpeechSupported()) return Promise.resolve([])
  const synth = window.speechSynthesis
  const prefix = normLang(langPrefix)

  const filterSort = (voices: SpeechSynthesisVoice[]) => {
    const matched = voices.filter((v) => normLang(v.lang).startsWith(prefix) && !isCantonese(v))
    const score = (v: SpeechSynthesisVoice) => {
      let s = 0
      const lang = normLang(v.lang)
      // Ưu tiên phương ngữ "chuẩn" của tiếng đó: zh ⇒ zh-cn; tiếng khác ⇒ khớp đúng prefix (vd 'ja' ⇒ 'ja-jp' cũng ổn).
      if (lang === `${prefix}-cn` || lang === prefix) s += 100
      if (/natural|online|premium|enhanced/i.test(v.name)) s += 10
      if (v.localService) s += 1
      return s
    }
    return matched.sort((a, b) => score(b) - score(a) || a.name.localeCompare(b.name))
  }

  const now = synth.getVoices()
  if (now.length > 0) return Promise.resolve(filterSort(now))

  return new Promise((resolve) => {
    let done = false
    const finish = () => {
      if (done) return
      done = true
      synth.removeEventListener('voiceschanged', onChanged)
      clearTimeout(timer)
      resolve(filterSort(synth.getVoices()))
    }
    const onChanged = () => finish()
    const timer = setTimeout(finish, timeoutMs)
    synth.addEventListener('voiceschanged', onChanged)
  })
}

/** Chọn giọng theo `voiceURI` người dùng đã lưu; không có/không còn ⇒ giọng đầu danh sách (đã sắp ưu tiên). */
export function pickVoice(voices: SpeechSynthesisVoice[], preferredUri?: string | null): SpeechSynthesisVoice | null {
  if (voices.length === 0) return null
  if (preferredUri) {
    const found = voices.find((v) => v.voiceURI === preferredUri)
    if (found) return found
  }
  return voices[0] ?? null
}

export interface SpeakOptions {
  /** BCP 47 cho utterance (vd `zh-CN`). Không có giọng thì trình duyệt tự chọn theo `lang`. */
  lang: string
  /** 0.1–10, mặc định 1. */
  rate?: number
  /** 0–2, mặc định 1. */
  pitch?: number
  voice?: SpeechSynthesisVoice | null
}

// Chrome có lỗi thu gom rác utterance đang phát ⇒ `onend` không bao giờ gọi. Giữ tham chiếu tới utterance hiện tại.
let currentUtterance: SpeechSynthesisUtterance | null = null

/**
 * Đọc `text`: huỷ lượt đang phát trước, resolve ở `onend`. Bị huỷ/ngắt (`canceled`/`interrupted`) cũng RESOLVE
 * (không phải lỗi của bên gọi); lỗi khác (`not-allowed`, `synthesis-failed`...) ⇒ reject.
 * Có đồng hồ an toàn: một số trình duyệt "kẹt" không bắn `onend` ⇒ tự resolve sau thời gian ước lượng.
 */
export function speak(text: string, { lang, rate = 1, pitch = 1, voice }: SpeakOptions): Promise<void> {
  if (!isSpeechSupported()) return Promise.reject(new Error('Trình duyệt không hỗ trợ đọc văn bản.'))
  const synth = window.speechSynthesis
  synth.cancel()

  return new Promise((resolve, reject) => {
    const u = new SpeechSynthesisUtterance(text)
    u.lang = voice?.lang ?? lang
    u.rate = rate
    u.pitch = pitch
    if (voice) u.voice = voice
    currentUtterance = u

    let settled = false
    const settle = (fn: () => void) => {
      if (settled) return
      settled = true
      clearTimeout(guard)
      if (currentUtterance === u) currentUtterance = null
      fn()
    }
    // Ước lượng: 8 giây + 400 ms mỗi ký tự (chữ Hán một âm tiết mỗi ký tự), chia cho tốc độ.
    const guard = setTimeout(() => settle(resolve), (8000 + text.length * 400) / Math.max(rate, 0.1))

    u.onend = () => settle(resolve)
    u.onerror = (e) => {
      if (e.error === 'canceled' || e.error === 'interrupted') settle(resolve)
      else settle(() => reject(new Error(`Không đọc được (${e.error}).`)))
    }
    synth.speak(u)
  })
}

export function cancelSpeech(): void {
  if (!isSpeechSupported()) return
  window.speechSynthesis.cancel()
  currentUtterance = null
}
