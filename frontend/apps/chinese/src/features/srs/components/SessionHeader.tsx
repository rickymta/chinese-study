import { Box, IconButton, LinearProgress, Tooltip, Typography } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'

export interface SessionHeaderProps {
  /** Số thẻ đã chấm trong phiên. */
  done: number
  /** Tổng thẻ trong bộ bài hiện tại (tăng khi tải thêm). */
  total: number
  onClose: () => void
}

/** Đầu màn ôn: nút đóng (X) + thanh tiến độ + "đã ôn/tổng". */
export function SessionHeader({ done, total, onClose }: SessionHeaderProps) {
  const percent = total > 0 ? Math.min(100, Math.round((done / total) * 100)) : 0
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, width: '100%' }}>
      <Tooltip title="Kết thúc phiên">
        <IconButton aria-label="Kết thúc phiên" onClick={onClose} edge="start" sx={{ ml: -0.5 }}>
          <CloseIcon />
        </IconButton>
      </Tooltip>
      <LinearProgress
        variant="determinate"
        value={percent}
        aria-label="Tiến độ phiên"
        sx={{ flex: 1, height: 8, borderRadius: 4 }}
      />
      <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'nowrap', fontVariantNumeric: 'tabular-nums' }}>
        {done}/{total}
      </Typography>
    </Box>
  )
}
