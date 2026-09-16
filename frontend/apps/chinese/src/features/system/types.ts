/** Phản hồi `GET /api/system/info` của MỌI service AntFarm (hợp đồng §6.1). */
export interface SystemInfo {
  service: string
  version: string
  environment: string
  /** ISO-8601 UTC, vd `2026-09-16T08:00:00Z`. */
  serverTimeUtc: string
}

/** Hai service mà app tiếng Trung đi qua gateway tới (§7 F1). */
export type SystemService = 'chinese' | 'identity'
