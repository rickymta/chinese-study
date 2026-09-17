import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { Alert, Snackbar, useMediaQuery, useTheme, type AlertColor } from '@mui/material'

export interface ToastApi {
  success(message: string): void
  error(message: string): void
  info(message: string): void
  warning(message: string): void
}

interface ToastItem {
  id: number
  message: string
  severity: AlertColor
}

const ToastContext = createContext<ToastApi | null>(null)

/** Thời gian hiện mỗi thông báo (hợp đồng §5.3.E: 4 giây). */
const AUTO_HIDE_MS = 4000

/**
 * Thông báo nhanh dùng chung (F4 §5.3.E): một `Snackbar` + `Alert`, hàng đợi đơn giản (thông báo sau chờ thông
 * báo trước đóng). Ở xs neo TRÊN-GIỮA để không đè bottom nav; md+ neo dưới-giữa. Bỏ qua `clickaway` (bấm chỗ khác
 * không tắt sớm — người học đang thao tác).
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const [queue, setQueue] = useState<ToastItem[]>([])
  const [current, setCurrent] = useState<ToastItem | null>(null)
  const [open, setOpen] = useState(false)

  // Lấy thông báo kế tiếp khi không có cái nào đang hiện.
  useEffect(() => {
    if (current === null && queue.length > 0) {
      setCurrent(queue[0]!)
      setQueue((q) => q.slice(1))
      setOpen(true)
    }
  }, [queue, current])

  const push = useCallback((severity: AlertColor, message: string) => {
    setQueue((q) => [...q, { id: Date.now() + Math.random(), message, severity }])
  }, [])

  const api = useMemo<ToastApi>(
    () => ({
      success: (m) => push('success', m),
      error: (m) => push('error', m),
      info: (m) => push('info', m),
      warning: (m) => push('warning', m),
    }),
    [push],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <Snackbar
        key={current?.id}
        open={open}
        autoHideDuration={AUTO_HIDE_MS}
        onClose={(_e, reason) => {
          if (reason === 'clickaway') return
          setOpen(false)
        }}
        // Hết hiệu ứng đóng mới bỏ `current` ⇒ effect ở trên lấy cái kế tiếp.
        slotProps={{ transition: { onExited: () => setCurrent(null) } }}
        anchorOrigin={isXs ? { vertical: 'top', horizontal: 'center' } : { vertical: 'bottom', horizontal: 'center' }}
        sx={isXs ? { top: 'calc(64px + env(safe-area-inset-top))' } : undefined}
      >
        <Alert
          severity={current?.severity ?? 'info'}
          variant="filled"
          onClose={() => setOpen(false)}
          sx={{ width: '100%', boxShadow: 3 }}
        >
          {current?.message}
        </Alert>
      </Snackbar>
    </ToastContext.Provider>
  )
}

/** `const toast = useToast(); toast.success('Đã lưu hồ sơ')`. */
export function useToast(): ToastApi {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast phải được dùng bên trong <ToastProvider> của @af/ui')
  return ctx
}
