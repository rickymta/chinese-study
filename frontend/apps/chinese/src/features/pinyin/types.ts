// Kiểu dữ liệu F5 theo hợp đồng chi tiết F4/F5 §6.1 (chinese-backend `/api/pinyin/*`).
// Lưu ý RK41: serializer backend bật `WhenWritingNull` ⇒ trường có thể `null` được khai `?:`/`| null` và chuẩn hoá ở `api.ts`.

/** Thanh 1–4 dùng trong bảng/bài luyện (thanh nhẹ không có chữ minh hoạ — R-L1). */
export type DrillTone = 1 | 2 | 3 | 4
export type ToneKey = '1' | '2' | '3' | '4'
export const DRILL_TONES: readonly DrillTone[] = [1, 2, 3, 4]
export const TONE_KEYS: readonly ToneKey[] = ['1', '2', '3', '4']

export type InitialGroup = 'khong' | 'moi' | 'dau-luoi' | 'cuong-luoi' | 'mat-luoi' | 'dau-luoi-truoc' | 'uon-luoi'
export type FinalGroup = 'don' | 'kep' | 'mui' | 'i' | 'u' | 'v' | 'dac-biet'

export interface PinyinExampleRef {
  /** Pinyin số, vd `ba4`. */
  pinyin: string
  hanzi: string
}

export interface PinyinInitial {
  /** `""` = không thanh mẫu (cột Ø). */
  code: string
  group: InitialGroup
  display: string
  ipa: string
  aspirated: boolean
  noteVi: string
  examples: PinyinExampleRef[]
}

export interface PinyinFinal {
  /** `v` = ü (`v`, `ve`, `van`, `vn`), `-i` = vận mẫu sau z/c/s/zh/ch/sh/r. */
  code: string
  group: FinalGroup
  display: string
  /** Cách viết khi đứng một mình (`yan`, `wu`, `yu`), `null` nếu không tự đứng. */
  standaloneSpelling?: string | null
  noteVi: string
}

export interface ToneExample {
  hanzi: string
  meaningVi: string
}

export interface PinyinSyllable {
  /** Khoá R5-1: chữ thường, không thanh, `v` chỉ ở `nv|lv|nve|lve`. */
  syllable: string
  initial: string
  final: string
  /** Chỉ khoá `"1".."4"`; rỗng ⇒ không có chữ minh hoạ (không nghe được). */
  tones: Partial<Record<ToneKey, ToneExample>>
}

export interface PinyinChart {
  version: string
  initials: PinyinInitial[]
  finals: PinyinFinal[]
  syllables: PinyinSyllable[]
}

export interface GuideExample {
  pinyin: string
  hanzi: string
  meaningVi: string
}

export type GuideBlock =
  | { type: 'paragraph'; text: string }
  | { type: 'tip'; text: string }
  | { type: 'tone_contour'; tones: number[] }
  | { type: 'examples'; items: GuideExample[] }
  | { type: 'compare'; title: string; pairs: { left: GuideExample; right: GuideExample; noteVi?: string }[] }

export interface GuideTopic {
  id: string
  title: string
  order: number
  blocks: GuideBlock[]
}

export interface PinyinGuide {
  version: string
  topics: GuideTopic[]
}

export type DrillMode = 'listen_tone' | 'tone_pair'

export interface SubmitDrillPart {
  syllable: string
  hanzi: string
  expectedTone: DrillTone
  answeredTone: DrillTone
}

export interface SubmitDrillItem {
  parts: SubmitDrillPart[]
  /** Của CÂU (ms), `null` nếu không đo được. */
  responseMs: number | null
  replayCount: number
}

export interface SubmitToneDrillRequest {
  /** `crypto.randomUUID()` sinh lúc bắt đầu phiên — nộp lại cùng id là idempotent (R5-10). */
  clientSessionId: string
  mode: DrillMode
  /** ISO-8601 UTC có `Z` (`toISOString()`) — backend từ chối chuỗi không `Z` (D38). */
  startedAt: string
  finishedAt: string
  items: SubmitDrillItem[]
}

export interface ToneCount {
  total: number
  correct: number
}

export interface SubmitToneDrillResponse {
  id: string
  clientSessionId: string
  mode: DrillMode
  /** Đếm theo CÂU. */
  total: number
  correct: number
  /** Ngày học theo múi giờ người dùng (`YYYY-MM-DD`). */
  localDate: string
  /** Đếm theo PHẦN, luôn đủ 4 khoá. */
  byTone: Record<ToneKey, ToneCount>
}

export interface ToneAccuracy {
  total: number
  correct: number
  /** `null` khi `total = 0`. */
  accuracy: number | null
}

export interface ToneConfusion {
  expected: DrillTone
  answered: DrillTone
  count: number
}

export interface ToneStats {
  totalAnswered: number
  sessionsCount: number
  lastSessionAt: string | null
  windowSize: number
  accuracy: number | null
  byTone: Record<ToneKey, ToneAccuracy>
  confusions: ToneConfusion[]
  recommendedFocus: DrillTone[]
  g0Reached: boolean
}
