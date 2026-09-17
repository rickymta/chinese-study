import { Box } from '@mui/material'

export interface MiZiGridProps {
  size: number
}

/**
 * Lưới 米字格 (viền + 2 đường giữa + 2 đường chéo, nét đứt) đặt TUYỆT ĐỐI dưới bảng viết — giúp người học canh
 * tỉ lệ/vị trí bộ phận của chữ như vở tập viết. Vẽ bằng SVG riêng, là ANH EM (không phải con) của div mà
 * hanzi-writer quản lý, nên khi `HanziWriterBoard` dọn `innerHTML` lưới vẫn còn.
 */
export function MiZiGrid({ size }: MiZiGridProps) {
  const s = size
  return (
    <Box
      component="svg"
      viewBox={`0 0 ${s} ${s}`}
      width={s}
      height={s}
      aria-hidden
      sx={{
        position: 'absolute',
        inset: 0,
        pointerEvents: 'none',
        color: 'divider',
        bgcolor: 'background.paper',
        borderRadius: 1,
      }}
    >
      <rect x={0.5} y={0.5} width={s - 1} height={s - 1} fill="none" stroke="currentColor" strokeWidth={1.5} />
      <g stroke="currentColor" strokeWidth={1} strokeDasharray="6 5">
        <line x1={s / 2} y1={0} x2={s / 2} y2={s} />
        <line x1={0} y1={s / 2} x2={s} y2={s / 2} />
        <line x1={0} y1={0} x2={s} y2={s} />
        <line x1={s} y1={0} x2={0} y2={s} />
      </g>
    </Box>
  )
}
