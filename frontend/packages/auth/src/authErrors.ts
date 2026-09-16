import { parseApiError, type ParsedApiError } from '@af/utils'

/** Kết quả diễn giải lỗi xác thực: thông điệp chung + lỗi gắn vào từng ô (nếu có). */
export interface AuthErrorView {
  message: string
  /** Lỗi theo trường để `setError` của react-hook-form (khoá camelCase: `email`, `password`, `displayName`, `timeZone`). */
  fieldErrors: Record<string, string>
  parsed: ParsedApiError
}

function formatLockedUntil(value: unknown): string | null {
  if (typeof value !== 'string') return null
  const t = Date.parse(value)
  if (!Number.isFinite(t)) return null
  return new Date(t).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

/**
 * Diễn giải lỗi của identity-service (mã §6.0/§6.2) thành lời tiếng Việt để hiện `Alert` tại chỗ
 * (hợp đồng §5.3.1: 401/423/403/409 hiện tại chỗ). Mã lạ ⇒ dùng `error` của máy chủ hoặc thông điệp mặc định.
 */
export function describeAuthError(err: unknown): AuthErrorView {
  const parsed = parseApiError(err)
  const fieldErrors: Record<string, string> = {}
  for (const [field, messages] of Object.entries(parsed.fieldErrors ?? {})) {
    if (messages[0]) fieldErrors[field] = messages[0]
  }

  let message = parsed.message
  switch (parsed.code) {
    case 'INVALID_CREDENTIALS':
      message = 'Email hoặc mật khẩu không đúng.'
      break
    case 'ACCOUNT_LOCKED': {
      const until = formatLockedUntil(parsed.details?.lockedUntil)
      message = until
        ? `Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau ${until}.`
        : 'Tài khoản tạm khoá do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.'
      break
    }
    case 'ACCOUNT_DISABLED':
      message = 'Tài khoản đã bị khoá. Vui lòng liên hệ quản trị viên.'
      break
    case 'REGISTRATION_CLOSED':
      message = 'Hệ thống hiện chưa mở đăng ký tài khoản mới.'
      break
    case 'ORIGIN_NOT_ALLOWED':
      message = 'Yêu cầu bị từ chối vì nguồn gửi không hợp lệ. Hãy mở ứng dụng bằng đúng địa chỉ chính thức.'
      break
    case 'EMAIL_TAKEN':
      fieldErrors.email ??= 'Email này đã được đăng ký.'
      message = 'Email này đã được đăng ký. Bạn có thể đăng nhập hoặc dùng email khác.'
      break
    case 'INVALID_TIME_ZONE':
      fieldErrors.timeZone ??= 'Múi giờ không hợp lệ.'
      message = 'Múi giờ không hợp lệ.'
      break
    case 'WRONG_PASSWORD':
      fieldErrors.currentPassword ??= 'Mật khẩu hiện tại không đúng.'
      message = 'Mật khẩu hiện tại không đúng.'
      break
    case 'PASSWORD_UNCHANGED':
      fieldErrors.newPassword ??= 'Mật khẩu mới phải khác mật khẩu hiện tại.'
      message = 'Mật khẩu mới phải khác mật khẩu hiện tại.'
      break
    case 'RATE_LIMITED':
      message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.'
      break
    case 'VALIDATION':
      if (Object.keys(fieldErrors).length) message = 'Vui lòng kiểm tra lại các ô được đánh dấu.'
      break
    default:
      if (parsed.status === 401) message = 'Email hoặc mật khẩu không đúng.'
      else if (parsed.status === 429) message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.'
  }

  return { message, fieldErrors, parsed }
}
