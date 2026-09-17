import type { ReactNode } from 'react'
import {
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  useMediaQuery,
  useTheme,
  type DialogProps,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'

export interface AppDialogProps extends Omit<DialogProps, 'onClose' | 'title'> {
  open: boolean
  /** Gọi khi người dùng chủ động đóng (nút X, nút trong `actions`, hoặc backdrop/ESC nếu `closeOnBackdrop`). */
  onClose: () => void
  title?: ReactNode
  /** Hàng nút dưới cùng (`DialogActions`). */
  actions?: ReactNode
  /**
   * `true` ⇒ bấm ra ngoài / ESC cũng đóng. Mặc định `false` — hộp thoại có thao tác ghi không được đóng ngoài ý muốn
   * (quy tắc CLAUDE.md). Hộp chỉ đọc muốn đóng nhanh thì khai tường minh kèm bình luận lý do.
   */
  closeOnBackdrop?: boolean
  hideCloseButton?: boolean
  /** Toàn màn hình khi bề rộng dưới breakpoint này (mặc định `'sm'` — điện thoại ~375px). `false` ⇒ không bao giờ. */
  fullScreenBelow?: 'sm' | 'md' | false
  children?: ReactNode
}

/**
 * Hộp thoại dùng chung (hợp đồng F4/F5 §5.3.A): bọc `Dialog` MUI, lọc `reason` của `onClose` để chặn đóng
 * ngoài ý muốn, nút X ở tiêu đề, tự toàn màn hình ở điện thoại. Mọi app dùng cái này thay `<Dialog>` trần
 * (lint `raw-dialog` chặn import trực tiếp trong `apps/`).
 */
export function AppDialog({
  open,
  onClose,
  title,
  actions,
  closeOnBackdrop = false,
  hideCloseButton = false,
  fullScreenBelow = 'sm',
  children,
  ...dialogProps
}: AppDialogProps) {
  const theme = useTheme()
  // Hook không được gọi có điều kiện ⇒ luôn tính, chỉ dùng khi `fullScreenBelow !== false`.
  const belowBreakpoint = useMediaQuery(theme.breakpoints.down(fullScreenBelow || 'sm'), { noSsr: true })
  const fullScreen = fullScreenBelow !== false && belowBreakpoint

  return (
    <Dialog
      open={open}
      fullScreen={fullScreen}
      fullWidth
      maxWidth="sm"
      {...dialogProps}
      // MUI v9 gọi `onClose(event, reason)` — chỉ cho backdrop/ESC đóng khi bên gọi cho phép.
      onClose={(_event, reason) => {
        if (!closeOnBackdrop && (reason === 'backdropClick' || reason === 'escapeKeyDown')) return
        onClose()
      }}
    >
      {(title || !hideCloseButton) && (
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1, pr: hideCloseButton ? 3 : 7 }}>
          {title}
          {!hideCloseButton && (
            <IconButton
              aria-label="Đóng"
              onClick={onClose}
              sx={{ position: 'absolute', right: 8, top: 8, color: 'text.secondary' }}
            >
              <CloseIcon />
            </IconButton>
          )}
        </DialogTitle>
      )}
      <DialogContent dividers={!!title}>{children}</DialogContent>
      {actions && <DialogActions sx={{ px: 3, py: 2 }}>{actions}</DialogActions>}
    </Dialog>
  )
}
