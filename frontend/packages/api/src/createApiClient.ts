import axios, { type AxiosError, type AxiosInstance } from 'axios'

/**
 * Mở rộng config axios: mỗi request có thể tắt điều hướng tự động khi lỗi (xem `maybeRedirectToErrorPage`).
 * Module augmentation ở đây ⇒ mọi app trong monorepo (import thẳng TS source của `@af/api`) đều thấy trường này.
 */
declare module 'axios' {
  interface AxiosRequestConfig {
    /**
     * `true` ⇒ TẮT điều hướng tự động tới `/403`/`/404` (F2 thêm `/401`) khi request GET này nhận lỗi tương ứng
     * (quy tắc CLAUDE.md "Trang lỗi 4xx thống nhất"). Dùng cho lời gọi nền không muốn làm mất trang hiện tại
     * (kiểm tra trạng thái service, polling, kiểm tra tồn tại...).
     */
    skipErrorRedirect?: boolean
  }
}

/** Thân lỗi chuẩn của mọi service AntFarm (hợp đồng §6.0). */
export interface ApiErrorBody {
  error: string
  code?: string
  details?: Record<string, unknown>
}

/** Lỗi đã chuẩn hoá — `message` luôn là tiếng Việt đọc được; `status`/`code`/`details` để màn hình rẽ nhánh. */
export class ApiError extends Error {
  status?: number
  code?: string
  details?: Record<string, unknown>
  /** Thân response thô (nếu có) — cho luồng cần đọc cấu trúc riêng. */
  data?: unknown

  constructor(message: string, init: { status?: number; code?: string; details?: Record<string, unknown>; data?: unknown } = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = init.status
    this.code = init.code
    this.details = init.details
    this.data = init.data
  }
}

export function isApiError(err: unknown): err is ApiError {
  return err instanceof ApiError
}

export interface ApiClientOptions {
  /** Gốc API — ngôn ngữ: `/chinese/api` (cùng origin mọi môi trường); identity: `VITE_IDENTITY_API_URL`. */
  baseURL: string
  /** Gửi cookie (refresh token HttpOnly của identity) — cần cho identity ở production (khác origin, cùng site). */
  withCredentials?: boolean
  /** Thời gian chờ mỗi request (ms). Mặc định 15 giây. */
  timeout?: number
  // F2 bổ sung: getAccessToken, refresh (single-flight), onAuthLost — xem hợp đồng §5.3.1.
}

/**
 * Điều hướng cả trang tới trang lỗi dùng chung. CHỈ áp cho request GET — request GHI (POST/PUT/PATCH/DELETE)
 * giữ nguyên lỗi để màn hình đang thao tác tự báo tại chỗ, không làm mất dữ liệu đang nhập.
 * Bỏ qua khi: `skipErrorRedirect`, method khác GET, hoặc đang đứng đúng trang đích (tránh vòng lặp).
 */
function maybeRedirectToErrorPage(
  config: { method?: string; skipErrorRedirect?: boolean } | undefined,
  targetPath: '/401' | '/403' | '/404',
): void {
  if (typeof window === 'undefined') return
  if (config?.skipErrorRedirect === true) return
  if ((config?.method ?? 'get').toLowerCase() !== 'get') return
  if (window.location.pathname === targetPath) return
  window.location.assign(targetPath)
}

/** Dựng thông điệp tiếng Việt từ lỗi axios: ưu tiên thân lỗi §6.0, rồi tới lỗi mạng/timeout. */
function toApiError(err: AxiosError): ApiError {
  const status = err.response?.status
  const body = err.response?.data as Partial<ApiErrorBody> | string | undefined
  const bodyObj = body && typeof body === 'object' ? body : undefined

  let message: string
  if (bodyObj?.error) message = String(bodyObj.error)
  else if (err.code === 'ECONNABORTED' || err.code === 'ETIMEDOUT') message = 'Máy chủ phản hồi quá lâu, vui lòng thử lại.'
  else if (!err.response) message = 'Không kết nối được máy chủ. Kiểm tra mạng hoặc dịch vụ chưa chạy.'
  else if (status === 502 || status === 503 || status === 504) message = 'Dịch vụ chưa sẵn sàng (gateway không tới được service).'
  else if (status === 404) message = 'Không tìm thấy tài nguyên.'
  else if (status === 403) message = 'Bạn không có quyền thực hiện thao tác này.'
  else if (status === 401) message = 'Cần đăng nhập để tiếp tục.'
  else message = err.message || 'Đã xảy ra lỗi không xác định.'

  return new ApiError(message, {
    status,
    code: bodyObj?.code ? String(bodyObj.code) : undefined,
    details: bodyObj?.details,
    data: err.response?.data,
  })
}

/**
 * Tạo Axios instance dùng chung (F1): baseURL, JSON, chuẩn hoá lỗi thành `ApiError`, điều hướng GET 403/404
 * tới trang lỗi dùng chung (trừ `skipErrorRedirect`). F2 thêm Bearer token + refresh single-flight + 401.
 */
export function createApiClient({ baseURL, withCredentials = false, timeout = 15_000 }: ApiClientOptions): AxiosInstance {
  const client = axios.create({
    baseURL,
    withCredentials,
    timeout,
    headers: { Accept: 'application/json' },
  })

  client.interceptors.request.use((config) => {
    // Body là FormData ⇒ để trình duyệt tự đặt multipart + boundary (axios 1.x sẽ biến FormData thành JSON
    // nếu header JSON được ép — bài học MedDental 30/08/2026). Body khác ⇒ JSON.
    const isFormData = typeof FormData !== 'undefined' && config.data instanceof FormData
    if (!isFormData && config.data !== undefined && !config.headers.has('Content-Type')) {
      config.headers.set('Content-Type', 'application/json')
    }
    return config
  })

  client.interceptors.response.use(
    (res) => res,
    (err: AxiosError) => {
      const status = err.response?.status
      if (status === 403) maybeRedirectToErrorPage(err.config, '/403')
      if (status === 404) maybeRedirectToErrorPage(err.config, '/404')
      return Promise.reject(toApiError(err))
    },
  )

  return client
}
