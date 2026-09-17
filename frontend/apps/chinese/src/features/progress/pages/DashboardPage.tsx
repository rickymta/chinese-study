import { useMemo } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { Alert, AlertTitle, Box, Button, Grid, Link, Skeleton, Stack, Typography } from '@mui/material'
import { useAuth } from '@af/auth'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useProgressOverview } from '../hooks'
import { detectBrowserTimeZone, isBrowserTimeZoneDifferent, todayHeading } from '../lib/labels'
import { buildTodayTasks } from '../lib/todayTasks'
import type { ProgressOverview } from '../types'
import { ActivityHeatmap } from '../components/ActivityHeatmap'
import { DailyGoalCard } from '../components/DailyGoalCard'
import { LessonProgressCard } from '../components/LessonProgressCard'
import { StreakCard } from '../components/StreakCard'
import { SystemStatusSection } from '../components/SystemStatusSection'
import { TodayTasks } from '../components/TodayTasks'
import { ToneAccuracyCard } from '../components/ToneAccuracyCard'
import { VocabularyCard } from '../components/VocabularyCard'
import { WritingProgressCard } from '../components/WritingProgressCard'

/** Lưới: một cột ở 375px, hai cột từ md (Grid MUI v9 `size`). */
const HALF = { xs: 12, md: 6 } as const

function DashboardSkeleton() {
  return (
    <Grid container spacing={2}>
      {Array.from({ length: 8 }, (_, i) => (
        <Grid key={i} size={HALF}>
          <Skeleton variant="rounded" height={i === 3 ? 220 : 150} />
        </Grid>
      ))}
    </Grid>
  )
}

/**
 * Nội dung bảng tổng quan theo thứ tự §5.3.4: chuỗi ngày → mục tiêu → việc hôm nay → lịch 90 ngày → từ vựng →
 * bài học → luyện viết → thanh điệu. Khối `null`/vắng ⇒ ẩn (R-PG7); số 0 ⇒ CTA trong thẻ.
 */
function DashboardBody({ overview }: { overview: ProgressOverview }) {
  const tasks = useMemo(() => buildTodayTasks(overview), [overview])
  const { srs, dailyGoal, vocabulary, lessons, writing, tone } = overview
  return (
    <Grid container spacing={2}>
      <Grid size={HALF}>
        <StreakCard streak={overview.streak} today={overview.today} />
      </Grid>
      {srs && dailyGoal && (
        <Grid size={HALF}>
          <DailyGoalCard goal={dailyGoal} srs={srs} />
        </Grid>
      )}
      <Grid size={HALF}>
        <TodayTasks tasks={tasks} />
      </Grid>
      <Grid size={HALF}>
        <ActivityHeatmap activity={overview.activity} today={overview.localDate} />
      </Grid>
      {vocabulary && (
        <Grid size={HALF}>
          <VocabularyCard vocabulary={vocabulary} />
        </Grid>
      )}
      {lessons && (
        <Grid size={HALF}>
          <LessonProgressCard lessons={lessons} />
        </Grid>
      )}
      {writing && (
        <Grid size={HALF}>
          <WritingProgressCard writing={writing} lastCompleted={lessons?.lastCompleted} />
        </Grid>
      )}
      {tone && (
        <Grid size={HALF}>
          <ToneAccuracyCard tone={tone} />
        </Grid>
      )}
    </Grid>
  )
}

/**
 * `/` (F11): trang chủ = bảng tổng quan tiến độ theo MÚI GIỜ HỒ SƠ — tiêu đề "Hôm nay, {thứ} {dd/MM}" lấy từ
 * `localDate` của server, không dùng ngày trình duyệt. Người không có `study.use` (tài khoản chỉ quản trị) thấy dải
 * giải thích thay vì bảng; khối trạng thái hệ thống chỉ hiện với `users.manage`.
 */
export function DashboardPage() {
  const { can } = useAuth()
  const canStudy = can(PERMISSIONS.STUDY_USE)
  const canManageUsers = can(PERMISSIONS.USERS_MANAGE)
  const query = useProgressOverview(canStudy)
  const browserTimeZone = useMemo(() => detectBrowserTimeZone(), [])
  const overview = query.data
  const tzHint = overview && isBrowserTimeZoneDifferent(overview.timeZone, browserTimeZone)

  return (
    <PageContainer
      title={overview ? todayHeading(overview.localDate) : canStudy && query.isLoading ? <Skeleton width={220} /> : 'Trang chủ'}
      maxWidth={1100}
    >
      <Stack sx={{ gap: 2 }}>
        {tzHint && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: -1.5 }}>
            Ngày học tính theo múi giờ hồ sơ ({overview.timeZone}) —{' '}
            <Link component={RouterLink} to="/ho-so">
              đổi ở Hồ sơ
            </Link>
            .
          </Typography>
        )}

        {!canStudy ? (
          <Alert severity="info">
            <AlertTitle>Chế độ chỉ xem</AlertTitle>
            Tài khoản của bạn chưa có quyền “Học tập” nên bảng tổng quan tiến độ không hiện. Bạn vẫn dùng được các mục quản
            trị được cấp trong menu; cần học thì liên hệ quản trị viên để được gán vai trò Học viên.
          </Alert>
        ) : query.isError ? (
          <Alert
            severity={parseApiError(query.error).status === 503 ? 'warning' : 'error'}
            action={
              <Button color="inherit" size="small" onClick={() => void query.refetch()} disabled={query.isFetching}>
                Thử lại
              </Button>
            }
          >
            <AlertTitle>Không tải được tổng quan</AlertTitle>
            {parseApiError(query.error).message}
          </Alert>
        ) : query.isLoading || !overview ? (
          <DashboardSkeleton />
        ) : (
          <DashboardBody overview={overview} />
        )}

        {canManageUsers && (
          <Box sx={{ mt: 1 }}>
            <SystemStatusSection />
          </Box>
        )}
      </Stack>
    </PageContainer>
  )
}
