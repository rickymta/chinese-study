/** Hợp đồng §6.2 W3a — ngôn ngữ trên website. */
export type LanguageStatus = 'open' | 'coming_soon' | 'hidden'

export const LANGUAGE_STATUSES: readonly LanguageStatus[] = ['open', 'coming_soon', 'hidden']

export interface LanguageDto {
  id: string
  code: string
  name: string
  nativeName: string
  tagline: string
  descriptionMarkdown: string
  status: LanguageStatus
  appUrl: string | null
  accentColor: string | null
  /** W3a luôn null — W4 (media) mới có. */
  coverMedia: unknown | null
  sortOrder: number
  /** `xmin` dạng chuỗi số — PUT gửi lại nguyên để kiểm concurrency. */
  version: string
  updatedAt: string
}

/** Thân POST/PUT (PUT không có `code`, thêm `version`). */
export interface LanguageInput {
  name: string
  nativeName: string
  tagline: string
  descriptionMarkdown: string
  status: LanguageStatus
  appUrl: string | null
  accentColor: string | null
}

/** Mã BCP-47 để đặt `lang` cho `nativeName` (chữ Hán cần `zh-CN` + phông CJK). Mã lạ ⇒ không đặt. */
export const LANGUAGE_CODE_TO_BCP47: Record<string, string> = {
  chinese: 'zh-CN',
  english: 'en',
  japanese: 'ja',
  korean: 'ko',
  vietnamese: 'vi',
}
