import { useCallback, useEffect, useRef, useState } from 'react'
import { useBlocker } from 'react-router-dom'
import { Alert, Button, Skeleton, Stack } from '@mui/material'
import { AppDialog, useTabParam } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useChineseSpeech } from '@/components/speech/ChineseSpeech'
import { usePinyinChart, useSubmitToneDrill, useToneStats } from '../../hooks'
import type { DrillMode, SubmitToneDrillRequest } from '../../types'
import { generateDrill } from '../../drill/generateDrill'
import type { AnsweredItem, DrillOutcome, DrillSession } from '../../drill/drillTypes'
import { DrillSetup } from './DrillSetup'
import { DrillRunner } from './DrillRunner'
import { DrillResult } from './DrillResult'
import { ToneStatsCard } from '../ToneStatsCard'

const MODE_PARAM = ['mot', 'cap'] as const
type ModeParam = (typeof MODE_PARAM)[number]
const toMode = (p: ModeParam): DrillMode => (p === 'cap' ? 'tone_pair' : 'listen_tone')
const toParam = (m: DrillMode): ModeParam => (m === 'tone_pair' ? 'cap' : 'mot')

type Phase = { kind: 'setup' } | { kind: 'running'; session: DrillSession } | { kind: 'result'; outcome: DrillOutcome; request: SubmitToneDrillRequest }

function buildRequest(outcome: DrillOutcome): SubmitToneDrillRequest {
  return {
    clientSessionId: outcome.session.clientSessionId,
    mode: outcome.session.mode,
    startedAt: outcome.session.startedAt.toISOString(),
    finishedAt: outcome.finishedAt.toISOString(),
    items: outcome.answers.map((a) => ({
      parts: a.item.parts.map((p, i) => ({ syllable: p.syllable, hanzi: p.hanzi, expectedTone: p.tone, answeredTone: a.answered[i]! })),
      responseMs: a.responseMs,
      replayCount: a.replayCount,
    })),
  }
}

/**
 * Tab Luyện: `setup` (chọn chế độ + thẻ thống kê) → `running` (20 câu) → `result` (nộp + kết quả).
 * Rời trang/đổi tab khi đang làm ⇒ `useBlocker` + AppDialog xác nhận (bỏ giữa chừng không lưu gì — R5-10).
 */
export function DrillTab() {
  const chart = usePinyinChart()
  const stats = useToneStats()
  const submit = useSubmitToneDrill()
  const { canSpeak, cancel } = useChineseSpeech()
  const [modeParam, setModeParam] = useTabParam<ModeParam>(MODE_PARAM, 'mot', 'che-do')
  const mode = toMode(modeParam)
  const [phase, setPhase] = useState<Phase>({ kind: 'setup' })
  const running = phase.kind === 'running'

  // Chặn điều hướng (kể cả đổi `?tab=` trong trang — cũng là một navigation) khi đang làm bài.
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      running && (currentLocation.pathname !== nextLocation.pathname || currentLocation.search !== nextLocation.search),
  )
  const confirmOpen = blocker.state === 'blocked'

  // Tải lại/đóng tab trình duyệt cũng hỏi (không tuỳ biến được lời — trình duyệt tự hiện).
  useEffect(() => {
    if (!running) return
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault()
    }
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [running])

  const focus = stats.data?.recommendedFocus ?? []

  const start = () => {
    if (!chart.data) return
    const items = generateDrill({ mode, chart: chart.data, focus })
    if (items.length === 0) return
    setPhase({
      kind: 'running',
      session: { clientSessionId: crypto.randomUUID(), mode, items, startedAt: new Date() },
    })
  }

  const requestRef = useRef<SubmitToneDrillRequest | null>(null)
  const finish = useCallback(
    (answers: AnsweredItem[], finishedAt: Date) => {
      if (phase.kind !== 'running') return
      const outcome: DrillOutcome = { session: phase.session, answers, finishedAt }
      const request = buildRequest(outcome)
      requestRef.current = request
      setPhase({ kind: 'result', outcome, request })
      submit.mutate(request)
    },
    [phase, submit],
  )

  const abandon = () => {
    cancel()
    setPhase({ kind: 'setup' })
    if (blocker.state === 'blocked') blocker.proceed()
  }

  const backToSetup = () => {
    submit.reset()
    setPhase({ kind: 'setup' })
  }

  const chartErr = chart.error ? parseApiError(chart.error) : null

  return (
    <Stack spacing={2}>
      {phase.kind === 'setup' && (
        <>
          {chart.isPending ? (
            <Skeleton variant="rounded" height={220} />
          ) : (
            <DrillSetup
              mode={mode}
              onModeChange={(m) => setModeParam(toParam(m))}
              focus={focus}
              onStart={start}
              disabled={!chart.data}
              disabledReason={
                chartErr ? (chartErr.status === 503 ? 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.' : `Không tải được bảng pinyin: ${chartErr.message}`) : undefined
              }
              canSpeak={canSpeak}
            />
          )}
          {chartErr && chartErr.status !== 503 && (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={() => void chart.refetch()}>
                  Thử lại
                </Button>
              }
            >
              {chartErr.message}
            </Alert>
          )}
          <ToneStatsCard />
        </>
      )}

      {phase.kind === 'running' && chart.data && (
        <DrillRunner key={phase.session.clientSessionId} session={phase.session} chart={chart.data} onFinish={finish} paused={confirmOpen} />
      )}

      {phase.kind === 'result' && (
        <DrillResult
          outcome={phase.outcome}
          serverResult={submit.data}
          submitting={submit.isPending}
          submitError={submit.error ?? undefined}
          onRetry={() => submit.mutate(phase.request)}
          onNewDrill={backToSetup}
          onViewStats={backToSetup}
        />
      )}

      <AppDialog
        open={confirmOpen}
        onClose={() => blocker.state === 'blocked' && blocker.reset()}
        title="Bỏ bài đang làm?"
        fullScreenBelow={false}
        actions={
          <>
            <Button onClick={() => blocker.state === 'blocked' && blocker.reset()} color="inherit">
              Tiếp tục làm
            </Button>
            <Button onClick={abandon} color="error" variant="contained">
              Bỏ bài
            </Button>
          </>
        }
      >
        Kết quả sẽ không được lưu. Chỉ những bài làm đủ 20 câu mới được tính vào thống kê.
      </AppDialog>
    </Stack>
  )
}
