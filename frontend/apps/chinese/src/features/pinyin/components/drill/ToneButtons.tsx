import { Box, ButtonBase, Typography, useTheme } from '@mui/material'
import { DRILL_TONES, type DrillTone } from '../../types'

export const TONE_GLYPH: Record<DrillTone, string> = { 1: 'ˉ', 2: 'ˊ', 3: 'ˇ', 4: 'ˋ' }

export interface ToneButtonsProps {
  /** Thanh đã chọn (nếu có). */
  value: DrillTone | null
  onSelect: (tone: DrillTone) => void
  disabled?: boolean
  /** Ở trạng thái chấm: tô xanh thanh đúng, đỏ thanh chọn sai. */
  correctTone?: DrillTone
  label?: string
}

/** 4 nút thanh lớn (≥ 64px cao) — nằm trong tầm ngón cái ở 375px; nhãn "1 ˉ", "2 ˊ", "3 ˇ", "4 ˋ". */
export function ToneButtons({ value, onSelect, disabled, correctTone, label }: ToneButtonsProps) {
  const theme = useTheme()
  const graded = correctTone !== undefined
  return (
    <Box>
      {label && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
          {label}
        </Typography>
      )}
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1 }}>
        {DRILL_TONES.map((t) => {
          const isChosen = value === t
          const isCorrect = graded && correctTone === t
          const isWrongChoice = graded && isChosen && correctTone !== t
          const borderColor = isCorrect ? theme.palette.success.main : isWrongChoice ? theme.palette.error.main : isChosen ? theme.palette.primary.main : theme.palette.divider
          const bg = isCorrect ? theme.palette.success.main : isWrongChoice ? theme.palette.error.main : isChosen ? theme.palette.primary.main : 'transparent'
          const fg = isCorrect || isWrongChoice || isChosen ? theme.palette.getContrastText(bg === 'transparent' ? theme.palette.background.paper : bg) : theme.palette.text.primary
          return (
            <ButtonBase
              key={t}
              onClick={() => onSelect(t)}
              disabled={disabled}
              aria-label={`Thanh ${t}`}
              aria-pressed={isChosen}
              sx={{
                minHeight: 64,
                borderRadius: 2,
                border: 2,
                borderColor,
                bgcolor: bg,
                color: fg,
                display: 'flex',
                flexDirection: 'column',
                gap: 0,
                fontSize: 22,
                fontWeight: 700,
                transition: 'background-color .15s, border-color .15s',
                '&.Mui-disabled': { opacity: graded ? 1 : 0.5 },
              }}
            >
              <span>{t}</span>
              <Typography component="span" sx={{ fontSize: 20, lineHeight: 1 }} aria-hidden>
                {TONE_GLYPH[t]}
              </Typography>
            </ButtonBase>
          )
        })}
      </Box>
    </Box>
  )
}
