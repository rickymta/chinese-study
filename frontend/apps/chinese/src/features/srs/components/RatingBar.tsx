import { Box, Button, Typography } from '@mui/material'
import { formatInterval } from '../lib/formatInterval'
import { RATING_META } from '../lib/ratings'
import { SRS_RATINGS, type SrsIntervals, type SrsRating } from '../types'

export interface RatingBarProps {
  intervals: SrsIntervals
  onRate: (rating: SrsRating) => void
  /** Khoá 300 ms sau khi chấm (chống bấm đúp) hoặc khi chưa lật. */
  disabled?: boolean
}

/**
 * 4 nút chấm Quên/Khó/Được/Dễ — lưới 4 cột bằng nhau, mỗi nút cao ≥ 56px, hai dòng: nhãn đậm + khoảng dự kiến.
 * Ở 375px mỗi nút rộng ~80px ⇒ vẫn đủ cho "5,5 phút". Phím `1`–`4` do trang xử lý (hiện ở tooltip/aria).
 */
export function RatingBar({ intervals, onRate, disabled = false }: RatingBarProps) {
  return (
    <Box
      role="group"
      aria-label="Chấm thẻ"
      sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1, width: '100%' }}
    >
      {SRS_RATINGS.map((rating) => {
        const meta = RATING_META[rating]
        const interval = formatInterval(intervals[rating])
        return (
          <Button
            key={rating}
            variant="contained"
            color={meta.color}
            disabled={disabled}
            onClick={() => onRate(rating)}
            aria-label={`${meta.label} — ${interval} (phím ${meta.key})`}
            aria-keyshortcuts={meta.key}
            sx={{
              minHeight: 56,
              px: 0.5,
              display: 'flex',
              flexDirection: 'column',
              gap: 0.25,
              lineHeight: 1.2,
              textTransform: 'none',
            }}
          >
            <Typography component="span" sx={{ fontWeight: 700, fontSize: 16 }}>
              {meta.label}
            </Typography>
            <Typography component="span" variant="caption" sx={{ opacity: 0.9, whiteSpace: 'nowrap' }}>
              {interval}
            </Typography>
          </Button>
        )
      })}
    </Box>
  )
}
