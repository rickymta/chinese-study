import { useCallback, useState } from 'react'

// Công tắc hiển thị của tab Nội dung (hợp đồng §5.3.1): lưu localStorage để bật/tắt giữ qua các bài; mặc định BẬT.

export type DisplayPrefKey = 'showPinyin' | 'showVi'

const STORAGE_KEYS: Record<DisplayPrefKey, string> = {
  showPinyin: 'af.chinese.lesson.showPinyin',
  showVi: 'af.chinese.lesson.showVi',
}

export function readDisplayPref(key: DisplayPrefKey): boolean {
  try {
    const raw = localStorage.getItem(STORAGE_KEYS[key])
    return raw === null ? true : raw !== '0'
  } catch {
    return true
  }
}

export function writeDisplayPref(key: DisplayPrefKey, value: boolean): void {
  try {
    localStorage.setItem(STORAGE_KEYS[key], value ? '1' : '0')
  } catch {
    /* Safari riêng tư / bộ nhớ đầy — chỉ mất ghi nhớ công tắc */
  }
}

export interface DisplayPrefs {
  showPinyin: boolean
  showVi: boolean
  setShowPinyin: (v: boolean) => void
  setShowVi: (v: boolean) => void
}

/** Trạng thái hai công tắc "Pinyin" / "Nghĩa tiếng Việt", đồng bộ localStorage. */
export function useDisplayPrefs(): DisplayPrefs {
  const [showPinyin, setPinyinState] = useState(() => readDisplayPref('showPinyin'))
  const [showVi, setViState] = useState(() => readDisplayPref('showVi'))
  const setShowPinyin = useCallback((v: boolean) => {
    setPinyinState(v)
    writeDisplayPref('showPinyin', v)
  }, [])
  const setShowVi = useCallback((v: boolean) => {
    setViState(v)
    writeDisplayPref('showVi', v)
  }, [])
  return { showPinyin, showVi, setShowPinyin, setShowVi }
}
