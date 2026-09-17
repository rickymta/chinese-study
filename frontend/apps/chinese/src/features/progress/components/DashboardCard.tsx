import type { ReactNode } from 'react'
import { Box, Card, CardContent, Typography } from '@mui/material'

export interface DashboardCardProps {
  title: string
  /** Icon nhỏ trước tiêu đề. */
  icon?: ReactNode
  /** Phần tử bên phải tiêu đề (chip, link phụ). */
  aside?: ReactNode
  children: ReactNode
}

/**
 * Khung thẻ thống nhất cho các khối của trang tổng quan: tiêu đề `h2` (điều hướng bằng đọc màn hình), icon, nội dung.
 * Cao đều trong lưới 2 cột (`height: 100%`).
 */
export function DashboardCard({ title, icon, aside, children }: DashboardCardProps) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, height: '100%' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 0 }}>
            {icon && <Box sx={{ display: 'inline-flex', color: 'text.secondary', '& svg': { fontSize: 20 } }}>{icon}</Box>}
            <Typography component="h2" variant="subtitle1" sx={{ fontWeight: 600 }} noWrap>
              {title}
            </Typography>
          </Box>
          {aside}
        </Box>
        {children}
      </CardContent>
    </Card>
  )
}
