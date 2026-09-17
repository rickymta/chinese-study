// Định dạng khoảng thời gian dự kiến trên 4 nút chấm (hợp đồng F6/F7 §5.3.2).
// Đầu vào chính: ISO-8601 duration (`PT1M`, `PT5M30S`, `P8D`, `P1DT2H`). Phòng backend .NET trả `TimeSpan` mặc định
// (`d.hh:mm:ss[.fffffff]`) — nhận luôn dạng đó để nút không hiện "—" oan.

const ISO_DURATION =
  /^P(?:(\d+(?:[.,]\d+)?)Y)?(?:(\d+(?:[.,]\d+)?)M)?(?:(\d+(?:[.,]\d+)?)W)?(?:(\d+(?:[.,]\d+)?)D)?(?:T(?:(\d+(?:[.,]\d+)?)H)?(?:(\d+(?:[.,]\d+)?)M)?(?:(\d+(?:[.,]\d+)?)S)?)?$/i
const DOTNET_TIMESPAN = /^(-)?(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$/

const num = (s: string | undefined): number => (s === undefined ? 0 : Number(s.replace(',', '.')))

/**
 * Đổi chuỗi khoảng thời gian sang tổng số giây. Trả `null` khi không hiểu. Năm/tháng ISO quy ước 365/30 ngày
 * (server không sinh, chỉ phòng hờ).
 */
export function parseDurationSeconds(input: string | null | undefined): number | null {
  if (!input) return null
  const s = input.trim()
  const iso = ISO_DURATION.exec(s)
  if (iso && s.length > 1) {
    const [, y, mo, w, d, h, mi, sec] = iso
    if ([y, mo, w, d, h, mi, sec].every((x) => x === undefined)) return null
    const days = num(y) * 365 + num(mo) * 30 + num(w) * 7 + num(d)
    return days * 86_400 + num(h) * 3_600 + num(mi) * 60 + num(sec)
  }
  const ts = DOTNET_TIMESPAN.exec(s)
  if (ts) {
    const [, neg, d, h, mi, sec, frac] = ts
    const total = num(d) * 86_400 + num(h) * 3_600 + num(mi) * 60 + num(sec) + (frac ? Number(`0.${frac}`) : 0)
    return neg ? -total : total
  }
  return null
}

/** Số thập phân kiểu Việt: 1 chữ số sau dấu phẩy, bỏ `,0` (`5.5` ⇒ "5,5"; `2.0` ⇒ "2"). */
function oneDecimal(v: number): string {
  const rounded = Math.round(v * 10) / 10
  return rounded.toFixed(1).replace(/\.0$/, '').replace('.', ',')
}

/**
 * Nhãn Việt ngắn cho khoảng thời gian: `< 1 giờ` ⇒ phút (dưới 10 phút giữ 1 chữ số thập phân), `< 1 ngày` ⇒ giờ,
 * `< 30 ngày` ⇒ ngày, `< 365 ngày` ⇒ tháng (ngày/30, 1 chữ số thập phân), còn lại năm (ngày/365).
 * Không hiểu đầu vào ⇒ "—".
 */
export function formatInterval(input: string | null | undefined): string {
  const seconds = parseDurationSeconds(input)
  if (seconds === null || !Number.isFinite(seconds) || seconds < 0) return '—'
  const minutes = seconds / 60
  if (minutes < 60) {
    if (minutes < 10) return `${oneDecimal(minutes)} phút`
    const m = Math.round(minutes)
    if (m < 60) return `${m} phút`
    // 59,6 phút làm tròn thành 60 ⇒ rơi xuống nhánh giờ.
  }
  // Làm tròn có thể đẩy lên bậc trên (23 giờ 50 ⇒ "1 ngày") ⇒ kiểm giá trị ĐÃ làm tròn.
  const hours = minutes / 60
  if (hours < 24) {
    const h = Math.max(1, Math.round(hours))
    if (h < 24) return `${h} giờ`
  }
  const days = hours / 24
  if (days < 30) {
    const d = Math.max(1, Math.round(days))
    if (d < 30) return `${d} ngày`
  }
  if (days < 365) return `${oneDecimal(days / 30)} tháng`
  return `${oneDecimal(days / 365)} năm`
}
