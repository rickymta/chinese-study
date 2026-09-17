import { Box, Chip, Typography } from '@mui/material'
import LocalFireDepartmentOutlinedIcon from '@mui/icons-material/LocalFireDepartmentOutlined'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { longestLabel, streakMessage } from '../lib/labels'
import type { ProgressStreak, ProgressToday } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  streak: ProgressStreak
  today: ProgressToday
}

/**
 * Chuỗi ngày học (R-PG3/R-PG4): số ngày lớn + "Dài nhất: N"; chưa học hôm nay ⇒ nhắc "Học 1 hoạt động để giữ chuỗi".
 * Không đóng băng, không bù ngày (D8) — chuỗi hiện tại vẫn còn cho tới hết ngày hôm nay theo múi giờ hồ sơ.
 */
export function StreakCard({ streak, today }: Props) {
  const active = streak.current > 0
  return (
    <DashboardCard
      title="Chuỗi ngày học"
      icon={<LocalFireDepartmentOutlinedIcon />}
      aside={
        streak.studiedToday ? (
          <Chip size="small" color="success" icon={<CheckCircleOutlinedIcon />} label="Hôm nay đã học" />
        ) : (
          <Chip size="small" variant="outlined" label="Hôm nay chưa học" />
        )
      }
    >
      <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1 }}>
        <Typography
          component="p"
          sx={{
            fontSize: { xs: 48, sm: 56 },
            fontWeight: 800,
            lineHeight: 1,
            fontVariantNumeric: 'tabular-nums',
            color: active ? (streak.studiedToday ? 'success.main' : 'warning.main') : 'text.disabled',
          }}
          aria-label={`Chuỗi hiện tại ${streak.current} ngày`}
        >
          {streak.current}
        </Typography>
        <Typography variant="h6" component="span" color="text.secondary">
          ngày liên tiếp
        </Typography>
      </Box>
      <Typography variant="body2" color={streak.studiedToday ? 'text.secondary' : 'warning.main'} sx={{ fontWeight: streak.studiedToday ? 400 : 600 }}>
        {streakMessage(streak)}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {longestLabel(streak)}
        {today.activityCount > 0 && ` · hôm nay ${today.activityCount} lượt`}
      </Typography>
    </DashboardCard>
  )
}
