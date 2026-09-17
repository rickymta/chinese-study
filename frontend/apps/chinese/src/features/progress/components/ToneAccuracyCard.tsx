import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Chip, Typography } from '@mui/material'
import GraphicEqOutlinedIcon from '@mui/icons-material/GraphicEqOutlined'
import { toPercent } from '../lib/labels'
import { PINYIN_MIN_ANSWERED, TONE_DRILL_PATH } from '../lib/todayTasks'
import type { ProgressTone } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  tone: ProgressTone
}

/** Thanh điệu (K14): độ chính xác chung + chip thanh cần luyện; chưa làm bài nào ⇒ CTA làm bài đầu tiên. */
export function ToneAccuracyCard({ tone }: Props) {
  const percent = toPercent(tone.accuracy)
  const weak = tone.recommendedFocus
  const notEnough = tone.totalAnswered < PINYIN_MIN_ANSWERED

  return (
    <DashboardCard title="Thanh điệu" icon={<GraphicEqOutlinedIcon />}>
      {tone.totalAnswered === 0 ? (
        <>
          <Typography variant="body2" color="text.secondary">
            Chưa có bài luyện nghe thanh nào. Nghe được 4 thanh là nền tảng trước khi học từ.
          </Typography>
          <Button component={RouterLink} to={TONE_DRILL_PATH} variant="contained" sx={{ minHeight: 44, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}>
            Làm bài luyện thanh đầu tiên
          </Button>
        </>
      ) : (
        <>
          <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1 }}>
            <Typography
              variant="h5"
              component="p"
              sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}
              color={percent === null ? 'text.secondary' : percent >= 85 ? 'success.main' : percent >= 80 ? 'text.primary' : 'warning.main'}
            >
              {percent === null ? '—' : `${percent}%`}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              nghe đúng thanh · {tone.totalAnswered} câu
            </Typography>
          </Box>

          {weak.length > 0 ? (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, flexWrap: 'wrap' }}>
              <Typography variant="body2" color="text.secondary">
                Cần luyện:
              </Typography>
              {weak.map((t) => (
                <Chip key={t} size="small" color="warning" variant="outlined" label={`Thanh ${t}`} />
              ))}
            </Box>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {notEnough ? `Làm thêm ${PINYIN_MIN_ANSWERED - tone.totalAnswered} câu để có nhận xét theo từng thanh.` : 'Bốn thanh đều khá vững.'}
            </Typography>
          )}

          <Button component={RouterLink} to={TONE_DRILL_PATH} variant="outlined" sx={{ minHeight: 44, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}>
            {weak.length > 0 ? `Luyện thanh ${weak.join(', ')}` : 'Luyện thanh'}
          </Button>
        </>
      )}
    </DashboardCard>
  )
}
