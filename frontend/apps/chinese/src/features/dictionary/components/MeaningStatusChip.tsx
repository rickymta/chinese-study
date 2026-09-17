import { Chip, Tooltip, type ChipProps } from '@mui/material'
import type { MeaningViStatus } from '../types'

export const MACHINE_MEANING_TOOLTIP = 'Nghĩa dịch máy, có thể chưa chính xác — đối chiếu nghĩa tiếng Anh'

export interface MeaningStatusChipProps {
  status: MeaningViStatus | null | undefined
  size?: ChipProps['size']
  sx?: ChipProps['sx']
}

/**
 * Chip "Chưa duyệt" cho nghĩa Việt `machine` (D5: mọi nghĩa đều dịch máy/CVDICT tới khi duyệt ở F10).
 * Trạng thái khác ⇒ không vẽ gì. Bọc `span` để tooltip vẫn hiện khi chip nằm trong phần tử bị disabled.
 */
export function MeaningStatusChip({ status, size = 'small', sx }: MeaningStatusChipProps) {
  if (status !== 'machine') return null
  return (
    <Tooltip title={MACHINE_MEANING_TOOLTIP} enterTouchDelay={0}>
      <span style={{ display: 'inline-flex' }}>
        <Chip size={size} variant="outlined" color="warning" label="Chưa duyệt" sx={sx} />
      </span>
    </Tooltip>
  )
}
