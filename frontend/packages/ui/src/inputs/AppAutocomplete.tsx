import type { ReactNode } from 'react'
import { Autocomplete, TextField, type AutocompleteProps, type TextFieldProps } from '@mui/material'

export interface AppAutocompleteProps<
  Value,
  Multiple extends boolean | undefined = false,
  DisableClearable extends boolean | undefined = false,
  FreeSolo extends boolean | undefined = false,
> extends Omit<AutocompleteProps<Value, Multiple, DisableClearable, FreeSolo>, 'renderInput'> {
  label?: ReactNode
  helperText?: ReactNode
  error?: boolean
  required?: boolean
  placeholder?: string
  /** `name`/`autoComplete`/`inputRef`... truyền thêm cho `TextField` bên trong. */
  textFieldProps?: Omit<TextFieldProps, 'label' | 'helperText' | 'error' | 'required' | 'placeholder' | 'slotProps'> & {
    slotProps?: TextFieldProps['slotProps']
  }
  /** Cho phép ghi đè `renderInput` hoàn toàn (hiếm khi cần). */
  renderInput?: AutocompleteProps<Value, Multiple, DisableClearable, FreeSolo>['renderInput']
}

/**
 * Bọc `Autocomplete` MUI v9 với `TextField` chuẩn (F4 §5.3.E) — MẪU CHUẨN cho mọi Autocomplete về sau.
 *
 * Bẫy lớn nhất (CLAUDE.md, sự cố MedDental 29/08/2026): trong `renderInput`, đè `slotProps` sau `{...params}`
 * mà không trải `params.slotProps` trước ⇒ mất `ref` của <input> ⇒ ô không bao giờ hiện gợi ý, không một lỗi
 * nào nổi lên. Ở đây luôn trải `params.slotProps` trước rồi mới gộp slot con từ `textFieldProps.slotProps`.
 */
export function AppAutocomplete<
  Value,
  Multiple extends boolean | undefined = false,
  DisableClearable extends boolean | undefined = false,
  FreeSolo extends boolean | undefined = false,
>({
  label,
  helperText,
  error,
  required,
  placeholder,
  textFieldProps,
  renderInput,
  ...autocompleteProps
}: AppAutocompleteProps<Value, Multiple, DisableClearable, FreeSolo>) {
  const { slotProps: extraSlotProps, ...restTextFieldProps } = textFieldProps ?? {}
  return (
    <Autocomplete
      {...autocompleteProps}
      renderInput={
        renderInput ??
        ((params) => (
          <TextField
            {...params}
            {...restTextFieldProps}
            label={label}
            placeholder={placeholder}
            helperText={helperText}
            error={error}
            required={required}
            slotProps={{
              ...params.slotProps,
              // Slot con: giữ nguyên của Autocomplete rồi mới đè phần bên gọi thêm (không được thay cả object).
              input: { ...params.slotProps.input, ...(extraSlotProps?.input as object | undefined) },
              inputLabel: { ...params.slotProps.inputLabel, ...(extraSlotProps?.inputLabel as object | undefined) },
              htmlInput: { ...params.slotProps.htmlInput, ...(extraSlotProps?.htmlInput as object | undefined) },
              ...(extraSlotProps?.formHelperText ? { formHelperText: extraSlotProps.formHelperText } : {}),
              ...(extraSlotProps?.select ? { select: extraSlotProps.select } : {}),
            }}
          />
        ))
      }
    />
  )
}
