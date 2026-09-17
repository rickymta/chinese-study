import type { ReactNode } from 'react'
import { Box, type SxProps, type Theme } from '@mui/material'

export interface StickyActionBarProps {
  children: ReactNode
  sx?: SxProps<Theme>
}

/**
 * Thanh hành động DÍNH ĐÁY cho màn học (luyện thanh, ôn thẻ...): ở điện thoại, nút "Tiếp" nằm cuối trang dài bị
 * bottom nav che (phát hiện khi tích hợp F5 ở 375×812). Dùng `position: sticky` với `bottom` = biến CSS
 * `--af-bottom-nav-offset` do `AppLayout` phát (chiều cao bottom nav + safe-area ở xs–sm, `0px` ở md+).
 *
 * Lưu ý: phần tử cha (và các tổ tiên tới vùng cuộn) không được có `overflow: hidden`, nếu không sticky mất tác dụng.
 */
export function StickyActionBar({ children, sx }: StickyActionBarProps) {
  return (
    <Box
      sx={[
        {
          position: 'sticky',
          bottom: 'var(--af-bottom-nav-offset, 0px)',
          zIndex: 2,
          // Nền + padding để nội dung cuộn phía dưới không lộ qua nút; kéo ra bằng padding của AppLayout ở xs.
          bgcolor: 'background.default',
          mx: { xs: -2, md: 0 },
          px: { xs: 2, md: 0 },
          py: 1,
          borderTop: { xs: 1, md: 0 },
          borderColor: { xs: 'divider', md: 'transparent' },
          display: 'flex',
          flexDirection: 'column',
          gap: 1,
        },
        ...(Array.isArray(sx) ? sx : [sx]),
      ]}
    >
      {children}
    </Box>
  )
}
