import { useMemo } from 'react'
import { Box, Typography, type SxProps, type Theme, type TypographyProps } from '@mui/material'
import { numberedToMarked, sandhiHints } from '@/lib/pinyin'

export interface PinyinProps {
  /** Pinyin dạng SỐ (`ni3 hao3`) — dạng lưu trữ; component tự đổi sang dạng dấu để hiển thị. */
  value: string
  /** Chữ Hán tương ứng (nhận diện 不/一 cho gợi ý biến điệu). */
  hanzi?: string
  /** Hiện gợi ý biến điệu (3-3, 不, 一) dưới dạng chú thích nhỏ. Mặc định tắt. */
  showSandhi?: boolean
  /** Nối liền âm tiết thành một từ (`Xī'ān`). */
  join?: boolean
  variant?: TypographyProps['variant']
  component?: TypographyProps['component']
  sx?: SxProps<Theme>
}

/**
 * Hiển thị pinyin dạng dấu từ chuỗi số (§5.3.C, R5-6). Gợi ý biến điệu chỉ là chú thích — app vẫn lưu thanh gốc (R-C3).
 */
export function Pinyin({ value, hanzi, showSandhi = false, join, variant = 'body1', component = 'span', sx }: PinyinProps) {
  const marked = useMemo(() => numberedToMarked(value, { join }), [value, join])
  const hints = useMemo(() => (showSandhi ? sandhiHints(value, hanzi) : []), [showSandhi, value, hanzi])

  if (hints.length === 0) {
    return (
      <Typography component={component} variant={variant} sx={sx}>
        {marked}
      </Typography>
    )
  }
  return (
    <Box component="span" sx={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'flex-start' }}>
      <Typography component={component} variant={variant} sx={sx}>
        {marked}
      </Typography>
      <Typography component="span" variant="caption" color="text.secondary">
        Biến điệu: {hints.map((h) => `âm tiết ${h.index + 1} ${h.text}`).join('; ')}
      </Typography>
    </Box>
  )
}
