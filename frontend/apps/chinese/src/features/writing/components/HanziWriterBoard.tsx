import { useEffect, useImperativeHandle, useRef, useState, type Ref } from 'react'
import HanziWriter, { type StrokeData } from 'hanzi-writer'
import { Alert, Box, Button, CircularProgress, useTheme } from '@mui/material'
import { loadCharData } from '../lib/charData'
import { MiZiGrid } from './MiZiGrid'

export type BoardMode = 'view' | 'guided' | 'recall'

/** Kết quả một lần viết hoàn tất (R-W3) — trang gửi lên `POST /api/writing/attempts`. */
export interface AttemptSummary {
  totalMistakes: number
  hintsUsed: number
  totalStrokes: number
  durationMs: number
}

export interface HanziWriterBoardHandle {
  /** Bước Xem: chạy hoạt hình thứ tự nét một lần. */
  animate: () => void
  /** Bước Xem: lặp hoạt hình cho tới khi `stopLoop`. */
  loop: () => void
  /** Bước Xem: dừng lặp, hiện nguyên chữ. */
  stopLoop: () => void
  /** Bước Tự viết: tô sáng nét hiện tại (tính là 1 gợi ý). */
  hint: () => void
  /** Bước Tô/Tự viết: bắt đầu lượt mới (0 lỗi, 0 gợi ý). */
  restart: () => void
}

export interface HanziWriterBoardProps {
  hanzi: string
  mode: BoardMode
  /** Cạnh bảng (px) — trang tính `min(innerWidth * 0.9, 360)`. */
  size: number
  /** Tốc độ hoạt hình bước Xem (0.5 = chậm, 1 = thường). */
  animationSpeed?: number
  /** Lượt viết hoàn tất (`quiz()` chạy tới `onComplete`). */
  onComplete?: (summary: AttemptSummary) => void
  /** Lượt viết (lại) bắt đầu — trang sinh `clientAttemptId` mới tại đây. */
  onStart?: () => void
  onMistake?: (d: StrokeData) => void
  onCorrectStroke?: (d: StrokeData) => void
  onLoadError?: () => void
  ref?: Ref<HanziWriterBoardHandle>
}

/** Ngưỡng tự hiện nét đúng (R-W2): Tô theo sau 2 lần sai, Tự viết sau 3. */
export const HINT_THRESHOLD: Record<Exclude<BoardMode, 'view'>, number> = { guided: 2, recall: 3 }

const PADDING = 12
/** CHECK của DB (§5.1.2): `total_strokes` 1–64, `duration_ms` ≤ 3 600 000. */
const MAX_DURATION_MS = 3_600_000

/** hanzi-writer chỉ nhận màu HEX (`#RGB`/`#RRGGBB`) — palette MUI có thể là `rgba()` ⇒ dùng dự phòng. */
function hexOr(value: string | undefined, fallback: string): string {
  return value && /^#([0-9a-f]{3}){1,2}$/i.test(value) ? value : fallback
}

type LoadStatus = 'loading' | 'ready' | 'error'

/**
 * Bọc `hanzi-writer` (§5.3.2). LUÔN truyền `charDataLoader` đọc file cục bộ (R-W1 — loader mặc định gọi CDN).
 * - Tạo trong `useEffect` theo `[hanzi, mode, speed]`; cleanup `cancelQuiz()` + xoá `innerHTML` (thư viện không có
 *   `destroy`; StrictMode React 19 chạy effect hai lần ⇒ không được để 2 SVG chồng nhau — RK39). Cờ `disposed`
 *   chặn callback tải dữ liệu về muộn sau khi đã dọn.
 * - Vùng vẽ `touch-action: none; user-select: none`: vuốt ngón tay để viết không cuộn trang (RK38).
 * - `guided`: `quiz({ showHintAfterMisses: 2 })` trên nền outline; `recall`: không outline, không chữ, ngưỡng 3.
 * - Đếm gợi ý (R-W3): nét đạt ngưỡng tự hiện (`mistakesOnStroke === ngưỡng`) + số lần bấm "Gợi ý nét".
 */
export function HanziWriterBoard({
  hanzi,
  mode,
  size,
  animationSpeed = 1,
  onComplete,
  onStart,
  onMistake,
  onCorrectStroke,
  onLoadError,
  ref,
}: HanziWriterBoardProps) {
  const theme = useTheme()
  const hostRef = useRef<HTMLDivElement>(null)
  const writerRef = useRef<HanziWriter | null>(null)
  const startQuizRef = useRef<(() => void) | null>(null)
  const [status, setStatus] = useState<LoadStatus>('loading')
  // Nút "Thử lại" khi tải dữ liệu nét lỗi — tăng để effect tạo lại bảng.
  const [nonce, setNonce] = useState(0)

  // Callback mới nhất trong ref: effect tạo bảng KHÔNG phụ thuộc vào callback (đổi callback không tạo lại bảng).
  const callbacks = useRef({ onComplete, onStart, onMistake, onCorrectStroke, onLoadError })
  callbacks.current = { onComplete, onStart, onMistake, onCorrectStroke, onLoadError }

  // Trạng thái lượt hiện tại (số gợi ý, nét đang chờ, mốc bắt đầu) — chỉ đọc/ghi trong callback của hanzi-writer.
  const attempt = useRef({ hints: 0, currentStroke: 0, startedAt: 0, totalStrokes: 0 })

  const isDark = theme.palette.mode === 'dark'
  const strokeColor = isDark ? '#ECEFF1' : '#212121'
  const outlineColor = isDark ? '#4A5550' : '#CFD8DC'
  const drawingColor = hexOr(theme.palette.secondary.main, isDark ? '#EF5350' : '#C62828')
  const highlightColor = hexOr(theme.palette.primary.main, isDark ? '#66BB6A' : '#2E7D32')

  useEffect(() => {
    const host = hostRef.current
    if (!host) return
    let disposed = false
    setStatus('loading')

    const writer = HanziWriter.create(host, hanzi, {
      width: size,
      height: size,
      padding: PADDING,
      renderer: 'svg',
      showOutline: mode !== 'recall',
      showCharacter: mode === 'view',
      strokeAnimationSpeed: animationSpeed,
      delayBetweenStrokes: 350,
      delayBetweenLoops: 1500,
      strokeColor,
      outlineColor,
      drawingColor,
      highlightColor,
      drawingWidth: 6,
      // R-W1: loader cục bộ. Giữ lại số nét để báo `totalStrokes` (không lấy từ `stroke_count` Unihan — R-W7).
      charDataLoader: (char, onLoad, onError) => {
        loadCharData(char).then(
          (data) => {
            if (disposed) return
            attempt.current.totalStrokes = data.strokes.length
            onLoad(data)
          },
          (err: unknown) => {
            if (!disposed) onError(err)
          },
        )
      },
      onLoadCharDataSuccess: () => {
        if (!disposed) setStatus('ready')
      },
      onLoadCharDataError: () => {
        if (disposed) return
        setStatus('error')
        callbacks.current.onLoadError?.()
      },
    })
    writerRef.current = writer

    const threshold = mode === 'view' ? 0 : HINT_THRESHOLD[mode]
    const startQuiz = () => {
      if (disposed || mode === 'view') return
      attempt.current.hints = 0
      attempt.current.currentStroke = 0
      attempt.current.startedAt = performance.now()
      callbacks.current.onStart?.()
      void writer.quiz({
        showHintAfterMisses: threshold,
        highlightOnComplete: true,
        onMistake: (d) => {
          if (disposed) return
          if (d.mistakesOnStroke === threshold) attempt.current.hints += 1
          callbacks.current.onMistake?.(d)
        },
        onCorrectStroke: (d) => {
          if (disposed) return
          attempt.current.currentStroke = d.strokeNum + 1
          callbacks.current.onCorrectStroke?.(d)
        },
        onComplete: ({ totalMistakes }) => {
          if (disposed) return
          const a = attempt.current
          callbacks.current.onComplete?.({
            totalMistakes,
            hintsUsed: a.hints,
            totalStrokes: Math.min(64, Math.max(1, a.totalStrokes)),
            durationMs: Math.min(MAX_DURATION_MS, Math.max(0, Math.round(performance.now() - a.startedAt))),
          })
        },
      })
    }
    startQuizRef.current = startQuiz

    if (mode === 'view') void writer.animateCharacter()
    else startQuiz()

    return () => {
      disposed = true
      startQuizRef.current = null
      writerRef.current = null
      try {
        writer.cancelQuiz()
      } catch {
        /* chưa tải xong dữ liệu — không có quiz để huỷ */
      }
      // Không có `destroy()`: xoá SVG do thư viện chèn để lần tạo kế (StrictMode/đổi chữ) không chồng 2 bảng.
      host.innerHTML = ''
    }
    // Màu theo theme cố ý KHÔNG nằm trong deps: đổi sáng/tối giữa lượt không được tạo lại bảng (mất lượt đang viết).
  }, [hanzi, mode, animationSpeed, nonce])

  // Đổi cỡ (xoay máy) ⇒ chỉ cập nhật kích thước, giữ nguyên trạng thái lượt.
  useEffect(() => {
    writerRef.current?.updateDimensions({ width: size, height: size, padding: PADDING })
  }, [size])

  useImperativeHandle(
    ref,
    () => ({
      animate: () => void writerRef.current?.animateCharacter(),
      loop: () => void writerRef.current?.loopCharacterAnimation(),
      // `showCharacter` cùng scope `character.main` ⇒ huỷ chuỗi lặp đang chạy rồi hiện nguyên chữ.
      stopLoop: () => void writerRef.current?.showCharacter(),
      hint: () => {
        const w = writerRef.current
        if (!w) return
        attempt.current.hints += 1
        void w.highlightStroke(attempt.current.currentStroke)
      },
      restart: () => {
        const w = writerRef.current
        if (!w) return
        try {
          w.cancelQuiz()
        } catch {
          /* bỏ qua */
        }
        startQuizRef.current?.()
      },
    }),
    [],
  )

  return (
    <Box sx={{ position: 'relative', width: size, height: size, mx: 'auto', flexShrink: 0 }}>
      <MiZiGrid size={size} />
      <Box
        ref={hostRef}
        lang="zh-CN"
        sx={{
          position: 'relative',
          width: size,
          height: size,
          // RK38: ngón tay vẽ trên bảng không được cuộn/zoom trang; không chọn văn bản khi kéo.
          touchAction: 'none',
          userSelect: 'none',
          WebkitUserSelect: 'none',
          WebkitTouchCallout: 'none',
          cursor: mode === 'view' ? 'default' : 'crosshair',
          '& svg': { display: 'block' },
        }}
      />
      {status === 'loading' && (
        <Box sx={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', pointerEvents: 'none' }}>
          <CircularProgress size={32} aria-label="Đang tải dữ liệu nét" />
        </Box>
      )}
      {status === 'error' && (
        <Box sx={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 2 }}>
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => setNonce((n) => n + 1)}>
                Thử lại
              </Button>
            }
          >
            Không tải được dữ liệu nét của chữ này.
          </Alert>
        </Box>
      )}
    </Box>
  )
}
