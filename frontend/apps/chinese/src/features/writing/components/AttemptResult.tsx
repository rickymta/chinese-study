import { Alert, Box, Button, Card, CardContent, CircularProgress, Typography } from '@mui/material'
import { isApiError } from '@af/api'
import ReplayIcon from '@mui/icons-material/Replay'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import SkipNextIcon from '@mui/icons-material/SkipNext'
import { LangText } from '@af/ui'
import type { AttemptSummary } from './HanziWriterBoard'
import { MasteryChip } from './CharacterInfoCard'
import type { RecordWritingAttemptResponse, WritingMode } from '../types'

export type SubmissionState =
  | { status: 'sending' }
  | { status: 'ok'; result: RecordWritingAttemptResponse }
  | { status: 'error'; error: unknown; retryable: boolean }

export interface AttemptResultProps {
  hanzi: string
  mode: WritingMode
  summary: AttemptSummary
  submission: SubmissionState
  onRetrySend: () => void
  onWriteAgain: () => void
  /** Tô theo ⇒ "Bước kế" sang Tự viết; Tự viết không có. */
  onNextStep?: () => void
  /** Có bộ (`tu=`) ⇒ "Chữ tiếp" / "Về danh sách". */
  onNextChar?: () => void
  nextCharLabel?: string
}

function describeError(error: unknown): string {
  if (isApiError(error)) {
    if (error.status === 422) return 'Chữ này chưa có trong kho từ — không ghi được kết quả.'
    return error.message
  }
  return error instanceof Error ? error.message : 'Không gửi được kết quả.'
}

/**
 * Kết quả một lượt (§5.3.2): số lỗi, số gợi ý, trạng thái thuộc chữ từ server (mastered mới ⇒ chúc mừng), trạng thái
 * gửi (đang gửi / lỗi + "Gửi lại" cùng `clientAttemptId`), nút "Viết lại" / "Bước kế" / "Chữ tiếp".
 */
export function AttemptResult({ hanzi, mode, summary, submission, onRetrySend, onWriteAgain, onNextStep, onNextChar, nextCharLabel }: AttemptResultProps) {
  const clean = summary.totalMistakes === 0 && summary.hintsUsed === 0
  const result = submission.status === 'ok' ? submission.result : null
  const headline = result?.becameMastered
    ? 'Đã thuộc chữ!'
    : clean
      ? mode === 'recall'
        ? 'Tự viết sạch!'
        : 'Tô đúng hết!'
      : 'Đã viết xong'

  return (
    <Card variant="outlined" sx={{ borderColor: result?.becameMastered ? 'success.main' : 'divider' }} aria-live="polite">
      <CardContent sx={{ px: { xs: 1.5, sm: 2 } }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography component="h3" variant="h6" sx={{ fontWeight: 700 }}>
              {headline}{' '}
              <LangText lang="zh-CN" component="span" sx={{ fontSize: 26, fontWeight: 400 }}>
                {hanzi}
              </LangText>
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {summary.totalStrokes} nét · {summary.totalMistakes} lỗi · {summary.hintsUsed} gợi ý
              {mode === 'recall' && !clean && ' — lần này chưa tính là viết sạch.'}
            </Typography>
          </Box>
          {result && <MasteryChip status={result.stats.masteryStatus} size="medium" />}
        </Box>

        {result?.becameMastered && (
          <Alert severity="success" sx={{ mt: 1.5 }}>
            Bạn đã tự viết sạch chữ này ở 2 ngày khác nhau. Chữ được đánh dấu đã thuộc.
          </Alert>
        )}
        {result && !result.becameMastered && mode === 'recall' && result.isClean && result.stats.cleanRecallDays < 2 && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Viết sạch ngày {result.stats.cleanRecallDays}/2 — mai tự viết sạch lần nữa là thuộc chữ.
          </Typography>
        )}

        {submission.status === 'sending' && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1.5 }}>
            <CircularProgress size={18} />
            <Typography variant="body2" color="text.secondary">
              Đang ghi kết quả…
            </Typography>
          </Box>
        )}
        {submission.status === 'error' && (
          <Alert
            severity="error"
            sx={{ mt: 1.5 }}
            action={
              submission.retryable ? (
                <Button color="inherit" size="small" onClick={onRetrySend}>
                  Gửi lại
                </Button>
              ) : undefined
            }
          >
            {describeError(submission.error)}
          </Alert>
        )}

        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 2 }}>
          <Button variant="outlined" startIcon={<ReplayIcon />} onClick={onWriteAgain} sx={{ minHeight: 44 }}>
            Viết lại
          </Button>
          {onNextStep && (
            <Button variant="contained" endIcon={<ArrowForwardIcon />} onClick={onNextStep} sx={{ minHeight: 44 }}>
              Bước kế: Tự viết
            </Button>
          )}
          {onNextChar && (
            <Button variant={onNextStep ? 'outlined' : 'contained'} endIcon={<SkipNextIcon />} onClick={onNextChar} sx={{ minHeight: 44 }}>
              {nextCharLabel ?? 'Chữ tiếp'}
            </Button>
          )}
        </Box>
      </CardContent>
    </Card>
  )
}
