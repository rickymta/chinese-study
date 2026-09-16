import { isApiError } from '@af/api'

/** Kết quả chuẩn hoá của MỌI lỗi (ApiError, Error thường, chuỗi, unknown) để màn hình hiển thị. */
export interface ParsedApiError {
  /** Thông điệp tiếng Việt đọc được — luôn có. */
  message: string
  /** Mã HTTP nếu lỗi tới từ máy chủ. */
  status?: number
  /** Mã lỗi nghiệp vụ theo hợp đồng §6.0 (`VALIDATION`, `EMAIL_TAKEN`, `ACCOUNT_LOCKED`...). */
  code?: string
  /** Lỗi theo trường (400 `VALIDATION`): khoá là tên trường camelCase, giá trị là danh sách thông điệp. */
  fieldErrors?: Record<string, string[]>
  /** `details` thô của thân lỗi (vd `lockedUntil` của 423). */
  details?: Record<string, unknown>
}

const DEFAULT_MESSAGE = 'Đã xảy ra lỗi không xác định.'

/** `Email` / `Account.Email` (ModelState của ASP.NET) → `email`; đã camelCase thì giữ nguyên. */
function toCamelKey(key: string): string {
  const last = key.split('.').pop() ?? key
  return last.length > 0 ? last[0]!.toLowerCase() + last.slice(1) : last
}

/**
 * Rút lỗi theo trường từ `details` (hợp đồng §6.0: `{ "email": ["Email không hợp lệ"] }`). Chấp nhận thêm dạng
 * `ValidationProblemDetails.errors` của ASP.NET phòng khi backend trả mặc định. Trả `undefined` khi không có gì.
 */
function extractFieldErrors(details: Record<string, unknown> | undefined): Record<string, string[]> | undefined {
  if (!details) return undefined
  const source =
    details.errors && typeof details.errors === 'object' ? (details.errors as Record<string, unknown>) : details
  const out: Record<string, string[]> = {}
  for (const [key, value] of Object.entries(source)) {
    const list = Array.isArray(value) ? value : typeof value === 'string' ? [value] : null
    if (!list) continue
    const messages = list.filter((m): m is string => typeof m === 'string' && m.length > 0)
    if (messages.length) out[toCamelKey(key)] = messages
  }
  return Object.keys(out).length ? out : undefined
}

/**
 * Chuẩn hoá lỗi bắt được trong `catch` thành `{ message, status?, code?, fieldErrors?, details? }`.
 * Dùng ở mọi màn hình thay vì đọc `err.response.data` rải rác. Không ném lại — đầu vào nào cũng cho ra thông điệp.
 */
export function parseApiError(err: unknown): ParsedApiError {
  if (isApiError(err)) {
    return {
      message: err.message || DEFAULT_MESSAGE,
      status: err.status,
      code: err.code,
      fieldErrors: extractFieldErrors(err.details),
      details: err.details,
    }
  }
  if (err instanceof Error) return { message: err.message || DEFAULT_MESSAGE }
  if (typeof err === 'string' && err.trim()) return { message: err }
  return { message: DEFAULT_MESSAGE }
}
