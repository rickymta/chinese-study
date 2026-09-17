// Giới hạn tốc độ đọc TTS tiếng Trung — thuần, dùng chung cho provider của kit và `useTtsRate` của app học viên
// (luật `PUT /api/me/learning-settings`: 0,5–1,2, làm tròn 2 chữ số). Mặc định 0,8: chậm hơn tự nhiên để nghe rõ thanh.
export const TTS_RATE_DEFAULT = 0.8
export const TTS_RATE_MIN = 0.5
export const TTS_RATE_MAX = 1.2

/** Kẹp 0,5–1,2 và làm tròn 2 chữ số; giá trị không hợp lệ (NaN/∞) ⇒ mặc định. */
export const clampTtsRate = (v: number): number =>
  Math.round(
    Math.min(TTS_RATE_MAX, Math.max(TTS_RATE_MIN, Number.isFinite(v) ? v : TTS_RATE_DEFAULT)) * 100,
  ) / 100
