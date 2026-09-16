import { z } from 'zod'

// Schema zod 4 dùng chung cho biểu mẫu tài khoản (hợp đồng §5.3.1, quy tắc R-A1/R-A2, độ dài cột §5.1.1).
// Thông điệp tiếng Việt có dấu — hiển thị thẳng dưới ô nhập.

/** Email: cắt khoảng trắng, tối đa 254 ký tự (cột `email varchar(254)`). So khớp hoa/thường do backend lo (R-A1). */
export const emailSchema = z
  .string({ error: 'Vui lòng nhập email' })
  .trim()
  .min(1, { error: 'Vui lòng nhập email' })
  .max(254, { error: 'Email quá dài (tối đa 254 ký tự)' })
  .pipe(z.email({ error: 'Email không hợp lệ' }))

/** Mật khẩu 8–128 ký tự, KHÔNG bắt buộc ký tự đặc biệt (R-A2, theo NIST SP 800-63B). */
export const passwordSchema = z
  .string({ error: 'Vui lòng nhập mật khẩu' })
  .min(8, { error: 'Mật khẩu cần ít nhất 8 ký tự' })
  .max(128, { error: 'Mật khẩu tối đa 128 ký tự' })

/** Tên hiển thị 1–100 ký tự (cột `display_name varchar(100)`). */
export const displayNameSchema = z
  .string({ error: 'Vui lòng nhập tên hiển thị' })
  .trim()
  .min(1, { error: 'Vui lòng nhập tên hiển thị' })
  .max(100, { error: 'Tên hiển thị tối đa 100 ký tự' })

/** Múi giờ IANA (`Asia/Ho_Chi_Minh`), tối đa 64 ký tự (cột `time_zone varchar(64)`); tính hợp lệ do backend kiểm (422 `INVALID_TIME_ZONE`). */
export const timeZoneSchema = z
  .string({ error: 'Vui lòng chọn múi giờ' })
  .trim()
  .min(1, { error: 'Vui lòng chọn múi giờ' })
  .max(64, { error: 'Múi giờ không hợp lệ' })
