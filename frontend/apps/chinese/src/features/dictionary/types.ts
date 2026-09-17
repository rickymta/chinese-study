// Kiểu dữ liệu F6 theo hợp đồng chi tiết F6/F7 §6.1 (chinese-backend `/api/dictionary/*`).
// Serializer backend bật `WhenWritingNull` ⇒ trường có thể `null` bị LƯỢC khỏi JSON: khai `?:`/`| null`
// và coi thiếu = null (RK41). Pinyin luôn ở dạng SỐ THANH (`ai4`) — hiển thị dạng dấu qua `lib/pinyin.ts`.

/** Nghĩa Việt: `machine` = dịch máy/CVDICT chưa duyệt (hiện chip "Chưa duyệt"); `reviewed` = đã duyệt ở F10. */
export type MeaningViStatus = 'machine' | 'reviewed'
export type MeaningViSource = 'cvdict' | 'machine' | 'manual'
/** Hán Việt: `derived` = suy ra từ âm từng chữ (R6-8); `reviewed` = đã duyệt. */
export type HanVietStatus = 'derived' | 'reviewed'
/** Cách khớp của một kết quả tìm — `browse` khi `q` rỗng (liệt kê theo lộ trình). */
export type MatchKind = 'browse' | 'hanzi' | 'pinyin' | 'han_viet' | 'meaning'

export interface SearchParams {
  q?: string
  /** 1..7 */
  hsk?: number
  /** ≥ 1 */
  page?: number
  /** 1..100, mặc định 20 */
  pageSize?: number
}

/** Một dòng kết quả tìm (`meaningsVi` tối đa 3 phần tử). */
export interface WordSummary {
  id: string
  simplified: string
  traditional?: string | null
  pinyin: string
  hsk3Level?: number | null
  hsk2Level?: number | null
  hanViet?: string | null
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
  matchKind: MatchKind
}

export interface SearchResponse {
  items: WordSummary[]
  page: number
  pageSize: number
  totalCount: number
}

/** Chữ cấu thành trong chi tiết từ. */
export interface WordCharacter {
  hanzi: string
  pinyinReadings?: string[] | null
  hanViet?: string[] | null
  strokeCount?: number | null
}

/** Trạng thái thẻ SRS của người dùng cho từ này — F7 thêm; trước F7 luôn `null`. */
export interface WordSrsInfo {
  cardId: string
  state: string
  dueAt?: string | null
  isSuspended: boolean
}

export interface WordDetail {
  id: string
  simplified: string
  traditional?: string | null
  variants?: string[] | null
  pinyin: string
  hsk3Level?: number | null
  hsk2Level?: number | null
  hskExam2026Level?: number | null
  officialIndex?: number | null
  pathOrder?: number | null
  frequencyRank?: number | null
  /** Mã từ loại (`n`, `v`, `a`…) — nhãn Việt qua `lib/pos.ts`, mã lạ ẩn. */
  pos?: string[] | null
  usageNote?: string | null
  meaningsEn?: string[] | null
  meaningsVi?: string[] | null
  meaningViStatus: MeaningViStatus
  meaningViSource?: MeaningViSource | null
  hanViet?: string | null
  hanVietStatus?: HanVietStatus | null
  /** Khoá nguồn (§5.4.1) — nhãn/giấy phép qua `lib/sources.ts`. */
  sources?: string[] | null
  characters?: WordCharacter[] | null
  srs?: WordSrsInfo | null
}

export interface CharacterDetail {
  hanzi: string
  traditionalVariants?: string[] | null
  pinyinReadings?: string[] | null
  hanViet?: string[] | null
  hanVietByPinyin?: Record<string, string> | null
  hanVietStatus?: HanVietStatus | null
  strokeCount?: number | null
  radical?: string | null
  radicalNumber?: number | null
  /** Từ có chữ này — tối đa 20, sắp theo `path_order`. */
  words?: WordSummary[] | null
}
