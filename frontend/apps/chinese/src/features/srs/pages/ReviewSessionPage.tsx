import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Alert, Box, Button, CircularProgress, Skeleton, Stack, Typography } from '@mui/material'
import { AppDrawer, StickyActionBar, useConfirm, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { ChineseSpeechProvider, useChineseSpeech } from '@/components/speech/ChineseSpeech'
import { QueryErrorAlert } from '@/features/dictionary/components/QueryErrorAlert'
import { WordDetailBody } from '@/features/dictionary/pages/WordDetailPage'
import { useWord } from '@/features/dictionary/hooks'
import { getSrsQueue, QUEUE_LIMIT_DEFAULT } from '../api'
import { useApplySrsSummary } from '../hooks'
import { useReviewOutbox } from '../useReviewOutbox'
import { ratingFromKey } from '../lib/ratings'
import { clampDurationMs, countRatings, mergeIncoming, shouldLoadMore } from '../lib/sessionDeck'
import { Flashcard } from '../components/Flashcard'
import { RatingBar } from '../components/RatingBar'
import { SessionHeader } from '../components/SessionHeader'
import { SessionSummary } from '../components/SessionSummary'
import { PendingReviewsBanner } from '../components/PendingReviewsBanner'
import type { SrsQueueCard, SrsRating, SrsSummary } from '../types'

/** Khoá nút chấm sau mỗi lượt để chống bấm đúp (hợp đồng: 300 ms). */
const RATE_LOCK_MS = 300

type Phase = 'loading' | 'ready' | 'error' | 'finished'

/** Nội dung ngăn kéo "Xem chi tiết": tải `/dictionary/words/{id}` và dùng lại thân trang chi tiết từ (F6.3). */
function WordDetailDrawerBody({ wordId }: { wordId: string }) {
  const query = useWord(wordId)
  if (query.isError) return <QueryErrorAlert error={query.error} onRetry={() => void query.refetch()} />
  if (query.isLoading || !query.data) {
    return (
      <Stack sx={{ gap: 2 }}>
        <Skeleton variant="rounded" width={160} height={80} />
        <Skeleton variant="text" width={120} />
        <Skeleton variant="rounded" height={96} />
      </Stack>
    )
  }
  return <WordDetailBody word={query.data} openLinksInNewTab />
}

function ReviewSessionInner() {
  const navigate = useNavigate()
  const confirm = useConfirm()
  const toast = useToast()
  const applySummary = useApplySrsSummary()
  const { autoPlayAudio, canSpeak, speakZh } = useChineseSpeech()

  const [phase, setPhase] = useState<Phase>('loading')
  const [loadError, setLoadError] = useState<unknown>(null)
  const [deck, setDeck] = useState<SrsQueueCard[]>([])
  const [index, setIndex] = useState(0)
  const [flipped, setFlipped] = useState(false)
  const [ratings, setRatings] = useState<SrsRating[]>([])
  const [locked, setLocked] = useState(false)
  const [exhausted, setExhausted] = useState(false)
  const [loadingMore, setLoadingMore] = useState(false)
  const [moreError, setMoreError] = useState<unknown>(null)
  const [lastSummary, setLastSummary] = useState<SrsSummary | null>(null)
  const [detailWordId, setDetailWordId] = useState<string | null>(null)
  /** Tăng để effect "tải thêm" chạy lại khi lô vừa tải phải bỏ thẻ mới (xem `loadMore`). */
  const [reloadTick, setReloadTick] = useState(0)

  const deckRef = useRef(deck)
  deckRef.current = deck
  const indexRef = useRef(index)
  indexRef.current = index
  const ratedIdsRef = useRef<Set<string>>(new Set())
  const loadingMoreRef = useRef(false)
  /** Đếm lượt chấm đã nộp vào outbox — phát hiện lượt chấm phát sinh TRONG LÚC đang gọi `/srs/queue`. */
  const submitSeqRef = useRef(0)
  const shownAtRef = useRef<number>(performance.now())
  const sessionStartRef = useRef<number>(Date.now())
  const sessionEndRef = useRef<number | null>(null)
  const spokenCardIdRef = useRef<string | null>(null)
  const lockTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const outbox = useReviewOutbox({
    onSent: (_item, res) => {
      applySummary(res.summary)
      setLastSummary(res.summary)
    },
    onDropped: (_item, err) => {
      const parsed = parseApiError(err)
      // 409/422 là câu trả lời thật (mã trùng thẻ khác, thẻ tạm dừng, đủ từ mới) ⇒ báo, không gửi lại.
      toast.error(`Không ghi được một lượt chấm: ${parsed.message}`)
    },
  })
  const pendingCardIdsRef = useRef(outbox.pendingCardIds)
  pendingCardIdsRef.current = outbox.pendingCardIds

  const current = deck[index]
  const done = ratings.length
  const remaining = deck.length - index

  // ── Tải hàng đợi lần đầu / "Ôn tiếp" ──
  const loadInitial = useCallback(async () => {
    setPhase('loading')
    setLoadError(null)
    setDeck([])
    setIndex(0)
    setFlipped(false)
    setRatings([])
    setExhausted(false)
    setMoreError(null)
    ratedIdsRef.current = new Set()
    spokenCardIdRef.current = null
    sessionStartRef.current = Date.now()
    sessionEndRef.current = null
    try {
      const res = await getSrsQueue(QUEUE_LIMIT_DEFAULT)
      applySummary(res.summary)
      setLastSummary(res.summary)
      // Thẻ đang chờ gửi (mở lại phiên với outbox dở) ⇒ bỏ để không chấm hai lần.
      const cards = mergeIncoming([], new Set(), pendingCardIdsRef.current, res.cards)
      setDeck(cards)
      if (cards.length === 0) {
        sessionEndRef.current = Date.now()
        setPhase('finished')
      } else {
        setPhase('ready')
      }
    } catch (err) {
      setLoadError(err)
      setPhase('error')
    }
  }, [applySummary])

  useEffect(() => {
    void loadInitial()
  }, [loadInitial])

  // ── Tải thêm khi còn ≤ 5 thẻ chưa chấm ──
  const loadMore = useCallback(async () => {
    if (loadingMoreRef.current) return
    loadingMoreRef.current = true
    setLoadingMore(true)
    setMoreError(null)
    try {
      const seqAtStart = submitSeqRef.current
      const pendingAtStart = pendingCardIdsRef.current.size > 0
      const res = await getSrsQueue(QUEUE_LIMIT_DEFAULT)
      applySummary(res.summary)
      setLastSummary(res.summary)
      // Có lượt chấm chưa chắc đã tới server trong suốt lời gọi (chờ gửi lúc bắt đầu/kết thúc, hoặc vừa chấm thêm)
      // ⇒ "từ mới hôm nay" của server đã cũ ⇒ KHÔNG nhận thẻ mới, kẻo cấp dư rồi lượt chấm bị 422 (tích hợp F7).
      const pendingNow = pendingCardIdsRef.current.size > 0
      const newUnsafe = pendingAtStart || pendingNow || submitSeqRef.current !== seqAtStart
      // Chỉ so với phần bộ bài CHƯA chấm (review F7): bản quay lại của thẻ learning đã nằm sẵn thì không nối thêm.
      const unrated = deckRef.current.slice(indexRef.current)
      const candidates = mergeIncoming(unrated, ratedIdsRef.current, pendingCardIdsRef.current, res.cards)
      const skippedNew = newUnsafe && candidates.some((c) => c.state === 'new')
      const added = newUnsafe ? candidates.filter((c) => c.state !== 'new') : candidates
      if (added.length > 0) setDeck((d) => [...d, ...added])
      // Không có gì mới, không còn lượt chờ gửi, không phải bỏ thẻ mới ⇒ server thật sự hết thẻ cho phiên này.
      // Còn lượt chờ ⇒ thẻ vừa chấm có thể quay lại (học lại sau 1 phút) ⇒ effect tự thử lại khi outbox đổi.
      else if (!pendingNow && !skippedNew) setExhausted(true)
      // Đã bỏ thẻ mới mà outbox đã trống (không còn gì làm effect chạy lại) ⇒ tự hẹn tải lại.
      if (skippedNew && !pendingNow) setReloadTick((t) => t + 1)
    } catch (err) {
      setMoreError(err)
    } finally {
      loadingMoreRef.current = false
      setLoadingMore(false)
    }
  }, [applySummary])

  useEffect(() => {
    if (phase !== 'ready') return
    if (shouldLoadMore(remaining, loadingMoreRef.current, exhausted)) void loadMore()
  }, [phase, remaining, exhausted, outbox.pendingCount, reloadTick, loadMore])

  // Hết thẻ trong bộ: chờ tải thêm xong; server hết ⇒ tổng kết.
  useEffect(() => {
    if (phase !== 'ready' || remaining > 0) return
    if (exhausted && !loadingMore) {
      sessionEndRef.current = Date.now()
      setPhase('finished')
    }
  }, [phase, remaining, exhausted, loadingMore])

  // ── Thẻ mới hiện: mốc thời gian + tự đọc (fallback ngoài thao tác — iOS có thể chặn, khi đó người học bấm loa) ──
  useEffect(() => {
    if (!current) return
    shownAtRef.current = performance.now()
    if (autoPlayAudio && canSpeak && spokenCardIdRef.current !== current.cardId) {
      spokenCardIdRef.current = current.cardId
      void speakZh(current.word.simplified).catch(() => undefined)
    }
    // Theo vị trí + id thẻ (thẻ "Quên" có thể quay lại ngay sau chính nó với cùng id): đổi giọng/tốc độ giữa chừng
    // không được đọc lại.
  }, [index, current?.cardId]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(
    () => () => {
      if (lockTimerRef.current) clearTimeout(lockTimerRef.current)
    },
    [],
  )

  const flip = useCallback(() => {
    if (!current || flipped) return
    setFlipped(true)
  }, [current, flipped])

  const rate = useCallback(
    (rating: SrsRating) => {
      if (!current || !flipped || locked) return
      const durationMs = clampDurationMs(performance.now() - shownAtRef.current)
      // Lạc quan: ghi nhận + chuyển thẻ kế ngay; gửi ở nền qua outbox (clientReviewId sinh MỘT lần, giữ khi gửi lại).
      submitSeqRef.current += 1
      outbox.submit({ clientReviewId: crypto.randomUUID(), cardId: current.cardId, rating, durationMs })
      ratedIdsRef.current.add(current.cardId)
      setRatings((r) => [...r, rating])
      setFlipped(false)
      setIndex((i) => i + 1)
      setLocked(true)
      if (lockTimerRef.current) clearTimeout(lockTimerRef.current)
      lockTimerRef.current = setTimeout(() => setLocked(false), RATE_LOCK_MS)
      // Đang trong handler thao tác người dùng ⇒ đọc thẻ kế ngay tại đây (iOS Safari chỉ cho phát ở đây).
      const next = deckRef.current[index + 1]
      if (next && autoPlayAudio && canSpeak) {
        spokenCardIdRef.current = next.cardId
        void speakZh(next.word.simplified).catch(() => undefined)
      }
    },
    [current, flipped, locked, outbox, index, autoPlayAudio, canSpeak, speakZh],
  )

  // ── Phím tắt: Space/Enter lật; 1–4 chấm (sau khi lật). Bỏ qua khi lặp phím, focus ô nhập, hoặc có ngăn kéo mở. ──
  useEffect(() => {
    if (phase !== 'ready') return
    const onKey = (e: KeyboardEvent) => {
      if (e.repeat || detailWordId !== null) return
      const target = e.target as HTMLElement | null
      if (target && (/^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName) || target.isContentEditable)) return
      if (e.key === ' ' || e.key === 'Enter') {
        if (!flipped) {
          e.preventDefault()
          flip()
        }
        return
      }
      const r = ratingFromKey(e.key)
      if (r && flipped) {
        e.preventDefault()
        rate(r)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [phase, flipped, detailWordId, flip, rate])

  const handleClose = useCallback(async () => {
    // Cùng điều kiện với banner: chỉ hỏi khi có lượt đã gửi hỏng (lượt đang gửi lần đầu vẫn hoàn tất ở nền).
    if (outbox.unsentCount > 0) {
      const ok = await confirm({
        title: 'Rời phiên ôn?',
        message: `Còn ${outbox.unsentCount} đánh giá chưa gửi. Rời đi vẫn giữ để gửi lại khi quay lại phiên?`,
        confirmText: 'Rời đi',
        cancelText: 'Ở lại',
      })
      if (!ok) return
    }
    navigate('/on-tap')
  }, [outbox.unsentCount, confirm, navigate])

  const counts = useMemo(() => countRatings(ratings), [ratings])

  // ── Render ──
  if (phase === 'loading') {
    return (
      <Stack sx={{ alignItems: 'center', justifyContent: 'center', gap: 2, minHeight: '50dvh' }}>
        <CircularProgress />
        <Typography color="text.secondary">Đang lấy thẻ đến hạn…</Typography>
      </Stack>
    )
  }

  if (phase === 'error') {
    return (
      <Stack sx={{ gap: 2, maxWidth: 560, mx: 'auto' }}>
        <QueryErrorAlert error={loadError} onRetry={() => void loadInitial()} />
        <Button variant="outlined" onClick={() => navigate('/on-tap')}>
          Về trang ôn tập
        </Button>
      </Stack>
    )
  }

  if (phase === 'finished') {
    const canContinue = !!lastSummary && lastSummary.dueNow + lastSummary.newAvailableToday > 0
    return (
      <Stack sx={{ gap: 2, py: 2 }}>
        <PendingReviewsBanner count={outbox.unsentCount} />
        <SessionSummary
          counts={counts}
          durationMs={(sessionEndRef.current ?? Date.now()) - sessionStartRef.current}
          canContinue={canContinue}
          onContinue={() => void loadInitial()}
          onHome={() => void handleClose()}
        />
      </Stack>
    )
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        // Trọn chiều cao còn lại dưới AppBar (xs–sm) trừ padding của <main> (16px ×2 ở xs, 24px ×2 ở md+);
        // dùng minHeight để nghĩa dài vẫn cuộn được, thanh đáy sticky.
        minHeight: {
          xs: 'calc(100dvh - var(--af-top-bar-offset, 0px) - 32px - env(safe-area-inset-bottom))',
          md: 'calc(100dvh - 48px)',
        },
        maxWidth: 720,
        mx: 'auto',
        width: '100%',
      }}
    >
      <Stack sx={{ gap: 1, flexShrink: 0 }}>
        <SessionHeader done={done} total={deck.length} onClose={() => void handleClose()} />
        <PendingReviewsBanner count={outbox.unsentCount} />
      </Stack>

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', py: 3 }}>
        {current ? (
          <Flashcard card={current} flipped={flipped} onOpenDetail={(id) => setDetailWordId(id)} />
        ) : moreError ? (
          <Stack sx={{ gap: 1.5, width: '100%', maxWidth: 480 }}>
            <QueryErrorAlert error={moreError} onRetry={() => void loadMore()} />
            <Button
              variant="outlined"
              onClick={() => {
                sessionEndRef.current = Date.now()
                setPhase('finished')
              }}
            >
              Kết thúc phiên
            </Button>
          </Stack>
        ) : (
          <Stack sx={{ alignItems: 'center', gap: 1.5 }}>
            <CircularProgress size={28} />
            <Typography color="text.secondary">Đang lấy thêm thẻ…</Typography>
          </Stack>
        )}
      </Box>

      {current && (
        <StickyActionBar sx={{ pb: 'calc(8px + env(safe-area-inset-bottom))', flexShrink: 0 }}>
          {flipped ? (
            <RatingBar intervals={current.intervals} onRate={rate} disabled={locked} />
          ) : (
            <Button variant="outlined" size="large" fullWidth onClick={flip} sx={{ minHeight: 56, fontSize: 18 }} aria-keyshortcuts="Space Enter">
              Hiện đáp án
            </Button>
          )}
          {moreError !== null && remaining > 0 && (
            <Alert severity="warning" sx={{ py: 0 }}>
              Chưa lấy thêm được thẻ — sẽ thử lại sau lượt chấm kế.
            </Alert>
          )}
        </StickyActionBar>
      )}

      {/* Chỉ đọc (xem nghĩa/chữ) ⇒ cho bấm ra ngoài/ESC đóng nhanh để quay lại thẻ. Phím tắt phiên tạm tắt khi mở. */}
      <AppDrawer open={detailWordId !== null} onClose={() => setDetailWordId(null)} title="Chi tiết từ" closeOnBackdrop width={480}>
        {detailWordId && <WordDetailDrawerBody wordId={detailWordId} />}
      </AppDrawer>
    </Box>
  )
}

/** `/on-tap/phien` (F7.2, cần `study.use`) — `AppShell` ẩn bottom nav ở route này để 4 nút chấm nằm sát đáy. */
export function ReviewSessionPage() {
  return (
    <ChineseSpeechProvider>
      <ReviewSessionInner />
    </ChineseSpeechProvider>
  )
}
