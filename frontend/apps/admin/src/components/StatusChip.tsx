import { Chip, type ChipProps } from '@mui/material'

export interface StatusChipOption {
  label: string
  color: ChipProps['color']
}

export interface StatusChipProps {
  /** Mã trạng thái thô từ API (`open`, `draft`, `published`...). */
  value: string
  /** Bảng mã → nhãn/màu; mã lạ hiện nguyên mã, màu `default`. */
  options: Record<string, StatusChipOption>
  size?: ChipProps['size']
}

/** Chip trạng thái dùng chung (W3a — ngôn ngữ; W3b+ FAQ, trang, bài viết): nhãn tiếng Việt theo bảng tra. */
export function StatusChip({ value, options, size = 'small' }: StatusChipProps) {
  const opt = options[value]
  return <Chip size={size} label={opt?.label ?? value} color={opt?.color ?? 'default'} variant={opt ? 'filled' : 'outlined'} />
}
