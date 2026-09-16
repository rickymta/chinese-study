import type { ReactNode } from 'react'
import { Button, IconButton, Tooltip, type ButtonProps, type IconButtonProps } from '@mui/material'
import VolumeUpOutlinedIcon from '@mui/icons-material/VolumeUpOutlined'
import { useChineseSpeech } from './ChineseSpeech'

export const NO_VOICE_TOOLTIP = 'Chưa có giọng tiếng Trung'

export interface SpeakButtonProps {
  /** Chữ Hán cần đọc. */
  text: string
  /** `icon` (mặc định) = nút loa tròn; `button` = nút có chữ. */
  variant?: 'icon' | 'button'
  /** Nhãn nút (dạng `button`); có chữ Hán thì bọc `LangText lang="zh-CN"`. */
  label?: ReactNode
  /** Nhãn trợ năng; mặc định lấy `label` nếu là chuỗi. */
  ariaLabel?: string
  size?: IconButtonProps['size']
  color?: ButtonProps['color']
  buttonVariant?: ButtonProps['variant']
  disabled?: boolean
  /** Gọi sau khi phát xong (hoặc bị ngắt). */
  onDone?: () => void
  sx?: ButtonProps['sx']
}

/**
 * Nút nghe dùng chung: gọi `speakZh` TRONG onClick (iOS Safari bắt buộc); không có giọng ⇒ vô hiệu + tooltip
 * (bọc `span` để tooltip vẫn hiện trên nút disabled).
 */
export function SpeakButton({
  text,
  variant = 'icon',
  label = 'Nghe',
  ariaLabel,
  size = 'medium',
  color = 'primary',
  buttonVariant = 'outlined',
  disabled,
  onDone,
  sx,
}: SpeakButtonProps) {
  const { canSpeak, speakZh } = useChineseSpeech()
  const isDisabled = disabled || !canSpeak
  const a11y = ariaLabel ?? (typeof label === 'string' ? label : 'Nghe')
  const handleClick = () => {
    void speakZh(text)
      .catch(() => undefined) // lỗi phát (not-allowed...) không làm hỏng màn hình
      .finally(() => onDone?.())
  }
  const control =
    variant === 'icon' ? (
      <IconButton aria-label={`${a11y} ${text}`} onClick={handleClick} disabled={isDisabled} size={size} color={color} sx={sx}>
        <VolumeUpOutlinedIcon fontSize={size === 'small' ? 'small' : 'medium'} />
      </IconButton>
    ) : (
      <Button
        aria-label={ariaLabel}
        onClick={handleClick}
        disabled={isDisabled}
        variant={buttonVariant}
        color={color}
        startIcon={<VolumeUpOutlinedIcon />}
        sx={sx}
      >
        {label}
      </Button>
    )
  if (!canSpeak && !disabled) {
    return (
      <Tooltip title={NO_VOICE_TOOLTIP}>
        <span style={{ display: 'inline-flex' }}>{control}</span>
      </Tooltip>
    )
  }
  return control
}
