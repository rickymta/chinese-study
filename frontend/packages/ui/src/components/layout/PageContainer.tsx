import { Box, Typography, type BoxProps } from '@mui/material'
import type { ReactNode } from 'react'

export interface PageContainerProps extends Omit<BoxProps, 'title'> {
  children: ReactNode
  /** Tiêu đề trang (h1 ngữ nghĩa, hiển thị cỡ h5). Bỏ trống ⇒ không vẽ phần đầu. */
  title?: ReactNode
  /** Nút/hành động đặt bên phải tiêu đề. */
  actions?: ReactNode
  maxWidth?: number | string
}

/** Khung nội dung trang: giới hạn bề rộng + tiêu đề tuỳ chọn. Padding do AppLayout lo. */
export function PageContainer({ children, title, actions, maxWidth = 1100, sx, ...props }: PageContainerProps) {
  return (
    // Gộp `sx` dạng MẢNG (khuyến nghị MUI) — spread object sẽ hỏng khi bên gọi truyền `sx` là mảng hoặc hàm.
    <Box sx={[{ width: '100%', maxWidth, mx: 'auto' }, ...(Array.isArray(sx) ? sx : [sx])]} {...props}>
      {(title || actions) && (
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 2,
            flexWrap: 'wrap',
            mb: 2,
          }}
        >
          {title && (
            <Typography component="h1" variant="h5" sx={{ fontWeight: 700 }}>
              {title}
            </Typography>
          )}
          {actions && <Box sx={{ display: 'flex', gap: 1 }}>{actions}</Box>}
        </Box>
      )}
      {children}
    </Box>
  )
}
