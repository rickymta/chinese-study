import { useMemo } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Card, CardContent, Chip, Divider, Stack, Typography } from '@mui/material'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import CancelIcon from '@mui/icons-material/Cancel'
import ReplayIcon from '@mui/icons-material/Replay'
import StyleOutlinedIcon from '@mui/icons-material/StyleOutlined'
import EmojiEventsOutlinedIcon from '@mui/icons-material/EmojiEventsOutlined'
import DrawOutlinedIcon from '@mui/icons-material/DrawOutlined'
import { SpeakButton } from '@/components/speech/SpeakButton'
import type { QuizOption, QuizQuestion, QuizQuestionResult, QuizResult } from '../../types'
import { minCorrectToPass } from '../../lib/quizScore'
import { InlineZh } from '../InlineZh'
import { OptionText, QuestionPrompt } from './QuestionParts'

export interface QuizResultViewProps {
  result: QuizResult
  /** Ảnh chụp câu hỏi của lượt (không đọc lại từ query — admin có thể vừa sửa quiz). */
  questions: QuizQuestion[]
  onRetry: () => void
  /** Slug bài — có thì hiện nút "Luyện viết chữ của bài" (F8) khi hoàn thành lần đầu. */
  lessonSlug?: string
}

function findOption(q: QuizQuestion | undefined, id: string): QuizOption | undefined {
  return q?.options.find((o) => o.id === id)
}

function ResultRow({ index, question, r }: { index: number; question: QuizQuestion | undefined; r: QuizQuestionResult }) {
  const chosen = findOption(question, r.optionId)
  const correct = findOption(question, r.correctOptionId)
  return (
    <Box sx={{ py: 1.5 }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
        {r.correct ? <CheckCircleIcon color="success" sx={{ mt: 0.25 }} /> : <CancelIcon color="error" sx={{ mt: 0.25 }} />}
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="caption" color="text.secondary">
            Câu {index + 1}
            {question?.type === 'listen_choice' && ' · nghe'}
          </Typography>
          {question ? (
            <QuestionPrompt question={question} compact />
          ) : (
            <Typography variant="body2" color="text.secondary">
              (Câu hỏi đã thay đổi)
            </Typography>
          )}
          {question?.type === 'listen_choice' && question.audioText && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mt: 0.5 }}>
              <OptionText option={{ id: 'audio', text: question.audioText, lang: 'zh' }} size="sm" />
              <SpeakButton text={question.audioText} size="small" ariaLabel="Nghe" />
            </Box>
          )}
          <Stack sx={{ gap: 0.25, mt: 0.75 }}>
            <Typography variant="body2" component="div" sx={{ display: 'flex', gap: 0.75, alignItems: 'baseline', flexWrap: 'wrap' }}>
              <Typography component="span" variant="body2" color="text.secondary" sx={{ whiteSpace: 'nowrap' }}>
                Bạn chọn:
              </Typography>
              {chosen ? <OptionText option={chosen} size="sm" /> : <span>—</span>}
            </Typography>
            {!r.correct && (
              <Typography variant="body2" component="div" sx={{ display: 'flex', gap: 0.75, alignItems: 'baseline', flexWrap: 'wrap' }}>
                <Typography component="span" variant="body2" color="success.main" sx={{ whiteSpace: 'nowrap', fontWeight: 600 }}>
                  Đáp án đúng:
                </Typography>
                {correct ? <OptionText option={correct} size="sm" /> : <span>—</span>}
              </Typography>
            )}
            {r.explanation && <InlineZh text={r.explanation} variant="body2" color="text.secondary" sx={{ mt: 0.25 }} />}
          </Stack>
        </Box>
      </Box>
    </Box>
  )
}

/**
 * Kết quả lượt quiz (§5.3.1): điểm lớn, Đạt/Chưa đạt kèm ngưỡng, khối chúc mừng khi `firstCompletion`
 * ("Đã thêm N từ vào ôn tập" chỉ khi N > 0 — phát lại cùng `clientAttemptId` trả 0), từng câu với đáp án đã
 * chọn / đáp án đúng / lời giải, nút "Làm lại". F8 thêm nút "Luyện viết chữ của bài" ở đây.
 */
export function QuizResultView({ result, questions, onRetry, lessonSlug }: QuizResultViewProps) {
  const byId = useMemo(() => new Map(questions.map((q) => [q.id, q])), [questions])
  const needed = minCorrectToPass(result.total, result.passThresholdPercent)

  return (
    <Stack sx={{ gap: 2 }}>
      <Card
        sx={{
          bgcolor: result.passed ? 'success.main' : 'background.paper',
          color: result.passed ? 'success.contrastText' : 'text.primary',
          border: result.passed ? 0 : 1,
          borderColor: 'divider',
        }}
      >
        <CardContent sx={{ textAlign: 'center' }}>
          <Typography variant="h2" component="p" sx={{ fontWeight: 800, lineHeight: 1.1, fontVariantNumeric: 'tabular-nums' }}>
            {result.scorePercent}%
          </Typography>
          <Typography variant="body1" sx={{ mt: 0.5 }}>
            Đúng {result.correct}/{result.total} câu
          </Typography>
          <Chip
            sx={{ mt: 1.5, fontWeight: 700, bgcolor: result.passed ? 'success.contrastText' : undefined, color: result.passed ? 'success.main' : undefined }}
            color={result.passed ? undefined : 'default'}
            label={result.passed ? 'Đạt' : 'Chưa đạt'}
          />
          <Typography variant="body2" sx={{ mt: 1, opacity: 0.9 }}>
            Ngưỡng hoàn thành {result.passThresholdPercent}% (đúng ít nhất {needed}/{result.total} câu).
            {!result.passed && ' Đọc lại hội thoại và từ vựng rồi thử lại nhé.'}
          </Typography>
        </CardContent>
      </Card>

      {result.firstCompletion && (
        <Card variant="outlined" sx={{ borderColor: 'primary.main' }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <EmojiEventsOutlinedIcon color="primary" fontSize="large" />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="h6" component="p" sx={{ fontWeight: 700 }}>
                  Hoàn thành bài!
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {result.srsCardsAdded > 0
                    ? `Đã thêm ${result.srsCardsAdded} từ của bài vào ôn tập — thẻ mới sẽ được ưu tiên trong hạn mức mỗi ngày.`
                    : 'Từ của bài đã có trong ôn tập.'}
                </Typography>
              </Box>
            </Box>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1.5 }}>
              <Button component={RouterLink} to="/on-tap" variant="contained" startIcon={<StyleOutlinedIcon />}>
                Ôn tập ngay
              </Button>
              {lessonSlug && (
                <Button
                  component={RouterLink}
                  to={`/luyen-viet?tab=bai-hoc&bai=${encodeURIComponent(lessonSlug)}`}
                  variant="outlined"
                  startIcon={<DrawOutlinedIcon />}
                >
                  Luyện viết chữ của bài
                </Button>
              )}
            </Box>
          </CardContent>
        </Card>
      )}

      <Card variant="outlined">
        <CardContent sx={{ px: { xs: 1.5, sm: 2 } }}>
          <Typography component="h3" variant="subtitle1" sx={{ fontWeight: 700 }}>
            Từng câu
          </Typography>
          {result.results.map((r, i) => (
            <Box key={r.questionId}>
              {i > 0 && <Divider />}
              <ResultRow index={i} question={byId.get(r.questionId)} r={r} />
            </Box>
          ))}
        </CardContent>
      </Card>

      <Button variant={result.passed ? 'outlined' : 'contained'} size="large" startIcon={<ReplayIcon />} onClick={onRetry} sx={{ minHeight: 52 }} fullWidth>
        Làm lại
      </Button>
    </Stack>
  )
}
