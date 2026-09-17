import { useCallback, useState, type ReactNode, type Ref } from 'react'
import { Box, Button, Chip, Typography } from '@mui/material'
import ReplayIcon from '@mui/icons-material/Replay'
import LightbulbOutlinedIcon from '@mui/icons-material/LightbulbOutlined'
import { HanziWriterBoard, HINT_THRESHOLD, type AttemptSummary, type HanziWriterBoardHandle } from './HanziWriterBoard'

export interface StepWriteProps {
  hanzi: string
  mode: 'guided' | 'recall'
  size: number
  /** Lượt (lại) bắt đầu — trang sinh `clientAttemptId`. */
  onStart: () => void
  /** Lượt hoàn tất — trang gửi server. */
  onComplete: (summary: AttemptSummary) => void
  /** Lượt đã xong ⇒ ẩn nút thao tác trên bảng (trang hiện `AttemptResult` bên dưới). */
  finished: boolean
  /** Lời hướng dẫn riêng của bước. */
  instruction: ReactNode
  ref?: Ref<HanziWriterBoardHandle>
}

/**
 * Phần dùng chung của bước Tô theo / Tự viết: bảng viết + bộ đếm nét/lỗi trực tiếp + nút "Viết lại" và (Tự viết)
 * "Gợi ý nét". Trang giữ `ref` bảng để "Viết lại" từ khối kết quả cũng gọi được `restart()`.
 */
export function StepWrite({ hanzi, mode, size, onStart, onComplete, finished, instruction, ref }: StepWriteProps) {
  const [live, setLive] = useState({ strokesDone: 0, mistakes: 0, hints: 0 })
  const threshold = HINT_THRESHOLD[mode]

  // Bộ đếm hiển thị chỉ để người học thấy tiến trình; số liệu gửi server lấy từ `AttemptSummary` của bảng.
  const handleStart = useCallback(() => {
    setLive({ strokesDone: 0, mistakes: 0, hints: 0 })
    onStart()
  }, [onStart])

  // `ref` có thể là hàm hoặc object — gán tay để vừa giữ bản sao cục bộ (nút Gợi ý/Viết lại) vừa chuyển cho trang.
  const [handle, setHandle] = useState<HanziWriterBoardHandle | null>(null)
  const attachRef = useCallback(
    (h: HanziWriterBoardHandle | null) => {
      setHandle(h)
      if (typeof ref === 'function') ref(h)
      else if (ref) ref.current = h
    },
    [ref],
  )

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1.5 }}>
      <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
        {instruction}
      </Typography>
      <HanziWriterBoard
        ref={attachRef}
        hanzi={hanzi}
        mode={mode}
        size={size}
        onStart={handleStart}
        onComplete={onComplete}
        onMistake={(d) =>
          setLive((s) => ({
            ...s,
            mistakes: d.totalMistakes,
            hints: d.mistakesOnStroke === threshold ? s.hints + 1 : s.hints,
          }))
        }
        onCorrectStroke={(d) => setLive((s) => ({ ...s, strokesDone: d.strokeNum + 1 }))}
      />
      <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', justifyContent: 'center' }} aria-live="polite">
        <Chip size="small" variant="outlined" label={`Nét đúng: ${live.strokesDone}`} />
        <Chip size="small" variant="outlined" color={live.mistakes > 0 ? 'warning' : 'default'} label={`Lỗi: ${live.mistakes}`} />
        <Chip size="small" variant="outlined" label={`Gợi ý: ${live.hints}`} />
      </Box>
      {!finished && (
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', justifyContent: 'center' }}>
          {mode === 'recall' && (
            <Button
              variant="outlined"
              startIcon={<LightbulbOutlinedIcon />}
              onClick={() => {
                handle?.hint()
                setLive((s) => ({ ...s, hints: s.hints + 1 }))
              }}
              sx={{ minHeight: 44 }}
            >
              Gợi ý nét
            </Button>
          )}
          <Button variant="text" startIcon={<ReplayIcon />} onClick={() => handle?.restart()} sx={{ minHeight: 44 }}>
            Viết lại
          </Button>
        </Box>
      )}
    </Box>
  )
}
