import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, LinearProgress, Typography } from '@mui/material'
import FlagOutlinedIcon from '@mui/icons-material/FlagOutlined'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import type { ProgressDailyGoal, ProgressSrs } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  goal: ProgressDailyGoal
  srs: ProgressSrs
}

/**
 * Mục tiêu ngày (R-PG5, D8): hiển thị riêng, không ảnh hưởng chuỗi. Đạt khi hết thẻ đến hạn và đã học đủ thẻ mới
 * trong hạn mức; thanh tiến độ `done/total`, mẫu số 0 ⇒ coi như đạt. Nút "Ôn ngay" dẫn tới trang ôn tập.
 */
export function DailyGoalCard({ goal, srs }: Props) {
  const percent = goal.total === 0 ? 100 : Math.round((goal.done / goal.total) * 100)
  const remaining = srs.dueToday + srs.newAvailableToday
  return (
    <DashboardCard title="Mục tiêu hôm nay" icon={<FlagOutlinedIcon />}>
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', mb: 0.5 }}>
          <Typography variant="h5" component="p" sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
            {goal.done}/{goal.total}
            <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
              lượt
            </Typography>
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {percent}%
          </Typography>
        </Box>
        <LinearProgress
          variant="determinate"
          value={Math.min(100, percent)}
          color={goal.achieved ? 'success' : 'primary'}
          sx={{ height: 10, borderRadius: 5 }}
          aria-label="Tiến độ mục tiêu ngày"
        />
      </Box>

      {goal.achieved ? (
        <Typography variant="body2" color="success.main" sx={{ fontWeight: 600 }}>
          Đã đạt mục tiêu hôm nay — hết thẻ đến hạn, đã học đủ thẻ mới.
        </Typography>
      ) : (
        <Typography variant="body2" color="text.secondary">
          Còn <strong>{srs.dueToday}</strong> thẻ đến hạn · <strong>{srs.newAvailableToday}</strong> thẻ mới
          {srs.reviewedToday > 0 && ` · đã ôn ${srs.reviewedToday} lượt`}
        </Typography>
      )}

      {remaining > 0 && (
        <Button
          component={RouterLink}
          to="/on-tap"
          variant="contained"
          startIcon={<PlayArrowIcon />}
          sx={{ minHeight: 44, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}
        >
          Ôn ngay ({remaining})
        </Button>
      )}
    </DashboardCard>
  )
}
