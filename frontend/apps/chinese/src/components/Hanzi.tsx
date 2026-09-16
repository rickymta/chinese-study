import { LangText, type LangTextProps } from '@af/ui'

export type HanziSize = 'sm' | 'md' | 'lg' | 'xl' | 'xxl'

/** Cỡ chữ (px) theo tên — chữ Hán cần lớn hơn chữ Latin cùng cấp để thấy rõ nét. */
const SIZE_PX: Record<HanziSize, number> = { sm: 18, md: 24, lg: 36, xl: 56, xxl: 80 }

export interface HanziProps extends Omit<LangTextProps, 'lang'> {
  size?: HanziSize
}

/**
 * Chữ Hán: bọc `LangText lang="zh-CN"` (phông CJK fallback + glyph giản thể đúng) với cỡ chữ đặt tên (§5.3.D).
 * Mọi chỗ hiển thị chữ Hán trong app dùng component này (review §9.3).
 */
export function Hanzi({ size = 'md', sx, children, ...props }: HanziProps) {
  return (
    <LangText
      lang="zh-CN"
      sx={[{ fontSize: SIZE_PX[size], lineHeight: 1.25, fontWeight: 400 }, ...(Array.isArray(sx) ? sx : [sx])]}
      {...props}
    >
      {children}
    </LangText>
  )
}
