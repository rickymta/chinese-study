import type { ReactNode } from 'react'
import { Box, Card, CardContent, Typography } from '@mui/material'

export interface AuthShellProps {
  /** Tên app hiện trên đầu thẻ, vd "AntFarm · Tiếng Trung". */
  brand: string
  title: string
  logo?: ReactNode
  children: ReactNode
}

/** Khung chung của trang đăng nhập/đăng ký: thẻ căn giữa, rộng tối đa 420px, dùng tốt ở 375px (mobile-first). */
export function AuthShell({ brand, title, logo, children }: AuthShellProps) {
  return (
    <Box
      component="main"
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        alignItems: { xs: 'flex-start', sm: 'center' },
        justifyContent: 'center',
        bgcolor: 'background.default',
        px: 2,
        py: { xs: 3, sm: 4 },
      }}
    >
      <Card sx={{ width: '100%', maxWidth: 420 }}>
        <CardContent sx={{ p: { xs: 2.5, sm: 3.5 } }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
            {logo}
            <Typography variant="subtitle2" sx={{ color: 'primary.main', fontWeight: 700, letterSpacing: 0.3 }}>
              {brand}
            </Typography>
          </Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 700, mb: 2.5 }}>
            {title}
          </Typography>
          {children}
        </CardContent>
      </Card>
    </Box>
  )
}
