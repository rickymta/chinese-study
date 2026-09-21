import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Divider, Stack, Typography } from '@mui/material'
import ReplayIcon from '@mui/icons-material/Replay'
import BarChartIcon from '@mui/icons-material/BarChart'
import { parseApiError } from '@af/utils'
import { Hanzi, Pinyin, SpeakButton } from '@af/chinese-kit'
import { DRILL_TONES, type SubmitToneDrillResponse } from '../../types'
import { summarizeByTone, type DrillOutcome } from '../../drill/drillTypes'

export interface DrillResultProps {
  outcome: DrillOutcome
  /** Kết quả server (201/200) — có thì ưu tiên số liệu server. */
  serverResult?: SubmitToneDrillResponse
  submitting: boolean
  submitError?: unknown
  onRetry: () => void
  onNewDrill: () => void
  onViewStats: () => void
}

/**
 * Kết quả phiên: điểm x/20, theo thanh, câu sai (nghe lại), nút "Làm bài mới"/"Xem thống kê". Gửi lỗi mạng/5xx ⇒
 * Alert + "Gửi lại" (cùng `clientSessionId` — idempotent); 422 ⇒ thông điệp server + chỉ "Làm bài mới".
 * Kết quả hiện từ client trong lúc chờ server.
 */
export function DrillResult({ outcome, serverResult, submitting, submitError, onRetry, onNewDrill, onViewStats }: DrillResultProps) {
  const total = outcome.answers.length
  const correct = outcome.answers.filter((a) => a.correct).length
  const byTone = summarizeByTone(outcome.answers)
  const wrong = outcome.answers.filter((a) => !a.correct)
  const err = submitError ? parseApiError(submitError) : null
  const isRejected = err?.status === 422 || err?.status === 400

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center' }}>
            <Typography variant="overline" color="text.secondary">
              Kết quả
            </Typography>
            <Typography variant="h2" component="p" sx={{ fontWeight: 800, lineHeight: 1 }}>
              {serverResult?.correct ?? correct}/{serverResult?.total ?? total}
            </Typography>
            <Typography color="text.secondary">
              {correct === total ? 'Tuyệt vời — đúng hết!' : correct >= total * 0.8 ? 'Khá tốt, xem lại các câu sai bên dưới.' : 'Chưa vững — nghe lại các câu sai rồi làm thêm một bài nữa.'}
            </Typography>
            {submitting ? (
              <Chip icon={<CircularProgress size={14} color="inherit" />} label="Đang lưu kết quả…" variant="outlined" />
            ) : serverResult ? (
              <Chip label={`Đã lưu · ngày học ${serverResult.localDate}`} color="success" variant="outlined" />
            ) : null}
          </Stack>

          <Divider sx={{ my: 2 }} />

          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1 }}>
            {DRILL_TONES.map((t) => {
              const s = serverResult?.byTone[String(t) as '1' | '2' | '3' | '4'] ?? byTone[t]
              return (
                <Box key={t} sx={{ textAlign: 'center', p: 1, border: 1, borderColor: 'divider', borderRadius: 2 }}>
                  <Typography variant="caption" color="text.secondary">
                    Thanh {t}
                  </Typography>
                  <Typography sx={{ fontWeight: 700 }}>
                    {s.total === 0 ? '—' : `${s.correct}/${s.total}`}
                  </Typography>
                </Box>
              )
            })}
          </Box>
        </CardContent>
      </Card>

      {err && (
        <Alert
          severity="error"
          action={
            !isRejected && (
              <Button color="inherit" size="small" onClick={onRetry} disabled={submitting}>
                Gửi lại
              </Button>
            )
          }
        >
          {isRejected ? `Máy chủ từ chối kết quả: ${err.message}` : `Chưa lưu được kết quả: ${err.message}`}
        </Alert>
      )}

      {wrong.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
              Câu sai ({wrong.length})
            </Typography>
            <Stack divider={<Divider />} spacing={1}>
              {wrong.map((a, idx) => (
                <Box key={idx} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 0.5 }}>
                  <Hanzi size="lg" sx={{ minWidth: 56 }}>
                    {a.item.parts.map((p) => p.hanzi).join('')}
                  </Hanzi>
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Pinyin value={a.item.parts.map((p) => `${p.syllable}${p.tone}`).join(' ')} sx={{ fontWeight: 600 }} />
                    <Typography variant="body2" color="text.secondary">
                      Đúng: thanh {a.item.parts.map((p) => p.tone).join('–')} · bạn chọn: thanh {a.answered.join('–')}
                    </Typography>
                  </Box>
                  <SpeakButton text={a.item.parts.map((p) => p.hanzi).join('')} />
                </Box>
              ))}
            </Stack>
          </CardContent>
        </Card>
      )}

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        <Button variant="contained" startIcon={<ReplayIcon />} onClick={onNewDrill} sx={{ minHeight: 48, flex: { xs: 1, sm: 'none' } }}>
          Làm bài mới
        </Button>
        <Button variant="outlined" startIcon={<BarChartIcon />} onClick={onViewStats} sx={{ minHeight: 48, flex: { xs: 1, sm: 'none' } }}>
          Xem thống kê
        </Button>
      </Box>
    </Stack>
  )
}
