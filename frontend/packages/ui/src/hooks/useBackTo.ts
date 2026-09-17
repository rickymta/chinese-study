import { useCallback } from 'react'
import { useLocation, useNavigate, type Location } from 'react-router-dom'

/**
 * Trạng thái điều hướng chuẩn khi mở trang con: `from` là đường dẫn (kèm query) của trang cha; `parent` là state
 * mà trang cha đang giữ (để khi quay về, trang cha lại quay về đúng trang ông — chuỗi danh sách → từ → chữ → …).
 */
export interface BackToState {
  from: string
  parent?: BackToState | null
}

/** Giới hạn độ sâu chuỗi (từ ↔ chữ có thể lồng vô hạn) — quá sâu thì cắt, tầng cuối quay về `fallback`. */
const MAX_DEPTH = 12

/**
 * Đường dẫn NỘI BỘ an toàn để `navigate`: bắt đầu bằng `/` nhưng không phải `//` (URL tương đối giao thức —
 * `//evil.com` sẽ bị trình duyệt hiểu là nhảy sang site khác).
 */
export function isInternalPath(value: unknown): value is string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
}

/** Đọc state hợp lệ từ `location.state` (state lạ/tay ⇒ `null`). */
function readState(raw: unknown): BackToState | null {
  if (!raw || typeof raw !== 'object') return null
  const s = raw as Partial<BackToState>
  if (!isInternalPath(s.from)) return null
  return { from: s.from, parent: s.parent ?? null }
}

function truncate(state: BackToState | null, depth: number): BackToState | null {
  if (!state) return null
  if (depth >= MAX_DEPTH) return { from: state.from, parent: null }
  return { from: state.from, parent: truncate(state.parent ?? null, depth + 1) }
}

/**
 * Tạo `state` cho `<Link state={linkState(location)}>` khi mở trang con — trang con dùng `useBackTo` để quay về
 * ĐÚNG trang cha kèm query (vd `/tu-dien?q=yeu&page=2`) thay vì đường dẫn mặc định. Chuỗi state của trang cha được
 * gắn vào `parent` nên quay lại nhiều tầng vẫn giữ query gốc.
 */
export function linkState(location: Pick<Location, 'pathname' | 'search' | 'state'>): BackToState {
  return { from: location.pathname + location.search, parent: truncate(readState(location.state), 1) }
}

/**
 * Nút "Quay lại" có chủ đích: nếu `location.state.from` là đường dẫn nội bộ ⇒ về đó (kèm `parent` làm state của
 * trang đích); ngược lại (mở link trực tiếp, tải lại trang, state lạ) ⇒ về `fallback`. Không dùng `navigate(-1)`
 * vì lịch sử có thể là trang ngoài app.
 */
export function useBackTo(fallback: string): () => void {
  const navigate = useNavigate()
  const location = useLocation()
  const state = readState(location.state)
  const from = state?.from
  const parent = state?.parent ?? null
  return useCallback(() => {
    if (from) navigate(from, { state: parent })
    else navigate(fallback)
  }, [navigate, from, parent, fallback])
}
