import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Box, Button, Card, CardContent, Chip, LinearProgress, Stack, Typography } from '@mui/material'
import VolumeUpIcon from '@mui/icons-material/VolumeUp'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import CancelIcon from '@mui/icons-material/Cancel'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { Hanzi, Pinyin, useChineseSpeech, SpeakButton } from '@af/chinese-kit'
import { LangText, StickyActionBar } from '@af/ui'
import type { DrillTone, PinyinChart, ToneKey } from '../../types'
import { computeResponseMs, type AnsweredItem, type DrillSession } from '../../drill/drillTypes'
import { ToneButtons } from './ToneButtons'

const AUTO_NEXT_MS = 1200

export interface DrillRunnerProps {
  session: DrillSession
  chart: Pick<PinyinChart, 'syllables'>
  /** Gọi khi trả lời xong câu cuối. */
  onFinish: (answers: AnsweredItem[], finishedAt: Date) => void
  /** `true` khi có hộp thoại đè lên ⇒ bỏ qua phím tắt. */
  paused?: boolean
}

/**
 * Bộ chạy bài luyện (R5-9): phát chữ, chọn thanh (1 hoặc 2 phần), chấm ngay, nghe lại thanh đúng/thanh đã chọn,
 * câu đúng tự sang sau 1,2 s, câu sai phải bấm "Tiếp". Phím: 1–4 chọn, Space nghe lại, Enter sang câu kế.
 * TTS: tự phát khi sang câu mới CHỈ khi câu trước được chuyển bằng thao tác người dùng (iOS Safari chặn phát
 * ngoài handler thao tác) — tự sang sau 1,2 s thì người học bấm "Nghe".
 */
export function DrillRunner({ session, chart, onFinish, paused }: DrillRunnerProps) {
  const { canSpeak, speakZh, speaking } = useChineseSpeech()
  const items = session.items
  const [index, setIndex] = useState(0)
  const [phase, setPhase] = useState<'answering' | 'feedback'>('answering')
  const [selections, setSelections] = useState<DrillTone[]>([])
  const answersRef = useRef<AnsweredItem[]>([])
  const replayRef = useRef(0)
  const firstPlayEndRef = useRef<number | null>(null)
  const playedOnceRef = useRef(false)
  const autoplayRef = useRef(true) // câu đầu: "Bắt đầu" là thao tác người dùng
  const autoNextTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const item = items[index]!
  const parts = item.parts
  const text = parts.map((p) => p.hanzi).join('')

  // Tra chữ minh hoạ của (âm tiết, thanh) để "Nghe thanh bạn chọn".
  const syllableMap = useMemo(() => new Map(chart.syllables.map((s) => [s.syllable, s])), [chart.syllables])
  const exampleFor = useCallback(
    (syllable: string, tone: DrillTone) => syllableMap.get(syllable)?.tones[String(tone) as ToneKey] ?? null,
    [syllableMap],
  )

  const play = useCallback(() => {
    if (!canSpeak) return
    if (playedOnceRef.current) replayRef.current++
    void speakZh(text)
      .catch(() => undefined)
      .finally(() => {
        if (!playedOnceRef.current) {
          playedOnceRef.current = true
          firstPlayEndRef.current = performance.now()
        }
      })
  }, [canSpeak, speakZh, text])

  // Tự phát khi vào câu mới nếu được phép (xem ghi chú đầu file). Guard theo index để StrictMode không phát hai lần.
  const playedIndexRef = useRef(-1)
  useEffect(() => {
    if (playedIndexRef.current === index) return
    playedIndexRef.current = index
    if (autoplayRef.current) play()
    // Chỉ phụ thuộc `index`: `play` đổi theo giọng/tốc độ nhưng không được phát lại giữa chừng câu.
  }, [index]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(
    () => () => {
      if (autoNextTimer.current) clearTimeout(autoNextTimer.current)
    },
    [],
  )

  const goNext = useCallback(
    (byUser: boolean) => {
      if (autoNextTimer.current) {
        clearTimeout(autoNextTimer.current)
        autoNextTimer.current = null
      }
      if (index + 1 >= items.length) {
        onFinish(answersRef.current, new Date())
        return
      }
      autoplayRef.current = byUser
      replayRef.current = 0
      firstPlayEndRef.current = null
      playedOnceRef.current = false
      setSelections([])
      setPhase('answering')
      setIndex((i) => i + 1)
    },
    [index, items.length, onFinish],
  )

  const select = useCallback(
    (tone: DrillTone) => {
      if (phase !== 'answering') return
      const next = [...selections, tone]
      setSelections(next)
      if (next.length < parts.length) return
      const correct = parts.every((p, i) => p.tone === next[i])
      const responseMs = computeResponseMs(firstPlayEndRef.current, performance.now())
      answersRef.current = [...answersRef.current, { item, answered: next, correct, responseMs, replayCount: Math.min(replayRef.current, 100) }]
      setPhase('feedback')
      if (correct) autoNextTimer.current = setTimeout(() => goNext(false), AUTO_NEXT_MS)
    },
    [phase, selections, parts, item, goNext],
  )

  // Phím tắt toàn cửa sổ.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (paused || e.repeat) return
      const target = e.target as HTMLElement | null
      if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return
      if (e.key >= '1' && e.key <= '4') {
        e.preventDefault()
        select(Number(e.key) as DrillTone)
      } else if (e.key === ' ') {
        e.preventDefault()
        play()
      } else if (e.key === 'Enter' && phase === 'feedback') {
        e.preventDefault()
        goNext(true)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [paused, select, play, goNext, phase])

  const lastAnswer = phase === 'feedback' ? answersRef.current[answersRef.current.length - 1] : undefined
  const progress = ((index + (phase === 'feedback' ? 1 : 0)) / items.length) * 100

  return (
    <Stack spacing={2}>
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
          <Typography variant="body2" color="text.secondary">
            Câu {index + 1}/{items.length}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {session.mode === 'listen_tone' ? 'Một âm tiết' : 'Cặp thanh'}
          </Typography>
        </Box>
        <LinearProgress variant="determinate" value={progress} sx={{ height: 8, borderRadius: 4 }} />
      </Box>

      <Card>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
          <Button
            variant="contained"
            size="large"
            startIcon={<VolumeUpIcon />}
            onClick={play}
            disabled={!canSpeak || speaking}
            sx={{ minHeight: 64, minWidth: 200, fontSize: 18 }}
          >
            {playedOnceRef.current ? 'Nghe lại' : 'Nghe'}
          </Button>
          {!canSpeak && (
            <Typography variant="caption" color="warning.main">
              Chưa có giọng tiếng Trung — không phát được câu hỏi.
            </Typography>
          )}

          {phase === 'answering' ? (
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
              {parts.length === 2
                ? `Chọn thanh cho chữ thứ ${selections.length + 1} / 2`
                : 'Bạn nghe được thanh nào?'}
            </Typography>
          ) : (
            lastAnswer && (
              <Stack spacing={1.5} sx={{ width: '100%', alignItems: 'center' }}>
                <Chip
                  icon={lastAnswer.correct ? <CheckCircleIcon /> : <CancelIcon />}
                  label={lastAnswer.correct ? 'Đúng!' : 'Chưa đúng'}
                  color={lastAnswer.correct ? 'success' : 'error'}
                />
                <Box sx={{ display: 'flex', gap: 3, justifyContent: 'center', flexWrap: 'wrap' }}>
                  {parts.map((p, i) => {
                    const chosen = lastAnswer.answered[i]!
                    const partOk = chosen === p.tone
                    const chosenExample = partOk ? null : exampleFor(p.syllable, chosen)
                    return (
                      <Box key={i} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 0.5, minWidth: 140 }}>
                        <Hanzi size="xl">{p.hanzi}</Hanzi>
                        <Pinyin value={`${p.syllable}${p.tone}`} variant="h6" sx={{ fontWeight: 700 }} />
                        <Typography variant="body2" color="text.secondary">
                          {p.meaningVi}
                        </Typography>
                        <Typography variant="caption" color={partOk ? 'success.main' : 'error.main'}>
                          {partOk ? `Thanh ${p.tone} — đúng` : `Đúng là thanh ${p.tone}, bạn chọn thanh ${chosen}`}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', justifyContent: 'center' }}>
                          <SpeakButton text={p.hanzi} variant="button" label={`Nghe thanh ${p.tone}`} size="small" />
                          {chosenExample && (
                            <SpeakButton
                              text={chosenExample.hanzi}
                              variant="button"
                              label={
                                <>
                                  Nghe thanh {chosen} (<LangText lang="zh-CN">{chosenExample.hanzi}</LangText>)
                                </>
                              }
                              ariaLabel={`Nghe thanh ${chosen}`}
                              size="small"
                              color="inherit"
                            />
                          )}
                        </Box>
                      </Box>
                    )
                  })}
                </Box>
              </Stack>
            )
          )}
        </CardContent>
      </Card>

      {parts.length === 1 ? (
        <ToneButtons
          value={selections[0] ?? null}
          onSelect={select}
          disabled={phase !== 'answering'}
          correctTone={phase === 'feedback' ? parts[0]!.tone : undefined}
        />
      ) : (
        <Stack spacing={1.5}>
          <ToneButtons
            label="Chữ thứ nhất"
            value={selections[0] ?? null}
            onSelect={select}
            disabled={phase !== 'answering' || selections.length !== 0}
            correctTone={phase === 'feedback' ? parts[0]!.tone : undefined}
          />
          <ToneButtons
            label="Chữ thứ hai"
            value={selections[1] ?? null}
            onSelect={select}
            disabled={phase !== 'answering' || selections.length !== 1}
            correctTone={phase === 'feedback' ? parts[1]!.tone : undefined}
          />
        </Stack>
      )}

      {phase === 'feedback' && (
        // Dính đáy (trên bottom nav + safe-area): ở 375×812 câu sai có phần giải thích dài đẩy nút "Tiếp" xuống
        // dưới bottom nav — phát hiện khi tích hợp F5.
        <StickyActionBar>
          <Button
            variant={lastAnswer?.correct ? 'outlined' : 'contained'}
            size="large"
            endIcon={<ArrowForwardIcon />}
            onClick={() => goNext(true)}
            sx={{ minHeight: 52 }}
            fullWidth
          >
            {index + 1 >= items.length ? 'Xem kết quả' : lastAnswer?.correct ? 'Tiếp (tự chuyển)' : 'Tiếp'}
          </Button>
        </StickyActionBar>
      )}
    </Stack>
  )
}
