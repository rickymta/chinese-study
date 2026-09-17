import { useRef, useState } from 'react'
import { Box, Button, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material'
import ReplayIcon from '@mui/icons-material/Replay'
import LoopIcon from '@mui/icons-material/Loop'
import StopIcon from '@mui/icons-material/Stop'
import { HanziWriterBoard, type HanziWriterBoardHandle } from './HanziWriterBoard'

export interface StepViewProps {
  hanzi: string
  size: number
}

type Speed = 0.5 | 1

/**
 * Bước Xem (R-W2, không ghi DB): hoạt hình thứ tự nét chạy ngay khi mở; nút Phát lại / Lặp / Tốc độ (0,5× cho
 * chữ nhiều nét). Đổi tốc độ tạo lại bảng (tuỳ chọn của hanzi-writer đặt lúc tạo) — bước này không có lượt nên
 * không mất gì.
 */
export function StepView({ hanzi, size }: StepViewProps) {
  const board = useRef<HanziWriterBoardHandle>(null)
  const [speed, setSpeed] = useState<Speed>(1)
  const [looping, setLooping] = useState(false)

  const toggleLoop = () => {
    if (looping) {
      board.current?.stopLoop()
      setLooping(false)
    } else {
      board.current?.loop()
      setLooping(true)
    }
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1.5 }}>
      <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
        Nhìn thứ tự và hướng từng nét. Chữ nhiều nét thì bật tốc độ 0,5×.
      </Typography>
      <HanziWriterBoard key={speed} hanzi={hanzi} mode="view" size={size} animationSpeed={speed} />
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', justifyContent: 'center', alignItems: 'center' }}>
        <Button
          variant="contained"
          startIcon={<ReplayIcon />}
          onClick={() => {
            if (looping) setLooping(false)
            board.current?.animate()
          }}
          sx={{ minHeight: 44 }}
        >
          Phát lại
        </Button>
        <Button variant="outlined" startIcon={looping ? <StopIcon /> : <LoopIcon />} onClick={toggleLoop} sx={{ minHeight: 44 }}>
          {looping ? 'Dừng lặp' : 'Lặp'}
        </Button>
        <ToggleButtonGroup
          exclusive
          size="small"
          value={speed}
          onChange={(_e, v: Speed | null) => {
            if (v !== null) {
              setLooping(false)
              setSpeed(v)
            }
          }}
          aria-label="Tốc độ hoạt hình"
        >
          <ToggleButton value={0.5} aria-label="Chậm 0,5 lần">
            0,5×
          </ToggleButton>
          <ToggleButton value={1} aria-label="Tốc độ thường">
            1×
          </ToggleButton>
        </ToggleButtonGroup>
      </Box>
    </Box>
  )
}
