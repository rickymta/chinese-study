import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Typography } from '@mui/material'
import TranslateOutlinedIcon from '@mui/icons-material/TranslateOutlined'
import type { ProgressVocabulary } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  vocabulary: ProgressVocabulary
}

/**
 * Từ vựng (R-PG8): `introduced/totalInPath`; thanh xếp chồng tự vẽ bằng Box: vững (success) + đang học (primary) +
 * chưa học (nền). Chưa có thẻ nào ⇒ CTA sang ôn tập.
 */
export function VocabularyCard({ vocabulary }: Props) {
  const { totalInPath, introduced, learning, mature } = vocabulary
  const denom = Math.max(totalInPath, introduced, 1)
  const maturePct = (mature / denom) * 100
  const learningPct = (learning / denom) * 100
  const percent = totalInPath > 0 ? Math.round((introduced / totalInPath) * 100) : 0

  return (
    <DashboardCard title="Từ vựng" icon={<TranslateOutlinedIcon />}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <Typography variant="h5" component="p" sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {introduced}
          <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>
            / {totalInPath} từ trong lộ trình
          </Typography>
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {percent}%
        </Typography>
      </Box>

      {/* Thanh xếp chồng: hai đoạn màu trên nền mờ; `role=img` + aria-label thay cho chú thích màu. */}
      <Box
        role="img"
        aria-label={`${mature} từ vững, ${learning} từ đang học, ${Math.max(0, totalInPath - introduced)} từ chưa học`}
        sx={{ display: 'flex', height: 10, borderRadius: 5, overflow: 'hidden', bgcolor: 'action.hover' }}
      >
        <Box sx={{ width: `${maturePct}%`, bgcolor: 'success.main', transition: 'width .3s' }} />
        <Box sx={{ width: `${learningPct}%`, bgcolor: 'primary.main', opacity: 0.6, transition: 'width .3s' }} />
      </Box>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <Legend color="success.main" label="Vững" value={mature} />
        <Legend color="primary.main" label="Đang học" value={learning} faded />
        <Legend color="action.hover" label="Chưa học" value={Math.max(0, totalInPath - introduced)} />
      </Box>

      {introduced === 0 && (
        <Button component={RouterLink} to="/on-tap" variant="outlined" sx={{ minHeight: 44, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}>
          Học từ đầu tiên
        </Button>
      )}
    </DashboardCard>
  )
}

function Legend({ color, label, value, faded }: { color: string; label: string; value: number; faded?: boolean }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
      <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: color, opacity: faded ? 0.6 : 1, flexShrink: 0 }} />
      <Typography variant="body2" color="text.secondary">
        {label}: <strong>{value}</strong>
      </Typography>
    </Box>
  )
}
