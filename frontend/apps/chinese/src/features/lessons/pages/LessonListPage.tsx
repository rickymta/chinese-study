import { Alert, Box, Skeleton, Stack, Typography } from '@mui/material'
import { PageContainer } from '@af/ui'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { useLessons } from '../hooks'
import { LessonCard } from '../components/LessonCard'
import type { LessonListResponse } from '@af/chinese-kit'

function ListSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Skeleton variant="rounded" height={160} />
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} variant="rounded" height={150} />
        ))}
      </Box>
    </Stack>
  )
}

function ListBody({ data }: { data: LessonListResponse }) {
  const items = [...data.items].sort((a, b) => a.orderIndex - b.orderIndex || a.title.localeCompare(b.title, 'vi'))
  if (items.length === 0) {
    return <Alert severity="info">Chưa có bài học nào được xuất bản.</Alert>
  }
  const next = data.nextLessonSlug ? items.find((l) => l.slug === data.nextLessonSlug) : undefined
  const completedAll = !next && items.every((l) => l.progress?.status === 'completed')

  return (
    <Stack sx={{ gap: 3 }}>
      {next ? (
        <Box>
          <Typography component="h2" variant="subtitle2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5, mb: 1 }}>
            Bài tiếp theo
          </Typography>
          <LessonCard lesson={next} highlighted />
        </Box>
      ) : (
        completedAll && <Alert severity="success">Bạn đã hoàn thành tất cả bài học hiện có. Hãy tiếp tục ôn tập để giữ từ vựng.</Alert>
      )}

      <Box>
        <Typography component="h2" variant="subtitle2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5, mb: 1 }}>
          Tất cả bài học ({items.length})
        </Typography>
        {/* Một cột ở 375px, hai cột từ md — thẻ cao bằng nhau trong một hàng. */}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
          {items.map((l) => (
            <LessonCard key={l.id} lesson={l} highlighted={l.slug === next?.slug} />
          ))}
        </Box>
      </Box>
    </Stack>
  )
}

/** `/bai-hoc` (F9, cần `study.use`): thẻ bài theo `orderIndex`, "Bài tiếp theo" nổi bật (R-LS4), không khoá tuần tự. */
export function LessonListPage() {
  const query = useLessons()
  return (
    <PageContainer title="Bài học" maxWidth={960}>
      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !query.data ? (
        <ListSkeleton />
      ) : (
        <ListBody data={query.data} />
      )}
    </PageContainer>
  )
}
