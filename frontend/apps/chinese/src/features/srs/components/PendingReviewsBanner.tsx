import { Alert } from '@mui/material'

/** Dải cảnh báo khi còn đánh giá gửi HỎNG ít nhất một lần (`unsentCount`, mất mạng/5xx) — không nháy khi chấm bình thường. */
export function PendingReviewsBanner({ count }: { count: number }) {
  if (count <= 0) return null
  return (
    <Alert severity="warning" sx={{ py: 0.5 }}>
      Đang chờ gửi {count} đánh giá — sẽ tự gửi lại khi có mạng.
    </Alert>
  )
}
