import { useEffect, useRef } from 'react'
import { useBlocker } from 'react-router-dom'
import { useConfirm } from '@af/ui'

/**
 * Cảnh báo rời trang khi còn thay đổi chưa lưu (hợp đồng W3a §5.3.3a — chép ý từ `apps/chinese/features/admin-content`):
 * chặn điều hướng trong app bằng `useBlocker` (cần data router — `createBrowserRouter`) + hộp `useConfirm`; đóng
 * tab/tải lại bằng `beforeunload` (trình duyệt hiện hộp chuẩn, không tuỳ biến được lời). Chỉ chặn khi ĐỔI PATHNAME —
 * đổi `?tab=` cùng trang không tính.
 */
export function useUnsavedChangesGuard(when: boolean) {
  const confirm = useConfirm()
  const blocker = useBlocker(({ currentLocation, nextLocation }) => when && currentLocation.pathname !== nextLocation.pathname)
  // Chỉ mở hộp khi `state` chuyển sang `blocked` (object `blocker` đổi identity mỗi render — không đưa vào deps).
  const blockerRef = useRef(blocker)
  blockerRef.current = blocker

  useEffect(() => {
    if (blocker.state !== 'blocked') return
    let cancelled = false
    void confirm({
      title: 'Rời trang?',
      message: 'Thay đổi chưa lưu sẽ mất. Bạn vẫn muốn rời trang này?',
      confirmText: 'Rời trang',
      cancelText: 'Ở lại',
      tone: 'danger',
    }).then((ok) => {
      if (cancelled) return
      const current = blockerRef.current
      if (current.state !== 'blocked') return
      if (ok) current.proceed()
      else current.reset()
    })
    return () => {
      cancelled = true
    }
  }, [blocker.state, confirm])

  useEffect(() => {
    if (!when) return
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault()
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [when])
}
