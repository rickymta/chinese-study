import { Alert, Box, Button, Card, CardContent, LinearProgress, Skeleton, Stack, Typography } from '@mui/material'
import { parseApiError } from '@af/utils'
import { useToneStats } from '../hooks'
import { TONE_KEYS } from '../types'

const percent = (v: number | null) => (v === null ? null : Math.round(v * 100))

/**
 * Thẻ thống kê thanh (R5-13): 4 thanh dạng LinearProgress + a/b câu; nhầm lẫn tối đa 3 dòng; `g0Reached` ⇒ Alert
 * success (§1.3); chưa có phiên ⇒ lời mời làm bài đầu; 503 ⇒ Alert (không điều hướng).
 */
export function ToneStatsCard() {
  const stats = useToneStats()

  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
          Bạn nghe thanh nào tốt?
        </Typography>

        {stats.isPending ? (
          <Stack spacing={1}>
            {[0, 1, 2, 3].map((i) => (
              <Skeleton key={i} variant="rounded" height={28} />
            ))}
          </Stack>
        ) : stats.isError ? (
          (() => {
            const err = parseApiError(stats.error)
            return (
              <Alert
                severity={err.status === 503 ? 'warning' : 'error'}
                action={
                  <Button color="inherit" size="small" onClick={() => void stats.refetch()}>
                    Thử lại
                  </Button>
                }
              >
                {err.status === 503 ? 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.' : err.message}
              </Alert>
            )
          })()
        ) : stats.data.sessionsCount === 0 ? (
          <Typography color="text.secondary">Làm bài đầu tiên để xem bạn yếu thanh nào.</Typography>
        ) : (
          <Stack spacing={1.5}>
            {stats.data.g0Reached && (
              <Alert severity="success">Bạn đã nghe thanh khá vững — có thể bắt đầu học từ vựng.</Alert>
            )}
            {TONE_KEYS.map((k) => {
              const t = stats.data.byTone[k]
              const p = percent(t.accuracy)
              const weak = stats.data.recommendedFocus.includes(Number(k) as 1 | 2 | 3 | 4)
              return (
                <Box key={k}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.25 }}>
                    <Typography variant="body2" sx={{ fontWeight: weak ? 700 : 500 }} color={weak ? 'warning.main' : 'text.primary'}>
                      Thanh {k}
                      {weak ? ' · cần luyện' : ''}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {p === null ? 'chưa có dữ liệu' : `${p}% · ${t.correct}/${t.total} câu`}
                    </Typography>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={p ?? 0}
                    color={p === null ? 'inherit' : p >= 85 ? 'success' : p >= 80 ? 'primary' : 'warning'}
                    sx={{ height: 8, borderRadius: 4 }}
                    aria-label={`Độ chính xác thanh ${k}`}
                  />
                </Box>
              )
            })}

            {stats.data.confusions.length > 0 && (
              <Box>
                <Typography variant="body2" sx={{ fontWeight: 600, mb: 0.5 }}>
                  Hay nhầm
                </Typography>
                {stats.data.confusions.slice(0, 3).map((c) => (
                  <Typography key={`${c.expected}-${c.answered}`} variant="body2" color="text.secondary">
                    Bạn hay nghe <strong>thanh {c.expected}</strong> thành <strong>thanh {c.answered}</strong> ({c.count} lần)
                  </Typography>
                ))}
              </Box>
            )}

            <Typography variant="caption" color="text.secondary">
              Tính trên {stats.data.windowSize} câu gần nhất mỗi thanh · {stats.data.sessionsCount} phiên · {stats.data.totalAnswered} câu tổng cộng
              {stats.data.accuracy !== null ? ` · chính xác chung ${percent(stats.data.accuracy)}%` : ''}
            </Typography>
          </Stack>
        )}
      </CardContent>
    </Card>
  )
}
