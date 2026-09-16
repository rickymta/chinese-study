import type { UseFormRegisterReturn } from 'react-hook-form'

/**
 * `register()` của react-hook-form trả `ref` dành cho <input>, nhưng `ref` của MUI `TextField` trỏ vào <div> gốc
 * ⇒ phải chuyển sang `inputRef` để RHF focus đúng ô khi lỗi và đọc giá trị mặc định. Dùng: `<TextField {...bindField(register('email'))} />`.
 */
export function bindField(reg: UseFormRegisterReturn) {
  const { ref, ...rest } = reg
  return { inputRef: ref, ...rest }
}
