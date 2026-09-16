import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import { Button, Typography } from '@mui/material'
import { AppDialog } from '../components/dialog/AppDialog'

export interface ConfirmOptions {
  title: ReactNode
  message: ReactNode
  /** Nhãn nút đồng ý. Mặc định "Đồng ý". */
  confirmText?: string
  /** Nhãn nút huỷ. Mặc định "Huỷ". */
  cancelText?: string
  /** `danger` ⇒ nút đồng ý màu đỏ (thao tác không hoàn tác: gỡ quyền, xoá). */
  tone?: 'default' | 'danger'
}

type ConfirmFn = (opts: ConfirmOptions) => Promise<boolean>

const ConfirmContext = createContext<ConfirmFn | null>(null)

interface PendingConfirm {
  opts: ConfirmOptions
  resolve: (ok: boolean) => void
}

/**
 * Hộp xác nhận dùng chung (F4 §5.3.E) thay `window.confirm`: `const confirm = useConfirm(); if (await confirm({...}))`.
 * Dựa trên `AppDialog` — bấm ra ngoài/ESC KHÔNG đóng (tránh vô tình huỷ/đồng ý); nút X = huỷ. Đặt trong
 * `ThemeProvider`, ngoài `RouterProvider` (không dùng hook router).
 */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<PendingConfirm | null>(null)
  // Giữ opts của hộp đang đóng để nội dung không "nhảy" trắng trong lúc chạy hiệu ứng đóng.
  const lastOptsRef = useRef<ConfirmOptions | null>(null)

  const confirm = useCallback<ConfirmFn>((opts) => {
    return new Promise<boolean>((resolve) => {
      // Gọi chồng khi đang mở một hộp khác ⇒ hộp cũ coi như huỷ (không để treo promise).
      setPending((prev) => {
        prev?.resolve(false)
        return { opts, resolve }
      })
    })
  }, [])

  const settle = (ok: boolean) => {
    setPending((prev) => {
      if (prev) {
        lastOptsRef.current = prev.opts
        prev.resolve(ok)
      }
      return null
    })
  }

  const opts = pending?.opts ?? lastOptsRef.current
  const danger = opts?.tone === 'danger'

  const value = useMemo(() => confirm, [confirm])

  return (
    <ConfirmContext.Provider value={value}>
      {children}
      <AppDialog
        open={pending !== null}
        onClose={() => settle(false)}
        title={opts?.title}
        fullScreenBelow={false}
        maxWidth="xs"
        actions={
          <>
            <Button color="inherit" onClick={() => settle(false)}>
              {opts?.cancelText ?? 'Huỷ'}
            </Button>
            <Button variant="contained" color={danger ? 'error' : 'primary'} onClick={() => settle(true)} autoFocus>
              {opts?.confirmText ?? 'Đồng ý'}
            </Button>
          </>
        }
      >
        {typeof opts?.message === 'string' ? <Typography variant="body2">{opts.message}</Typography> : opts?.message}
      </AppDialog>
    </ConfirmContext.Provider>
  )
}

/** Trả hàm `confirm(opts) => Promise<boolean>`; `true` khi người dùng bấm đồng ý. */
export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext)
  if (!ctx) throw new Error('useConfirm phải được dùng bên trong <ConfirmProvider> của @af/ui')
  return ctx
}
