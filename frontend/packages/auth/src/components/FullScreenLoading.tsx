import { Box, CircularProgress, Typography } from '@mui/material'

/** Màn chờ toàn trang khi đang nạp phiên (bootstrap) — tránh nháy trang đăng nhập rồi mới vào app. */
export function FullScreenLoading({ label = 'Đang kiểm tra phiên đăng nhập…' }: { label?: string }) {
  return (
    <Box
      role="status"
      aria-live="polite"
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        bgcolor: 'background.default',
        p: 3,
      }}
    >
      <CircularProgress />
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
    </Box>
  )
}
