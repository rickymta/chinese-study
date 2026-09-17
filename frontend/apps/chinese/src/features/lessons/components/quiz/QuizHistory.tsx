import { Box, Chip, Skeleton, Stack, Typography } from '@mui/material'
import { useAuth } from '@af/auth'
import { useQuizAttempts } from '../../hooks'
import type { QuizAttemptSummary } from '../../types'

/** `HH:mm dd/MM/yyyy` theo múi giờ người học (claim `zoneinfo`); múi giờ lạ ⇒ múi giờ trình duyệt. */
function formatSubmittedAt(iso: string, timeZone: string | undefined): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const opts: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric', hour12: false }
  try {
    return new Intl.DateTimeFormat('vi-VN', { ...opts, timeZone }).format(d)
  } catch {
    return new Intl.DateTimeFormat('vi-VN', opts).format(d)
  }
}

function AttemptRow({ attempt, timeZone }: { attempt: QuizAttemptSummary; timeZone: string | undefined }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, py: 0.75, borderBottom: 1, borderColor: 'divider' }}>
      <Typography variant="body2" sx={{ flex: 1, minWidth: 0, fontVariantNumeric: 'tabular-nums' }}>
        {formatSubmittedAt(attempt.submittedAt, timeZone)}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap' }}>
        {attempt.correct}/{attempt.total} · {attempt.scorePercent}%
      </Typography>
      <Chip size="small" color={attempt.passed ? 'success' : 'default'} variant={attempt.passed ? 'filled' : 'outlined'} label={attempt.passed ? 'Đạt' : 'Chưa đạt'} />
    </Box>
  )
}

/**
 * 5 lần làm gần nhất của bài (`GET /api/lessons/{id}/quiz-attempts?limit=5`), hiện ở màn mở đầu quiz.
 * Lỗi tải ⇒ ẩn (không quan trọng bằng việc làm quiz); rỗng ⇒ "Chưa làm lần nào".
 */
export function QuizHistory({ lessonId, enabled = true }: { lessonId: string; enabled?: boolean }) {
  const { account } = useAuth()
  const timeZone = account?.timeZone || undefined
  const query = useQuizAttempts(lessonId, enabled)

  if (query.isError) return null
  return (
    <Stack sx={{ gap: 0.5 }}>
      <Typography variant="subtitle2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
        Lần làm gần đây
      </Typography>
      {query.isLoading || !query.data ? (
        <>
          <Skeleton variant="text" />
          <Skeleton variant="text" />
        </>
      ) : query.data.items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Chưa làm lần nào.
        </Typography>
      ) : (
        query.data.items.map((a) => <AttemptRow key={a.attemptId} attempt={a} timeZone={timeZone} />)
      )}
    </Stack>
  )
}
