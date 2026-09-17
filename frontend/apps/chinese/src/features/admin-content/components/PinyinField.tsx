import { useMemo, useState } from 'react'
import { TextField, type TextFieldProps } from '@mui/material'
import { numberedToMarked } from '@/lib/pinyin'
import { countPinyinSyllables, pinyinProblem } from '../lib/zhText'

export interface PinyinFieldProps extends Omit<TextFieldProps, 'value' | 'onChange' | 'error' | 'helperText'> {
  /** Pinyin dạng SỐ THANH đang nhập (`ni3 hao3`). */
  value: string
  onChange: (value: string) => void
  /** Chữ Hán tương ứng — có thì kiểm số âm tiết = số chữ Hán. */
  hanzi?: string
  /** Lỗi từ server (400 theo đường dẫn) — ưu tiên hơn kiểm cục bộ, nhưng chỉ khi giá trị ô CHƯA đổi kể từ lúc nhận lỗi. */
  serverError?: string
  /** `true` ⇒ rỗng không bị coi là lỗi (trường tuỳ chọn như `promptPinyin`). */
  optional?: boolean
}

/**
 * Ô nhập pinyin số thanh (quy ước lưu trữ): helperText hiện bản DẤU (`nǐ hǎo`) để soát mắt thường; sai dạng hoặc lệch
 * số âm tiết so với chữ Hán ⇒ báo ngay (server vẫn kiểm lại). Không chặn gõ — chỉ báo.
 */
export function PinyinField({ value, onChange, hanzi, serverError, optional = false, ...rest }: PinyinFieldProps) {
  // Ghi nhớ giá trị ô lúc nhận lỗi server (mẫu "điều chỉnh state khi prop đổi" của React): người dùng sửa ô rồi thì
  // lỗi cũ (vốn nói về giá trị trước) không còn đúng — quay về kiểm cục bộ, tới lần lưu kế tiếp.
  const [trackedError, setTrackedError] = useState(serverError)
  const [errorValue, setErrorValue] = useState<string | null>(serverError ? value : null)
  if (serverError !== trackedError) {
    setTrackedError(serverError)
    setErrorValue(serverError ? value : null)
  }
  const activeServerError = serverError && errorValue === value ? serverError : undefined

  const problem = useMemo(() => {
    if (activeServerError) return activeServerError
    if (!value.trim()) return optional ? null : 'Chưa nhập pinyin.'
    if (hanzi !== undefined && hanzi.trim()) return pinyinProblem(hanzi, value)
    return countPinyinSyllables(value) === null ? 'Pinyin chưa đúng dạng số thanh (vd ni3 hao3, thanh nhẹ 5, ü viết v).' : null
  }, [activeServerError, value, hanzi, optional])

  const marked = value.trim() ? numberedToMarked(value) : ''

  return (
    <TextField
      {...rest}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      error={!!problem}
      helperText={problem ?? (marked ? `Hiển thị: ${marked}` : 'Dạng số thanh, vd ni3 hao3')}
      autoComplete="off"
      slotProps={{
        ...rest.slotProps,
        htmlInput: { autoCapitalize: 'none', spellCheck: false, ...(rest.slotProps?.htmlInput as object | undefined) },
      }}
    />
  )
}
