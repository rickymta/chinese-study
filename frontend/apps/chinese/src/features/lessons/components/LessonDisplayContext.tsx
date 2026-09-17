import { createContext, useContext, type ReactNode } from 'react'

export interface LessonDisplayValue {
  /** Hiện pinyin (ruby trên chữ Hán nội dòng, dòng pinyin ở hội thoại/ví dụ). */
  showPinyin: boolean
  /** Hiện nghĩa tiếng Việt ở hội thoại/ví dụ. */
  showVi: boolean
}

const DEFAULT_DISPLAY: LessonDisplayValue = { showPinyin: true, showVi: true }

const LessonDisplayContext = createContext<LessonDisplayValue>(DEFAULT_DISPLAY)

/**
 * Công tắc hiển thị cho cây khối nội dung (tab Nội dung, quiz, xem trước F10). Không bọc ⇒ mặc định bật cả hai —
 * `LessonContent` dùng lại ở nơi khác (F10) không cần lo prop.
 */
export function LessonDisplayProvider({ value, children }: { value: LessonDisplayValue; children: ReactNode }) {
  return <LessonDisplayContext.Provider value={value}>{children}</LessonDisplayContext.Provider>
}

export function useLessonDisplay(): LessonDisplayValue {
  return useContext(LessonDisplayContext)
}
