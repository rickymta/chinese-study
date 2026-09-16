import { Box, type BoxProps } from '@mui/material'
import type { ElementType, ReactNode } from 'react'

export interface LangTextProps extends Omit<BoxProps, 'component'> {
  /** Mã ngôn ngữ BCP 47 của nội dung (`zh-CN`, `zh-TW`, `ja`, `en`...). */
  lang: string
  children: ReactNode
  /** Phần tử HTML/Component bọc — mặc định `span`. */
  component?: ElementType
}

/**
 * Văn bản có gắn `lang` + phông theo ngôn ngữ (hợp đồng R-C7). Tên chung `LangText` vì package dùng cho mọi
 * ngôn ngữ; app tiếng Trung có thể bọc thành `Hanzi` riêng.
 *
 * Với `zh-*` (và các ngôn ngữ CJK khác) dùng biến CSS `--af-font-cjk` do `buildTheme` phát ra ở `:root` —
 * không có `lang` đúng thì trình duyệt có thể chọn glyph kiểu Nhật cho cùng một mã Unicode.
 */
export function LangText({ lang, children, component = 'span', sx, ...props }: LangTextProps) {
  const isCjk = /^(zh|ja|ko)(-|$)/i.test(lang)
  return (
    <Box
      component={component}
      lang={lang}
      sx={{ ...(isCjk ? { fontFamily: 'var(--af-font-cjk)' } : {}), ...sx }}
      {...props}
    >
      {children}
    </Box>
  )
}
