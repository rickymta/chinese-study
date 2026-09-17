import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Typography } from '@mui/material'
import DrawOutlinedIcon from '@mui/icons-material/DrawOutlined'
import type { ProgressLastCompletedLesson, ProgressWriting } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  writing: ProgressWriting
  /** Bài hoàn thành gần nhất — để gợi ý "chữ bài vừa học chưa luyện" (R-PG9 mục 4). */
  lastCompleted?: ProgressLastCompletedLesson | null
}

function Stat({ label, value, color }: { label: string; value: number; color?: string }) {
  return (
    <Box sx={{ flex: 1, minWidth: 0 }}>
      <Typography variant="h5" component="p" sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums', color }}>
        {value}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
    </Box>
  )
}

/** Luyện viết: đã luyện / đã thuộc / cần luyện trên tổng bộ HSK 1; CTA khi chưa viết chữ nào; lối tắt tới chữ yếu. */
export function WritingProgressCard({ writing, lastCompleted }: Props) {
  const { practicedChars, masteredChars, weakChars, totalChars } = writing
  const unpracticed = lastCompleted && lastCompleted.unpracticedChars > 0 ? lastCompleted : null

  return (
    <DashboardCard title="Luyện viết" icon={<DrawOutlinedIcon />}>
      <Box sx={{ display: 'flex', gap: 2 }}>
        <Stat label={`đã luyện / ${totalChars} chữ`} value={practicedChars} />
        <Stat label="đã thuộc" value={masteredChars} color="success.main" />
        <Stat label="cần luyện" value={weakChars} color={weakChars > 0 ? 'warning.main' : undefined} />
      </Box>

      {practicedChars === 0 ? (
        <Button component={RouterLink} to="/luyen-viet" variant="contained" sx={{ minHeight: 44, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}>
          Viết chữ đầu tiên
        </Button>
      ) : (
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
          {weakChars > 0 && (
            <Button component={RouterLink} to="/luyen-viet?tab=can-luyen" variant="outlined" sx={{ minHeight: 44 }}>
              Luyện {weakChars} chữ yếu
            </Button>
          )}
          {unpracticed && (
            <Button
              component={RouterLink}
              to={`/luyen-viet?tab=bai-hoc&bai=${encodeURIComponent(unpracticed.slug)}`}
              variant="outlined"
              sx={{ minHeight: 44 }}
            >
              {unpracticed.unpracticedChars} chữ bài "{unpracticed.title}"
            </Button>
          )}
        </Box>
      )}
    </DashboardCard>
  )
}
