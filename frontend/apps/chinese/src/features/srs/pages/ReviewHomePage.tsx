import { Link as RouterLink } from 'react-router-dom'
import { Alert, Box, Button, Card, CardContent, Link, Skeleton, Stack, Typography } from '@mui/material'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { PageContainer } from '@af/ui'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { useSrsSummary } from '../hooks'
import type { SrsSummary } from '../types'

/** Tổng số từ trong lộ trình HSK 3.0 cấp 1 (D1) — mẫu số của "Từ vững". */
const PATH_TOTAL = 500

/** `HH:mm dd/MM` theo múi giờ người học (`summary.timeZone`); múi giờ lạ ⇒ múi giờ trình duyệt. */
function formatNextDue(iso: string, timeZone: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const opts: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', hour12: false }
  try {
    return new Intl.DateTimeFormat('vi-VN', { ...opts, timeZone }).format(d)
  } catch {
    return new Intl.DateTimeFormat('vi-VN', opts).format(d)
  }
}

function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card variant="outlined" sx={{ flex: 1 }}>
      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
          {label}
        </Typography>
        <Typography variant="h4" component="p" sx={{ fontWeight: 700, lineHeight: 1.2, fontVariantNumeric: 'tabular-nums' }}>
          {value}
        </Typography>
        {hint && (
          <Typography variant="caption" color="text.secondary">
            {hint}
          </Typography>
        )}
      </CardContent>
    </Card>
  )
}

function HomeBody({ summary }: { summary: SrsSummary }) {
  const toStart = summary.dueNow + summary.newAvailableToday
  const limitReached = summary.reviewLimitRemaining === 0 && summary.dueToday > 0
  const nextDue = summary.nextDueAt ? formatNextDue(summary.nextDueAt, summary.timeZone) : ''

  return (
    <Stack sx={{ gap: 2 }}>
      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 1.5 }}>
        <StatCard label="Đến hạn hôm nay" value={String(summary.dueToday)} />
        <StatCard
          label="Từ mới còn học được"
          value={`${summary.newAvailableToday}/${summary.dailyNewCards}`}
          hint={summary.newIntroducedToday > 0 ? `Đã học ${summary.newIntroducedToday} từ mới hôm nay` : undefined}
        />
        <StatCard label="Đã ôn hôm nay" value={String(summary.reviewedToday)} />
      </Box>

      {limitReached && (
        <Alert severity="info">Đã đạt giới hạn {summary.dailyReviewLimit} lượt ôn hôm nay — thẻ còn lại sẽ chờ sang ngày mai.</Alert>
      )}

      {toStart > 0 ? (
        <Button
          component={RouterLink}
          to="/on-tap/phien"
          variant="contained"
          size="large"
          fullWidth
          startIcon={<PlayArrowIcon />}
          sx={{ minHeight: 56, fontSize: 18 }}
        >
          Bắt đầu ôn ({toStart})
        </Button>
      ) : (
        <Card sx={{ bgcolor: 'success.main', color: 'success.contrastText' }}>
          <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <CheckCircleOutlinedIcon fontSize="large" />
            <Box>
              <Typography variant="h6" component="p" sx={{ fontWeight: 700 }}>
                Hôm nay xong rồi!
              </Typography>
              <Typography variant="body2">
                {nextDue ? `Lượt ôn kế tiếp: ${nextDue}` : 'Chưa có thẻ nào chờ — thêm từ ở Từ điển hoặc chờ từ mới ngày mai.'}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      )}

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Typography variant="body2" color="text.secondary">
          Từ vững: <strong>{summary.matureCards}</strong>/{PATH_TOTAL}
          {summary.totalCards > 0 && ` · đang ôn ${summary.totalCards} thẻ`}
        </Typography>
        <Link component={RouterLink} to="/ho-so?tab=hoc-tap" variant="body2">
          Cài đặt học tập
        </Link>
      </Box>
    </Stack>
  )
}

function HomeSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 1.5 }}>
        <Skeleton variant="rounded" height={88} sx={{ flex: 1 }} />
        <Skeleton variant="rounded" height={88} sx={{ flex: 1 }} />
        <Skeleton variant="rounded" height={88} sx={{ flex: 1 }} />
      </Box>
      <Skeleton variant="rounded" height={56} />
      <Skeleton variant="text" width={200} />
    </Stack>
  )
}

/** `/on-tap` (F7.2, cần `study.use`): ba ô số + nút bắt đầu phiên; `summary` làm mới khi quay lại tab. */
export function ReviewHomePage() {
  const query = useSrsSummary()
  return (
    <PageContainer title="Ôn tập" maxWidth={720}>
      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !query.data ? (
        <HomeSkeleton />
      ) : (
        <HomeBody summary={query.data} />
      )}
    </PageContainer>
  )
}
