// @af/utils — tiện ích dùng chung cho MỌI app ngôn ngữ của AntFarm (import thẳng TS source, không build/dist).
// Tiện ích đặc thù một ngôn ngữ (vd pinyin) KHÔNG đặt ở đây — đặt trong app của ngôn ngữ đó (hợp đồng §5.3.3).

export { parseApiError } from './parseApiError'
export type { ParsedApiError } from './parseApiError'
export { emailSchema, passwordSchema, displayNameSchema, timeZoneSchema } from './schemas'

// F4 — múi giờ (danh sách chọn, quy bí danh, so khớp) + thời gian tương đối tiếng Việt.
export {
  normalizeTimeZone,
  detectBrowserTimeZone,
  listTimeZoneOptions,
  matchesTimeZoneQuery,
  getUtcOffsetMinutes,
  formatUtcOffset,
  FALLBACK_TIME_ZONES,
} from './timeZones'
export type { TimeZoneOption } from './timeZones'
export { formatRelativeTime } from './relativeTime'
