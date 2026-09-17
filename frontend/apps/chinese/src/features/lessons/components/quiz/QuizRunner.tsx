import { useCallback, useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, Chip, LinearProgress, Stack, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import SendIcon from '@mui/icons-material/Send'
import { isApiError } from '@af/api'
import { StickyActionBar, useConfirm, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useChineseSpeech } from '@/components/speech/ChineseSpeech'
import { useSubmitQuiz } from '../../hooks'
import { buildAnswers, countUnanswered, minCorrectToPass } from '../../lib/quizScore'
import { shuffleOptions, type OptionOrder } from '../../lib/shuffle'
import { PASS_THRESHOLD_PERCENT, type LessonDetail, type QuizQuestion, type QuizResult } from '../../types'
import { QuizHistory } from './QuizHistory'
import { QuizQuestionView } from './QuizQuestionView'
import { QuizResultView } from './QuizResultView'

export interface QuizRunnerProps {
  lesson: LessonDetail
  slug: string
  /** Server báo `422 QUIZ_CHANGED` (admin vừa sửa quiz) ⇒ trang tải lại bài; runner về màn mở đầu. */
  onLessonChanged: () => void
  /** `false` khi tab Quiz chưa mở ⇒ lịch sử lần làm không tải. */
  active?: boolean
}

type Phase = 'intro' | 'running' | 'result'

/** Một lượt làm: id idempotent + mốc bắt đầu + ẢNH CHỤP câu hỏi và thứ tự lựa chọn (cố định suốt lượt). */
interface Attempt {
  clientAttemptId: string
  startedAt: string
  questions: QuizQuestion[]
  order: OptionOrder
}

/**
 * Máy trạng thái quiz (§5.3.1): intro → câu i/n → nộp → kết quả. Không chấm tại chỗ. `clientAttemptId` sinh
 * LÚC BẮT ĐẦU lượt (R-LS9) — lỗi mạng giữ đáp án, "Thử lại" gửi cùng id ⇒ server không tạo bản ghi thứ hai.
 * TTS câu nghe: chỉ gọi `speak` trong handler Bắt đầu/Tiếp/Trước (iOS Safari chặn ngoài thao tác người dùng).
 */
export function QuizRunner({ lesson, slug, onLessonChanged, active = true }: QuizRunnerProps) {
  const toast = useToast()
  const confirm = useConfirm()
  const { autoPlayAudio, canSpeak, speakZh } = useChineseSpeech()
  const submit = useSubmitQuiz(slug)

  const [phase, setPhase] = useState<Phase>('intro')
  const [attempt, setAttempt] = useState<Attempt | null>(null)
  const [answers, setAnswers] = useState<Map<string, string>>(() => new Map())
  const [index, setIndex] = useState(0)
  const [result, setResult] = useState<QuizResult | null>(null)
  /** Đã bấm "Nộp bài" ít nhất một lần ⇒ khoá đổi đáp án (gửi lại phải đúng bộ đáp án cũ — R-LS9). */
  const [locked, setLocked] = useState(false)

  const questions = attempt?.questions ?? []
  const total = questions.length
  const current = questions[index]
  const unanswered = useMemo(() => countUnanswered(questions, answers), [questions, answers])
  const listenCount = useMemo(() => lesson.quiz.filter((q) => q.type === 'listen_choice').length, [lesson.quiz])

  /** Tự phát câu nghe khi đi tới câu đó — gọi từ handler nút (không từ effect). */
  const autoplayFor = useCallback(
    (q: QuizQuestion | undefined) => {
      if (!q || q.type !== 'listen_choice' || !q.audioText || !autoPlayAudio || !canSpeak) return
      void speakZh(q.audioText).catch(() => undefined)
    },
    [autoPlayAudio, canSpeak, speakZh],
  )

  const start = useCallback(() => {
    const snapshot = lesson.quiz.map((q) => ({ ...q, options: [...q.options] }))
    const next: Attempt = {
      clientAttemptId: crypto.randomUUID(),
      startedAt: new Date().toISOString(),
      questions: snapshot,
      order: shuffleOptions(snapshot),
    }
    submit.reset()
    setAttempt(next)
    setAnswers(new Map())
    setIndex(0)
    setResult(null)
    setLocked(false)
    setPhase('running')
    autoplayFor(snapshot[0])
  }, [lesson.quiz, submit, autoplayFor])

  const goTo = useCallback(
    (i: number) => {
      if (i < 0 || i >= total) return
      setIndex(i)
      autoplayFor(questions[i])
    },
    [total, questions, autoplayFor],
  )

  const select = useCallback(
    (optionId: string) => {
      if (!current || locked) return
      setAnswers((prev) => {
        const next = new Map(prev)
        next.set(current.id, optionId)
        return next
      })
    },
    [current, locked],
  )

  const doSubmit = useCallback(() => {
    if (!attempt || submit.isPending) return
    setLocked(true)
    submit.mutate(
      {
        lessonId: lesson.id,
        body: { clientAttemptId: attempt.clientAttemptId, startedAt: attempt.startedAt, answers: buildAnswers(attempt.questions, answers) },
      },
      {
        onSuccess: (res) => {
          setResult(res)
          setPhase('result')
        },
        onError: (err) => {
          const code = isApiError(err) ? err.code : undefined
          if (code === 'QUIZ_CHANGED') {
            toast.warning('Bài vừa được cập nhật — tải lại quiz.')
            onLessonChanged()
            setAttempt(null)
            setPhase('intro')
          } else if (code === 'QUIZ_EMPTY') {
            toast.error('Bài này chưa có câu hỏi.')
            onLessonChanged()
            setAttempt(null)
            setPhase('intro')
          }
          // Lỗi khác (mạng, 5xx, 409...): giữ đáp án, hiện Alert + "Thử lại" cùng clientAttemptId.
        },
      },
    )
  }, [attempt, submit, lesson.id, answers, toast, onLessonChanged])

  const quit = useCallback(async () => {
    const ok = await confirm({
      title: 'Thoát lượt làm?',
      message: 'Đáp án đã chọn sẽ không được lưu. Bạn có thể làm lại từ đầu bất cứ lúc nào.',
      confirmText: 'Thoát',
      cancelText: 'Ở lại',
      tone: 'danger',
    })
    if (!ok) return
    submit.reset()
    setAttempt(null)
    setLocked(false)
    setPhase('intro')
  }, [confirm, submit])

  // Phím tắt khi đang làm: 1–4 chọn, ←/→ chuyển câu, Enter = Tiếp (hoặc Nộp ở câu cuối khi đã đủ).
  // Bỏ qua khi tab Quiz đang ẩn (runner vẫn mounted để giữ đáp án) hoặc có hộp thoại/ngăn kéo đang mở
  // (Enter trong hộp "Thoát lượt làm?" không được nộp bài).
  useEffect(() => {
    if (phase !== 'running' || !active) return
    const onKey = (e: KeyboardEvent) => {
      if (e.repeat) return
      const target = e.target as HTMLElement | null
      if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return
      if (document.querySelector('[role="dialog"], .MuiDrawer-root .MuiPaper-root')) return
      if (e.key >= '1' && e.key <= '4' && current) {
        const opt = attempt?.order[current.id]?.[Number(e.key) - 1]
        if (opt) {
          e.preventDefault()
          select(opt.id)
        }
      } else if (e.key === 'ArrowRight') {
        e.preventDefault()
        goTo(index + 1)
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault()
        goTo(index - 1)
      } else if (e.key === 'Enter') {
        e.preventDefault()
        if (index + 1 < total) goTo(index + 1)
        else if (unanswered === 0) doSubmit()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [phase, active, current, attempt, select, goTo, index, total, unanswered, doSubmit])

  // ─── Bài không có câu hỏi ───
  if (lesson.quiz.length === 0) {
    return <Alert severity="info">Bài này chưa có câu hỏi — hãy đọc nội dung và học từ vựng trước.</Alert>
  }

  // ─── Kết quả ───
  if (phase === 'result' && result && attempt) {
    return <QuizResultView result={result} questions={attempt.questions} onRetry={start} />
  }

  // ─── Màn mở đầu ───
  if (phase === 'intro' || !attempt || !current) {
    const best = lesson.progress?.bestScorePercent
    return (
      <Stack sx={{ gap: 2 }}>
        <Card variant="outlined">
          <CardContent>
            <Typography component="h3" variant="h6" sx={{ fontWeight: 700 }}>
              Kiểm tra bài
            </Typography>
            <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 1 }}>
              <Chip size="small" label={`${lesson.quiz.length} câu`} />
              {listenCount > 0 && <Chip size="small" variant="outlined" label={`${listenCount} câu nghe`} />}
              <Chip size="small" variant="outlined" label={`Đạt từ ${PASS_THRESHOLD_PERCENT}% (≥ ${minCorrectToPass(lesson.quiz.length)}/${lesson.quiz.length} câu)`} />
              {best != null && <Chip size="small" color={lesson.progress?.status === 'completed' ? 'success' : 'default'} label={`Điểm cao nhất ${best}%`} />}
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
              Đáp án chỉ hiện sau khi nộp bài. Đạt {PASS_THRESHOLD_PERCENT}% trở lên là hoàn thành bài — từ của bài sẽ được thêm vào ôn tập.
            </Typography>
            {listenCount > 0 && !canSpeak && (
              <Alert severity="warning" sx={{ mt: 1.5 }}>
                Máy chưa có giọng tiếng Trung — ở câu nghe bạn có thể bấm "Hiện chữ" để trả lời.
              </Alert>
            )}
            <Button variant="contained" size="large" startIcon={<PlayArrowIcon />} onClick={start} fullWidth sx={{ mt: 2, minHeight: 52, fontSize: 17 }}>
              {lesson.progress?.attemptsCount ? 'Làm lại' : 'Bắt đầu'}
            </Button>
          </CardContent>
        </Card>
        <QuizHistory lessonId={lesson.id} enabled={active} />
      </Stack>
    )
  }

  // ─── Đang làm ───
  const isLast = index + 1 >= total
  const answeredCount = total - unanswered
  const submitError = submit.isError ? parseApiError(submit.error) : null

  return (
    <Stack sx={{ gap: 2 }}>
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5, gap: 1 }}>
          <Typography variant="body2" color="text.secondary">
            Câu {index + 1}/{total}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Đã trả lời {answeredCount}/{total}
          </Typography>
        </Box>
        <LinearProgress variant="determinate" value={(answeredCount / total) * 100} sx={{ height: 8, borderRadius: 4 }} />
      </Box>

      <QuizQuestionView
        key={current.id}
        question={current}
        options={attempt.order[current.id] ?? current.options}
        selectedOptionId={answers.get(current.id) ?? null}
        onSelect={select}
        disabled={locked}
      />

      <StickyActionBar>
        {submitError && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={doSubmit} disabled={submit.isPending}>
                Thử lại
              </Button>
            }
          >
            Chưa nộp được bài ({submitError.message}). Đáp án của bạn vẫn được giữ và đã khoá — bấm "Thử lại" để gửi
            lại đúng bộ đáp án này.
          </Alert>
        )}
        {isLast && unanswered > 0 && (
          <Typography variant="caption" color="warning.main" sx={{ textAlign: 'center' }}>
            Còn {unanswered} câu chưa trả lời — dùng nút "Trước" để quay lại.
          </Typography>
        )}
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="outlined" size="large" startIcon={<ArrowBackIcon />} onClick={() => goTo(index - 1)} disabled={index === 0 || submit.isPending} sx={{ minHeight: 52, flex: 1 }}>
            Trước
          </Button>
          {isLast ? (
            <Button
              variant="contained"
              size="large"
              endIcon={<SendIcon />}
              onClick={doSubmit}
              disabled={unanswered > 0 || submit.isPending}
              sx={{ minHeight: 52, flex: 2 }}
            >
              {submit.isPending ? 'Đang chấm…' : 'Nộp bài'}
            </Button>
          ) : (
            <Button variant="contained" size="large" endIcon={<ArrowForwardIcon />} onClick={() => goTo(index + 1)} disabled={submit.isPending} sx={{ minHeight: 52, flex: 2 }}>
              Tiếp
            </Button>
          )}
        </Box>
        <Button size="small" color="inherit" onClick={() => void quit()} disabled={submit.isPending} sx={{ alignSelf: 'center', color: 'text.secondary' }}>
          Thoát lượt làm
        </Button>
      </StickyActionBar>
    </Stack>
  )
}
