import { Box, Typography, useTheme } from '@mui/material'

/**
 * Đường nét cao độ 5 mức (thang Chao) của từng thanh: 1 = 55, 2 = 35, 3 = 214, 4 = 51; thanh nhẹ vẽ chấm ngắn ở mức 3.
 * Vẽ bằng SVG `polyline` thuần, không thư viện (D-BA §5.3.D).
 */
const CONTOURS: Record<number, { label: string; points: number[]; name: string }> = {
  1: { label: '55', points: [5, 5], name: 'Thanh 1 — cao, bằng' },
  2: { label: '35', points: [3, 5], name: 'Thanh 2 — đi lên' },
  3: { label: '214', points: [2, 1, 4], name: 'Thanh 3 — xuống rồi lên' },
  4: { label: '51', points: [5, 1], name: 'Thanh 4 — xuống mạnh' },
  5: { label: '·', points: [3, 3], name: 'Thanh nhẹ — ngắn' },
}

const W = 96
const H = 72
const PAD = 8

function toPolyline(points: number[]): string {
  const stepX = (W - PAD * 2) / Math.max(points.length - 1, 1)
  return points
    .map((level, i) => {
      const x = PAD + i * stepX
      const y = H - PAD - ((level - 1) / 4) * (H - PAD * 2)
      return `${x},${y}`
    })
    .join(' ')
}

export function ToneContour({ tones }: { tones: number[] }) {
  const theme = useTheme()
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, my: 1 }}>
      {tones.map((t) => {
        const c = CONTOURS[t]
        if (!c) return null
        const isNeutral = t === 5
        return (
          <Box
            key={t}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              border: 1,
              borderColor: 'divider',
              borderRadius: 2,
              p: 1,
              minWidth: 112,
            }}
          >
            <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} role="img" aria-label={c.name}>
              {/* 5 mức cao độ */}
              {[1, 2, 3, 4, 5].map((lv) => {
                const y = H - PAD - ((lv - 1) / 4) * (H - PAD * 2)
                return <line key={lv} x1={PAD} x2={W - PAD} y1={y} y2={y} stroke={theme.palette.divider} strokeWidth={1} />
              })}
              <polyline
                points={toPolyline(isNeutral ? [3, 3] : c.points)}
                fill="none"
                stroke={theme.palette.primary.main}
                strokeWidth={isNeutral ? 6 : 4}
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeDasharray={isNeutral ? '2 10' : undefined}
              />
            </svg>
            <Typography variant="caption" sx={{ fontWeight: 600 }}>
              {t === 5 ? 'Thanh nhẹ' : `Thanh ${t}`} · {c.label}
            </Typography>
          </Box>
        )
      })}
    </Box>
  )
}
