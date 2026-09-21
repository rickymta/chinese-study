import { IconButton, Tooltip } from '@mui/material'
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'

export interface ReorderButtonsProps {
  index: number
  count: number
  onMove: (from: number, to: number) => void
  /** Bỏ trống ⇒ không vẽ nút xoá (danh sách có nút xoá riêng trong thẻ). */
  onRemove?: () => void
  disabled?: boolean
  /** Nhãn cho trợ năng: "ngôn ngữ", "câu hỏi", "banner"... */
  itemLabel: string
  size?: 'small' | 'medium'
}

/**
 * Bộ nút lên/xuống(/xoá) dùng chung cho mọi danh sách sắp thứ tự trong admin — không kéo-thả (hợp đồng: nút mũi
 * tên, dùng được ở 375px). Chép từ `apps/chinese/features/admin-content`, thêm `onRemove` tuỳ chọn.
 */
export function ReorderButtons({ index, count, onMove, onRemove, disabled = false, itemLabel, size = 'small' }: ReorderButtonsProps) {
  return (
    <>
      <Tooltip title="Lên">
        <span>
          <IconButton size={size} aria-label={`Chuyển ${itemLabel} lên`} disabled={disabled || index === 0} onClick={() => onMove(index, index - 1)}>
            <ArrowUpwardIcon fontSize="inherit" />
          </IconButton>
        </span>
      </Tooltip>
      <Tooltip title="Xuống">
        <span>
          <IconButton
            size={size}
            aria-label={`Chuyển ${itemLabel} xuống`}
            disabled={disabled || index >= count - 1}
            onClick={() => onMove(index, index + 1)}
          >
            <ArrowDownwardIcon fontSize="inherit" />
          </IconButton>
        </span>
      </Tooltip>
      {onRemove && (
        <Tooltip title="Xoá">
          <span>
            <IconButton size={size} aria-label={`Xoá ${itemLabel}`} disabled={disabled} onClick={onRemove} color="error">
              <DeleteOutlineIcon fontSize="inherit" />
            </IconButton>
          </span>
        </Tooltip>
      )}
    </>
  )
}
