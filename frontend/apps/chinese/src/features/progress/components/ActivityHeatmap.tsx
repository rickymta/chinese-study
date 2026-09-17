import { useEffect, useMemo, useState } from 'react'
import { alpha, Box, Tooltip, Typography, useTheme } from '@mui/material'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import { formatDdMm, WEEKDAY_SHORT_VI } from '../lib/dates'
import { buildHeatmap, type HeatLevel, type HeatmapCell } from '../lib/heatmap'
import type { ActivityDay } from '../types'
import { DashboardCard } from './DashboardCard'

/** Ô 16px + khe 3px ⇒ 14 cột ≈ 266px + cột nhãn thứ 22px: vừa 375px sau padding trang + thẻ. */
const CELL = 16
const GAP = 3
const LABEL_COL = 22

interface Props {
  activity: ActivityDay[]
  /** `localDate` của server. */
  today: string
}

/**
 * Lịch hoạt động 90 ngày (R-PG6) — lưới Box tự vẽ, không thư viện biểu đồ. Cột = tuần (Thứ Hai đầu), nhãn tháng phía
 * trên, nhãn T2/T4/T6 bên trái. Màu = alpha của `primary` theo mức 1–4 nên đủ tương phản cả sáng/tối; ô hôm nay có
 * viền. Rê/focus ô ⇒ Tooltip "dd/MM: N lượt"; CHẠM/bấm ô ⇒ ghim tooltip của ô đó {@link PIN_MS} ms (xem `Cell`).
 */
export function ActivityHeatmap({ activity, today }: Props) {
  const theme = useTheme()
  const grid = useMemo(() => buildHeatmap(activity, today), [activity, today])
  // Ô đang được ghim tooltip do chạm — một ô tại một thời điểm, tự bỏ ghim sau PIN_MS.
  const [pinned, setPinned] = useState<string | null>(null)
  useEffect(() => {
    if (!pinned) return
    const t = window.setTimeout(() => setPinned(null), PIN_MS)
    return () => window.clearTimeout(t)
  }, [pinned])

  const colorOf = (level: HeatLevel): string => {
    const base = theme.palette.primary.main
    switch (level) {
      case 0:
        return alpha(theme.palette.text.primary, 0.08)
      case 1:
        return alpha(base, 0.3)
      case 2:
        return alpha(base, 0.55)
      case 3:
        return alpha(base, 0.8)
      case 4:
        return base
    }
  }

  const columns = grid.weeks.length
  const gridWidth = columns * CELL + Math.max(0, columns - 1) * GAP

  return (
    <DashboardCard
      title="90 ngày qua"
      icon={<CalendarMonthOutlinedIcon />}
      aside={
        <Typography variant="caption" color="text.secondary" noWrap>
          {grid.activeDays} ngày có học · {grid.total} lượt
        </Typography>
      }
    >
      {/* Cuộn ngang chỉ khi thật sự hẹp hơn 288px (không xảy ra ở 375px) — không để tràn trang. */}
      <Box sx={{ overflowX: 'auto', pb: 0.5 }}>
        <Box sx={{ display: 'inline-block', minWidth: LABEL_COL + gridWidth }}>
          {/* Hàng nhãn tháng: định vị tuyệt đối theo chỉ số cột. */}
          <Box sx={{ position: 'relative', height: 16, ml: `${LABEL_COL}px`, width: gridWidth }}>
            {grid.monthLabels.map((m) => (
              <Typography
                key={m.weekIndex}
                variant="caption"
                color="text.secondary"
                sx={{ position: 'absolute', left: m.weekIndex * (CELL + GAP), top: 0, lineHeight: '16px', whiteSpace: 'nowrap' }}
              >
                {m.label}
              </Typography>
            ))}
          </Box>

          <Box sx={{ display: 'flex', gap: `${GAP}px` }}>
            {/* Cột nhãn thứ — chỉ T2/T4/T6 cho thoáng. */}
            <Box sx={{ display: 'grid', gridTemplateRows: `repeat(7, ${CELL}px)`, rowGap: `${GAP}px`, width: LABEL_COL - GAP, flexShrink: 0 }}>
              {WEEKDAY_SHORT_VI.map((d, i) => (
                <Typography key={d} variant="caption" color="text.secondary" sx={{ lineHeight: `${CELL}px`, fontSize: 10 }}>
                  {i % 2 === 0 ? d : ''}
                </Typography>
              ))}
            </Box>

            {/*
              ARIA hợp lệ: grid > row (mỗi thứ trong tuần) > gridcell. DOM đi theo HÀNG (Thứ Hai của mọi tuần, rồi
              Thứ Ba…) — CSS grid đặt theo cột `columns`; `display: contents` để hàng không phá lưới.
            */}
            <Box
              role="grid"
              aria-label={`Lịch hoạt động 90 ngày: ${grid.activeDays} ngày có học, ${grid.total} lượt`}
              aria-rowcount={7}
              aria-colcount={columns}
              sx={{ display: 'grid', gridTemplateColumns: `repeat(${columns}, ${CELL}px)`, gap: `${GAP}px` }}
            >
              {WEEKDAY_SHORT_VI.map((dayLabel, di) => (
                <Box key={dayLabel} role="row" aria-label={dayLabel} sx={{ display: 'contents' }}>
                  {grid.weeks.map((week, wi) => {
                    const cell = week[di]
                    return cell ? (
                      <Cell
                        key={cell.date}
                        cell={cell}
                        color={colorOf(cell.level)}
                        pinned={pinned === cell.date}
                        onPin={() => setPinned((cur) => (cur === cell.date ? null : cell.date))}
                      />
                    ) : (
                      <Box key={`${di}-${wi}`} role="gridcell" sx={{ width: CELL, height: CELL }} />
                    )
                  })}
                </Box>
              ))}
            </Box>
          </Box>
        </Box>
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, justifyContent: 'flex-end' }}>
        <Typography variant="caption" color="text.secondary" sx={{ mr: 0.5 }}>
          Ít
        </Typography>
        {([0, 1, 2, 3, 4] as HeatLevel[]).map((lv) => (
          <Box key={lv} sx={{ width: 12, height: 12, borderRadius: 0.5, bgcolor: colorOf(lv) }} />
        ))}
        <Typography variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
          Nhiều
        </Typography>
      </Box>
    </DashboardCard>
  )
}

/** Thời gian giữ tooltip sau khi chạm một ô. */
const PIN_MS = 3000

interface CellProps {
  cell: HeatmapCell
  color: string
  pinned: boolean
  onPin: () => void
}

/**
 * Tooltip CÓ ĐIỀU KHIỂN: `hovered` do MUI báo qua `onOpen`/`onClose` (chuột, focus bàn phím); `pinned` do chạm/bấm.
 * Không dùng bộ xử lý chạm của MUI (`disableTouchListener`): đo ở tích hợp F11, chạm nhanh (< 100 ms) không mở được, và
 * chuột giả lập mà trình duyệt phát sau khi chạm (`mouseout`) đóng tooltip gần như ngay lập tức. Popper đặt phía trên
 * (ngón tay che phía dưới), `disableInteractive` để popper không chắn con trỏ.
 */
function Cell({ cell, color, pinned, onPin }: CellProps) {
  const label = `${formatDdMm(cell.date)}: ${cell.count} lượt`
  const [hovered, setHovered] = useState(false)
  return (
    <Tooltip
      title={label}
      open={pinned || hovered}
      onOpen={() => setHovered(true)}
      onClose={() => setHovered(false)}
      placement="top"
      disableInteractive
      disableTouchListener
      enterDelay={0}
      slotProps={{ popper: { modifiers: [{ name: 'offset', options: { offset: [0, 2] } }] } }}
    >
      <Box
        role="gridcell"
        tabIndex={0}
        aria-label={label}
        onClick={onPin}
        sx={{
          width: CELL,
          height: CELL,
          borderRadius: 0.5,
          bgcolor: color,
          outline: cell.isToday ? '2px solid' : 'none',
          outlineColor: 'secondary.main',
          outlineOffset: -1,
          cursor: 'pointer',
          '&:focus-visible': { outline: '2px solid', outlineColor: 'primary.main' },
        }}
      />
    </Tooltip>
  )
}
