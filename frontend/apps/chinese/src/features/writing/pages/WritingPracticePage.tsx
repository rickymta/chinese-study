import { useCallback, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { Alert, Box, Button, Skeleton, Stack, Tab, Tabs, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { isApiError } from '@af/api'
import { PageContainer, useBackTo, useTabParam } from '@af/ui'
import { useChineseSpeech } from '@af/chinese-kit'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeechProvider'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { VoiceMissingAlert } from '@/features/pinyin/components/VoiceMissingAlert'
import { useHanziDataManifest, useRecordWritingAttempt, useWritingCharacter, useWritingSetAll } from '../hooks'
import { hasStrokeData } from '../lib/charData'
import { findNextPracticable, positionInSet } from '../lib/nextChar'
import { practicePath, setToHomeSearch, STEP_TABS, tuParamToSet, type StepTab } from '../lib/setParam'
import { CharacterInfoCard } from '../components/CharacterInfoCard'
import { StepView } from '../components/StepView'
import { StepGuided } from '../components/StepGuided'
import { StepRecall } from '../components/StepRecall'
import { AttemptResult, type SubmissionState } from '../components/AttemptResult'
import { LicenseNote } from '../components/LicenseNote'
import { useBoardSize } from '../components/useBoardSize'
import type { AttemptSummary, HanziWriterBoardHandle } from '../components/HanziWriterBoard'
import type { RecordWritingAttemptRequest, WritingCharacterDetail, WritingMode, WritingSet } from '../types'

interface FinishedAttempt {
  mode: WritingMode
  summary: AttemptSummary
}

function PracticeSkeleton() {
  return (
    <Stack sx={{ gap: 2 }}>
      <Skeleton variant="rounded" height={120} />
      <Skeleton variant="rounded" height={48} />
      <Skeleton variant="rounded" height={340} sx={{ mx: 'auto', width: '90%', maxWidth: 360 }} />
    </Stack>
  )
}

interface PracticeBodyProps {
  hanzi: string
  ch: WritingCharacterDetail
  set: WritingSet | null
  canWrite: boolean
  /** Danh mục dữ liệu nét tải lỗi — cha đã báo, thân trang không chồng thêm "Chưa có dữ liệu nét". */
  manifestFailed: boolean
}

/**
 * Thân trang cho MỘT chữ (cha gắn `key={hanzi}` ⇒ đổi chữ là làm mới toàn bộ trạng thái lượt).
 * Máy trạng thái lượt: bảng gọi `onStart` (sinh `clientAttemptId` mới, xoá kết quả cũ) → `onComplete` (gửi server,
 * hiện `AttemptResult`) → "Viết lại" (`restart()` ⇒ lại `onStart`) / "Bước kế" / "Chữ tiếp".
 */
function PracticeBody({ hanzi, ch, set, canWrite, manifestFailed }: PracticeBodyProps) {
  const navigate = useNavigate()
  const location = useLocation()
  const size = useBoardSize()
  const manifest = useHanziDataManifest()
  const { status: speechStatus } = useChineseSpeech()

  // Gợi ý bước mặc định (§5.3.2): chưa viết lần nào ⇒ Xem; đã có lần viết ⇒ Tự viết. Chốt một lần lúc mở chữ
  // (ref) để sau lượt đầu (stats xuất hiện) tab không tự nhảy.
  const defaultStepRef = useRef<StepTab>(canWrite && ch.stats ? 'tu-viet' : 'xem')
  const [step, setStep] = useTabParam<StepTab>(STEP_TABS, defaultStepRef.current)
  const effectiveStep: StepTab = canWrite ? step : 'xem'

  const boardRef = useRef<HanziWriterBoardHandle>(null)
  // Rỗng cho tới khi bảng gọi `onStart` (bước Xem không có lượt) — không sinh UUID mỗi lần render.
  const attemptIdRef = useRef<string>('')
  const lastBodyRef = useRef<RecordWritingAttemptRequest | null>(null)
  const [finished, setFinished] = useState<FinishedAttempt | null>(null)
  const [submission, setSubmission] = useState<SubmissionState | null>(null)
  const record = useRecordWritingAttempt()

  const send = useCallback(
    (body: RecordWritingAttemptRequest) => {
      lastBodyRef.current = body
      setSubmission({ status: 'sending' })
      record.mutate(body, {
        onSuccess: (result) => {
          // Lượt khác đã bắt đầu trong lúc chờ mạng ⇒ bỏ qua phản hồi muộn (kết quả vẫn được ghi ở server).
          if (attemptIdRef.current !== body.clientAttemptId) return
          setSubmission({ status: 'ok', result })
        },
        onError: (error) => {
          if (attemptIdRef.current !== body.clientAttemptId) return
          const status = isApiError(error) ? error.status : undefined
          // 400/422 là lỗi dữ liệu — gửi lại vô ích; còn lại (mạng, 5xx, 409) cho gửi lại.
          setSubmission({ status: 'error', error, retryable: status !== 400 && status !== 422 })
        },
      })
    },
    [record],
  )

  // Bảng bắt đầu lượt (lần đầu / "Viết lại" / đổi bước): id mới, xoá kết quả cũ (R-W8: id sinh LÚC BẮT ĐẦU lượt).
  const handleStart = useCallback(() => {
    attemptIdRef.current = crypto.randomUUID()
    lastBodyRef.current = null
    setFinished(null)
    setSubmission(null)
  }, [])

  const handleComplete = useCallback(
    (mode: WritingMode) => (summary: AttemptSummary) => {
      setFinished({ mode, summary })
      send({
        clientAttemptId: attemptIdRef.current,
        hanzi,
        mode,
        totalStrokes: summary.totalStrokes,
        totalMistakes: summary.totalMistakes,
        hintsUsed: summary.hintsUsed,
        durationMs: summary.durationMs,
      })
    },
    [hanzi, send],
  )

  const retrySend = useCallback(() => {
    const body = lastBodyRef.current
    if (!body) return
    // 409 DUPLICATE_ATTEMPT_ID (id trùng của người khác — cực hiếm) ⇒ id mới; lỗi mạng ⇒ CÙNG id để server khử trùng.
    const was409 = submission?.status === 'error' && isApiError(submission.error) && submission.error.status === 409
    if (was409) {
      attemptIdRef.current = crypto.randomUUID()
      send({ ...body, clientAttemptId: attemptIdRef.current })
    } else send(body)
  }, [send, submission])

  // "Chữ tiếp" trong cùng bộ (đã cache ở trang danh sách hoặc tải gộp ở đây).
  const setAll = useWritingSetAll(set)
  const items = setAll.data ?? []
  const nextHanzi = set ? findNextPracticable(items, hanzi, (h) => hasStrokeData(h, manifest.data)) : null
  const position = set ? positionInSet(items, hanzi) : null
  const goNextChar = useCallback(() => {
    if (!set) return
    if (nextHanzi) navigate(practicePath(nextHanzi, set), { state: location.state })
    else navigate(`/luyen-viet${setToHomeSearch(set)}`)
  }, [set, nextHanzi, navigate, location.state])

  const changeStep = (next: StepTab) => {
    // Đổi bước giữa lượt: bảng tạo lại theo `mode` ⇒ `onStart` tự dọn; sang bước Xem thì dọn tay.
    if (next === 'xem') handleStart()
    setStep(next)
  }

  return (
    <Stack sx={{ gap: 2 }}>
      {position && (
        <Typography variant="caption" color="text.secondary">
          Chữ {position.index}/{position.total} trong bộ
        </Typography>
      )}
      {/* Bước Tô/Tự viết: thẻ thu gọn để bảng viết còn trong màn 375px; Tự viết đang dở ⇒ che chữ mẫu (cả trong từ). */}
      <CharacterInfoCard ch={ch} hideGlyph={effectiveStep === 'tu-viet' && !finished} compact={effectiveStep !== 'xem'} />
      <VoiceMissingAlert status={speechStatus} />

      {!canWrite && !manifestFailed && (
        <Alert severity="warning">
          Chưa có dữ liệu nét cho chữ này — chỉ xem được thông tin, chưa tô/tự viết được. Hãy chọn chữ khác trong bộ.
        </Alert>
      )}

      <Tabs value={effectiveStep} onChange={(_e, v: StepTab) => changeStep(v)} variant="fullWidth" sx={{ borderBottom: 1, borderColor: 'divider' }} aria-label="Bước luyện viết">
        <Tab value="xem" label="1. Xem nét" disabled={!canWrite} />
        <Tab value="to-theo" label="2. Tô theo" disabled={!canWrite} />
        <Tab value="tu-viet" label="3. Tự viết" disabled={!canWrite} />
      </Tabs>

      {canWrite && effectiveStep === 'xem' && <StepView hanzi={hanzi} size={size} />}
      {canWrite && effectiveStep === 'to-theo' && (
        <StepGuided ref={boardRef} hanzi={hanzi} size={size} onStart={handleStart} onComplete={handleComplete('guided')} finished={!!finished} />
      )}
      {canWrite && effectiveStep === 'tu-viet' && (
        <StepRecall ref={boardRef} hanzi={hanzi} size={size} onStart={handleStart} onComplete={handleComplete('recall')} finished={!!finished} />
      )}

      {finished && submission && (
        <AttemptResult
          hanzi={hanzi}
          mode={finished.mode}
          summary={finished.summary}
          submission={submission}
          onRetrySend={retrySend}
          onWriteAgain={() => boardRef.current?.restart()}
          onNextStep={finished.mode === 'guided' ? () => changeStep('tu-viet') : undefined}
          onNextChar={set ? goNextChar : undefined}
          nextCharLabel={nextHanzi ? 'Chữ tiếp' : 'Về danh sách'}
        />
      )}
    </Stack>
  )
}

function PracticeInner() {
  const { hanzi = '' } = useParams<{ hanzi: string }>()
  const [params] = useSearchParams()
  const set = tuParamToSet(params.get('tu'))
  const goBack = useBackTo(`/luyen-viet${setToHomeSearch(set)}`)
  const query = useWritingCharacter(hanzi || undefined)
  const manifest = useHanziDataManifest()
  const canWrite = hasStrokeData(hanzi, manifest.data)

  return (
    <PageContainer maxWidth={720}>
      <Button startIcon={<ArrowBackIcon />} onClick={goBack} sx={{ mb: 1, ml: -1 }}>
        Danh sách chữ
      </Button>
      {query.isError ? (
        <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isLoading || !query.data || manifest.isLoading ? (
        <PracticeSkeleton />
      ) : (
        <Box>
          {manifest.isError && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              Không tải được danh mục dữ liệu nét — tải lại trang để thử lại.
            </Alert>
          )}
          <PracticeBody key={hanzi} hanzi={hanzi} ch={query.data} set={set} canWrite={canWrite} manifestFailed={manifest.isError} />
        </Box>
      )}
      <LicenseNote />
    </PageContainer>
  )
}

/**
 * `/luyen-viet/:hanzi?tab=xem|to-theo|tu-viet&tu=<bộ>` (F8, cần `study.use`). `:hanzi` đã được router giải mã.
 * Chữ không có trong kho ⇒ 404 do `createApiClient` điều hướng. Một `useSpeech('zh')` dùng chung (nút nghe).
 */
export function WritingPracticePage() {
  return (
    <ChineseSpeechProvider>
      <PracticeInner />
    </ChineseSpeechProvider>
  )
}
