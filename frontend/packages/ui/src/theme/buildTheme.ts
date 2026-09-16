import { createTheme, alpha, type ThemeOptions } from '@mui/material/styles'

export type ThemeMode = 'light' | 'dark'

/**
 * Phông chữ Hán/CJK dùng chung (hợp đồng §5.3.0.5). Không tải webfont trong MVP — dựa vào phông hệ thống:
 * Windows có Microsoft YaHei, macOS/iOS có PingFang SC, Linux thường có Noto Sans SC.
 * Được phát ra thành biến CSS `--af-font-cjk` ở `:root` để `LangText` (và bất kỳ CSS nào) dùng lại.
 */
export const AF_FONT_CJK = '"Noto Sans SC", "Microsoft YaHei", "PingFang SC", "Hiragino Sans GB", sans-serif'

export interface BuildThemeOptions {
  mode: ThemeMode
  /**
   * Màu nhấn RIÊNG của từng app ngôn ngữ (tiếng Trung: đỏ son `#C62828`). Đặt vào `palette.secondary`
   * để dùng cho huy hiệu, chip, điểm nhấn — còn `primary` là màu nền tảng dùng chung mọi ngôn ngữ.
   */
  accent?: string
  /** Màu nhấn riêng cho chế độ tối (mặc định làm sáng từ `accent`). */
  accentDark?: string
  /** ThemeOptions bổ sung, deep-merge ĐÈ LÊN base (app tuỳ biến thêm nếu cần). */
  extra?: ThemeOptions
}

// Màu nền tảng dùng chung mọi ngôn ngữ (§5.3.0.5): xanh lục đậm + nền sáng ngà.
const PRIMARY_LIGHT = '#2E7D32'
// Chế độ tối cần tông SÁNG hơn để chữ nút text/outlined còn đọc được trên nền tối (bài học MedDental
// 30/08/2026: một màu không gánh nổi cả vai "nền nút" lẫn vai "chữ nút" ở hai chế độ).
const PRIMARY_DARK = '#66BB6A'
const BG_LIGHT = '#FAFAF7'
const BG_DARK = '#101412'
const PAPER_DARK = '#1A201C'

/**
 * Theme dùng chung cho mọi app AntFarm. Mỗi app truyền `accent` riêng của ngôn ngữ mình.
 */
export function buildTheme({ mode, accent = '#C62828', accentDark, extra }: BuildThemeOptions) {
  const isDark = mode === 'dark'
  const primary = isDark ? PRIMARY_DARK : PRIMARY_LIGHT
  const secondary = isDark ? (accentDark ?? lighten(accent)) : accent
  const textOnPrimary = isDark ? '#0B1410' : '#FFFFFF'

  const base: ThemeOptions = {
    palette: {
      mode,
      primary: { main: primary, contrastText: textOnPrimary },
      secondary: { main: secondary, contrastText: isDark ? '#1A0A0A' : '#FFFFFF' },
      background: isDark
        ? { default: BG_DARK, paper: PAPER_DARK }
        : { default: BG_LIGHT, paper: '#FFFFFF' },
    },
    typography: {
      fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
      fontSize: 14,
      // Không spread `extra.typography` ở đây — `createTheme(base, extra)` bên dưới đã deep-merge (gợi ý review F1).
    },
    shape: { borderRadius: 10 },
    components: {
      MuiButton: { defaultProps: { disableElevation: true } },
      MuiCard: { defaultProps: { variant: 'outlined' } },
      MuiCssBaseline: {
        styleOverrides: {
          ':root': { '--af-font-cjk': AF_FONT_CJK },
          // Thanh cuộn mảnh, tự đúng cho cả sáng/tối nhờ alpha() trên màu chữ.
          '*': {
            scrollbarWidth: 'thin' as const,
            scrollbarColor: `${alpha(isDark ? '#FFFFFF' : '#101412', 0.25)} transparent`,
          },
        },
      },
    },
  }

  return extra ? createTheme(base, extra) : createTheme(base)
}

/** Làm sáng một màu hex (#RRGGBB) ~35% để dùng ở chế độ tối — đủ dùng cho màu nhấn, không cần thư viện. */
function lighten(hex: string): string {
  if (!/^#[0-9a-fA-F]{6}$/.test(hex)) return hex
  const n = parseInt(hex.slice(1), 16)
  const mix = (c: number) => Math.round(c + (255 - c) * 0.35)
  const r = mix((n >> 16) & 0xff)
  const g = mix((n >> 8) & 0xff)
  const b = mix(n & 0xff)
  return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, '0')}`
}
