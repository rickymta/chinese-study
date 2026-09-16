import { useMemo, type ReactNode, type Ref } from 'react'
import { listTimeZoneOptions, matchesTimeZoneQuery, normalizeTimeZone, type TimeZoneOption } from '@af/utils'
import { AppAutocomplete } from './AppAutocomplete'

export interface TimeZoneAutocompleteProps {
  /** ID IANA đang chọn (`''` ⇒ chưa chọn). */
  value: string
  onChange: (timeZone: string) => void
  onBlur?: () => void
  label?: ReactNode
  error?: boolean
  helperText?: ReactNode
  disabled?: boolean
  required?: boolean
  name?: string
  /** `ref` của <input> để react-hook-form focus khi lỗi. */
  inputRef?: Ref<HTMLInputElement>
}

/**
 * Ô chọn múi giờ (F4 §5.3.E): danh sách từ `Intl.supportedValuesOf('timeZone')` (dự phòng ~30 múi giờ), quy bí
 * danh cũ (R4-2), luôn chứa `value` hiện tại, nhãn `(UTC+07:00) Asia/Ho_Chi_Minh` sắp theo offset rồi tên.
 * Gõ "Ho_Chi" / "ho chi" đều ra gợi ý; chọn được bằng bàn phím (mặc định của Autocomplete).
 */
export function TimeZoneAutocomplete({
  value,
  onChange,
  onBlur,
  label = 'Múi giờ',
  error,
  helperText,
  disabled,
  required,
  name,
  inputRef,
}: TimeZoneAutocompleteProps) {
  // Danh sách gốc (~400 múi giờ, mỗi cái một lần Intl.DateTimeFormat) chỉ dựng MỘT lần; `value` lạ (không có
  // trong danh sách — hiếm) mới ghép thêm, không dựng lại toàn bộ mỗi lần chọn.
  const baseOptions = useMemo(() => listTimeZoneOptions(), [])
  const normalized = value ? normalizeTimeZone(value) : ''
  const options = useMemo(
    () => (normalized && !baseOptions.some((o) => o.id === normalized) ? listTimeZoneOptions([normalized]) : baseOptions),
    [baseOptions, normalized],
  )
  const selected = useMemo(() => options.find((o) => o.id === normalized) ?? null, [options, normalized])

  return (
    <AppAutocomplete<TimeZoneOption, false, false, false>
      options={options}
      value={selected}
      onChange={(_e, opt) => onChange(opt?.id ?? '')}
      onBlur={onBlur}
      getOptionLabel={(o) => o.label}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      filterOptions={(opts, state) => opts.filter((o) => matchesTimeZoneQuery(o, state.inputValue))}
      autoHighlight
      disabled={disabled}
      label={label}
      error={error}
      helperText={helperText}
      required={required}
      fullWidth
      textFieldProps={{
        name,
        inputRef,
        // `htmlInput` gộp vào slot của Autocomplete (AppAutocomplete trải `params.slotProps` trước).
        slotProps: { htmlInput: { autoCapitalize: 'none', spellCheck: false } },
      }}
    />
  )
}
