import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, LinearProgress, Typography } from '@mui/material'
import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined'
import type { ProgressLessons } from '../types'
import { DashboardCard } from './DashboardCard'

interface Props {
  lessons: ProgressLessons
}

/** Bài học: `completed/published` + bài tiếp theo (R-LS4); chưa có bài published ⇒ thông báo; hết bài ⇒ chúc mừng. */
export function LessonProgressCard({ lessons }: Props) {
  const { published, completed, inProgress, next } = lessons
  const percent = published > 0 ? Math.round((completed / published) * 100) : 0
  const firstTime = completed === 0 && inProgress === 0

  return (
    <DashboardCard title="Bài học" icon={<AutoStoriesOutlinedIcon />}>
      {published === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Chưa có bài học nào được xuất bản.
        </Typography>
      ) : (
        <>
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', mb: 0.5 }}>
              <Typography variant="h5" component="p" sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                {completed}
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>
                  / {published} bài hoàn thành
                </Typography>
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {percent}%
              </Typography>
            </Box>
            <LinearProgress variant="determinate" value={percent} sx={{ height: 8, borderRadius: 4 }} aria-label="Tiến độ bài học" />
            {inProgress > 0 && (
              <Typography variant="caption" color="text.secondary">
                Đang học dở {inProgress} bài
              </Typography>
            )}
          </Box>

          {next ? (
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                {firstTime ? 'Bắt đầu từ' : 'Bài tiếp theo'}
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }} noWrap>
                {next.title}
              </Typography>
              <Button
                component={RouterLink}
                to={`/bai-hoc/${encodeURIComponent(next.slug)}`}
                variant={firstTime ? 'contained' : 'outlined'}
                sx={{ mt: 1, minHeight: 44, width: { xs: '100%', sm: 'auto' } }}
              >
                {firstTime ? 'Bắt đầu bài học đầu tiên' : 'Học tiếp'}
              </Button>
            </Box>
          ) : (
            <Typography variant="body2" color="success.main" sx={{ fontWeight: 600 }}>
              Bạn đã hoàn thành mọi bài hiện có — tiếp tục ôn tập để giữ từ vựng.
            </Typography>
          )}
        </>
      )}
    </DashboardCard>
  )
}
