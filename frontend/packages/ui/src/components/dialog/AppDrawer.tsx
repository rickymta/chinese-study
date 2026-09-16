import type { ReactNode } from 'react'
import { Box, Divider, Drawer, IconButton, Typography, useMediaQuery, useTheme } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'

export interface AppDrawerProps {
  open: boolean
  onClose: () => void
  title?: ReactNode
  /**
   * Cạnh bám: `'responsive'` (mặc định) = trượt từ dưới lên ở xs–sm (điện thoại), từ phải sang ở md+.
   * Có thể ép `'right' | 'bottom' | 'left'`.
   */
  anchor?: 'right' | 'bottom' | 'left' | 'responsive'
  /** Bề rộng khi bám trái/phải (px). Mặc định 420. */
  width?: number
  /** `true` ⇒ bấm ra ngoài / ESC cũng đóng. Mặc định `false` — hộp chỉ đọc muốn đóng nhanh thì khai tường minh kèm lý do. */
  closeOnBackdrop?: boolean
  children?: ReactNode
  /** Hàng nút dưới cùng. */
  actions?: ReactNode
}

/**
 * Ngăn kéo dùng chung (hợp đồng F4/F5 §5.3.A): responsive theo thiết bị, chặn đóng ngoài ý muốn như `AppDialog`.
 * Bám dưới: bo góc trên, cao tối đa `85dvh`, nội dung cuộn, chừa vùng an toàn iPhone.
 */
export function AppDrawer({
  open,
  onClose,
  title,
  anchor = 'responsive',
  width = 420,
  closeOnBackdrop = false,
  children,
  actions,
}: AppDrawerProps) {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const resolvedAnchor = anchor === 'responsive' ? (isDesktop ? 'right' : 'bottom') : anchor
  const isBottom = resolvedAnchor === 'bottom'

  return (
    <Drawer
      open={open}
      anchor={resolvedAnchor}
      onClose={(_event, reason) => {
        if (!closeOnBackdrop && (reason === 'backdropClick' || reason === 'escapeKeyDown')) return
        onClose()
      }}
      slotProps={{
        paper: {
          sx: isBottom
            ? {
                borderTopLeftRadius: 16,
                borderTopRightRadius: 16,
                maxHeight: '85dvh',
                display: 'flex',
                flexDirection: 'column',
                pb: 'env(safe-area-inset-bottom)',
              }
            : { width: { xs: '100%', sm: width }, maxWidth: '100vw', display: 'flex', flexDirection: 'column' },
        },
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 2, py: 1.5, flexShrink: 0 }}>
        {isBottom && !title && <Box sx={{ width: 36, height: 4, borderRadius: 2, bgcolor: 'divider', mx: 'auto' }} />}
        {title && (
          <Typography component="h2" variant="h6" sx={{ flex: 1, minWidth: 0, fontWeight: 700 }}>
            {title}
          </Typography>
        )}
        <IconButton aria-label="Đóng" onClick={onClose} sx={{ ml: 'auto', color: 'text.secondary' }}>
          <CloseIcon />
        </IconButton>
      </Box>
      <Divider />
      <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', px: 2, py: 2 }}>{children}</Box>
      {actions && (
        <>
          <Divider />
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1, px: 2, py: 1.5, flexShrink: 0 }}>{actions}</Box>
        </>
      )}
    </Drawer>
  )
}
