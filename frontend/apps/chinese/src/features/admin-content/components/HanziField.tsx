import { TextField, type TextFieldProps } from '@mui/material'

export interface HanziFieldProps extends Omit<TextFieldProps, 'value' | 'onChange'> {
  value: string
  onChange: (value: string) => void
}

/** Ô nhập chữ Hán: `lang="zh-CN"` + phông CJK (quy ước CLAUDE.md) để glyph giản thể hiện đúng ngay lúc gõ. */
export function HanziField({ value, onChange, ...rest }: HanziFieldProps) {
  return (
    <TextField
      {...rest}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      autoComplete="off"
      slotProps={{
        ...rest.slotProps,
        htmlInput: {
          lang: 'zh-CN',
          spellCheck: false,
          style: { fontFamily: 'var(--af-font-cjk)', fontSize: 18 },
          ...(rest.slotProps?.htmlInput as object | undefined),
        },
      }}
    />
  )
}
