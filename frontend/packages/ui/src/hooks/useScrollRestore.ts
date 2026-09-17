import { useEffect, useRef } from 'react'

const PREFIX = 'af.scroll.'

function readScroll(key: string): number | null {
  try {
    const raw = sessionStorage.getItem(PREFIX + key)
    const n = raw === null ? NaN : Number(raw)
    return Number.isFinite(n) && n >= 0 ? n : null
  } catch {
    return null
  }
}

function writeScroll(key: string, y: number) {
  try {
    sessionStorage.setItem(PREFIX + key, String(Math.round(y)))
  } catch {
    /* Safari riêng tư / bộ nhớ đầy — bỏ qua, chỉ mất khôi phục vị trí */
  }
}

/**
 * Giữ vị trí cuộn của một danh sách khi người dùng mở trang con rồi quay lại (vd `/tu-dien?q=…` → chi tiết → back).
 * - Lưu `window.scrollY` LIÊN TỤC theo sự kiện `scroll` (gộp bằng `requestAnimationFrame`) và lúc `pagehide`.
 *   KHÔNG lưu trong cleanup effect: cleanup passive chạy SAU khi React đã thay DOM bằng trang con ⇒ trang ngắn hơn
 *   ⇒ `scrollY` bị kẹp về 0 (phát hiện ở review F6.3).
 * - Khôi phục ĐÚNG MỘT LẦN cho mỗi `key` khi `ready` = true (dữ liệu đã render nên chiều cao trang đủ để cuộn).
 * `key` nên chứa cả query để mỗi kết quả tìm có vị trí riêng.
 */
export function useScrollRestore(key: string, ready: boolean): void {
  const restoredFor = useRef<string | null>(null)

  // Lưu theo scroll (rAF) + pagehide; huỷ rAF đang chờ khi rời trang để không ghi giá trị đã bị kẹp.
  useEffect(() => {
    if (typeof window === 'undefined') return
    let raf = 0
    const onScroll = () => {
      if (raf) return
      raf = window.requestAnimationFrame(() => {
        raf = 0
        writeScroll(key, window.scrollY)
      })
    }
    const onPageHide = () => writeScroll(key, window.scrollY)
    window.addEventListener('scroll', onScroll, { passive: true })
    window.addEventListener('pagehide', onPageHide)
    return () => {
      if (raf) window.cancelAnimationFrame(raf)
      window.removeEventListener('scroll', onScroll)
      window.removeEventListener('pagehide', onPageHide)
    }
  }, [key])

  // Khôi phục một lần khi sẵn sàng.
  useEffect(() => {
    if (!ready || typeof window === 'undefined' || restoredFor.current === key) return
    restoredFor.current = key
    const y = readScroll(key)
    if (y === null || y === 0) return
    // Đợi một khung hình để danh sách vẽ xong rồi mới cuộn (không thì chiều cao trang chưa đủ).
    let done = false
    const id = window.requestAnimationFrame(() => {
      done = true
      window.scrollTo({ top: y })
    })
    return () => {
      if (done) return
      // rAF bị huỷ trước khi chạy (StrictMode dev chạy effect → cleanup → chạy lại; hoặc `ready`/`key` đổi ngay)
      // ⇒ bỏ đánh dấu để lần chạy effect kế tiếp còn khôi phục — không thì dev không bao giờ cuộn lại (integration F6.3).
      window.cancelAnimationFrame(id)
      restoredFor.current = null
    }
  }, [key, ready])
}
