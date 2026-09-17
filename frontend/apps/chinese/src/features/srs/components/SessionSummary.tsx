import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material'
import { RATING_META } from '../lib/ratings'
import { formatSessionDuration, type RatingCounts } from '../lib/sessionDeck'
import { SRS_RATINGS } from '../types'

export interface SessionSummaryProps {
  counts: RatingCounts
  durationMs: number
  /** Server còn thẻ (dueNow + newAvailableToday > 0) ⇒ hiện "Ôn tiếp". */
  canContinue: boolean
  onContinue: () => void
  onHome: () => void
}

/** Màn kết thúc phiên: tổng thẻ, số lượt theo 4 mức (thanh ngang màu), thời gian, nút về/ôn tiếp. */
export function SessionSummary({ counts, durationMs, canContinue, onContinue, onHome }: SessionSummaryProps) {
  const total = SRS_RATINGS.reduce((sum, r) => sum + counts[r], 0)
  const correct = total - counts.again
  return (
    <Stack sx={{ gap: 2, alignItems: 'stretch', width: '100%', maxWidth: 480, mx: 'auto' }}>
      <Box sx={{ textAlign: 'center' }}>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 700 }}>
          {total === 0 ? 'Không có thẻ để ôn' : 'Xong phiên ôn!'}
        </Typography>
        <Typography color="text.secondary">
          {total} lượt · {formatSessionDuration(durationMs)}
          {total > 0 && ` · nhớ ${Math.round((correct / total) * 100)}%`}
        </Typography>
      </Box>

      {total > 0 && (
        <Card variant="outlined">
          <CardContent>
            {/* Thanh ngang xếp chồng theo 4 mức. */}
            <Box
              role="img"
              aria-label={SRS_RATINGS.map((r) => `${RATING_META[r].label} ${counts[r]}`).join(', ')}
              sx={{ display: 'flex', height: 12, borderRadius: 6, overflow: 'hidden', bgcolor: 'action.hover', mb: 1.5 }}
            >
              {SRS_RATINGS.map((r) =>
                counts[r] > 0 ? (
                  <Box key={r} sx={{ flex: counts[r], bgcolor: `${RATING_META[r].color}.main` }} />
                ) : null,
              )}
            </Box>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1, textAlign: 'center' }}>
              {SRS_RATINGS.map((r) => (
                <Box key={r}>
                  <Typography variant="h6" component="p" sx={{ fontWeight: 700, color: `${RATING_META[r].color}.main` }}>
                    {counts[r]}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {RATING_META[r].label}
                  </Typography>
                </Box>
              ))}
            </Box>
          </CardContent>
        </Card>
      )}

      <Stack sx={{ gap: 1 }}>
        {canContinue && (
          <Button variant="contained" size="large" onClick={onContinue} sx={{ minHeight: 56 }}>
            Ôn tiếp
          </Button>
        )}
        <Button variant={canContinue ? 'outlined' : 'contained'} size="large" onClick={onHome} sx={{ minHeight: 56 }}>
          Về trang ôn tập
        </Button>
      </Stack>
    </Stack>
  )
}
