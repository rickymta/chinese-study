import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios'

/**
 * Mở rộng config axios: mỗi request có thể tắt điều hướng tự động khi lỗi (xem `maybeRedirectToErrorPage`).
 * Module augmentation ở đây ⇒ mọi app trong monorepo (import thẳng TS source của `@af/api`) đều thấy trường này.
 */
declare module 'axios' {
  interface AxiosRequestConfig {
    /**
     * `true` ⇒ TẮT điều hướng tự động tới `/403`/`/404` khi request GET này nhận lỗi tương ứng
     * (quy tắc CLAUDE.md "Trang lỗi 4xx thống nhất"). Dùng cho lời gọi nền không muốn làm mất trang hiện tại
     * (kiểm tra trạng thái service, polling, kiểm tra tồn tại...).
     */
    skipErrorRedirect?: boolean
    /**
     * `true` ⇒ KHÔNG làm mới token rồi gửi lại khi nhận 401 (F2). Tự động bật cho lần gửi lại và cho
     * `/auth/login|register|refresh|logout` (401 ở đó là câu trả lời thật, không phải token hết hạn).
     */
    skipAuthRefresh?: boolean
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
  /** Access token hiện có trong bộ nhớ (F2, `@af/auth`). Có ⇒ gắn `Authorization: Bearer` cho mọi request. */
  getAccessToken?: () => string | null
  /**
   * Làm mới phiên khi nhận 401: trả access token MỚI; ném lỗi nếu mất phiên. `@af/auth` cung cấp hàm này
   * (single-flight + Web Locks giữa các tab). Client gọi lại request đúng MỘT lần với token mới.
   */
  refresh?: () => Promise<string>
  /** Gọi khi làm mới thất bại (hoặc gửi lại vẫn 401) — `@af/auth` chuyển sang ẩn danh ⇒ `RequireAuth` đưa về `/dang-nhap?returnTo=...&reason=expired`. */
  onAuthLost?: () => void
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
  else if (status === 429) message = 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.'
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
 * Bốn endpoint phiên của identity — 401 ở đây là câu trả lời thật (sai mật khẩu, cookie hỏng), không kéo theo làm mới.
 * Các endpoint khác dưới `/auth/*` (vd `/auth/password` cần Bearer) vẫn được làm mới + gửi lại như bình thường.
 */
function isAuthUrl(url: string | undefined): boolean {
  return !!url && /(^|\/)auth\/(login|register|refresh|logout)(\?|$)/.test(url)
}

/**
 * Tạo Axios instance dùng chung: baseURL, JSON, Bearer token, làm mới phiên khi 401 (single-flight, gửi lại một
 * lần), chuẩn hoá lỗi thành `ApiError`, điều hướng GET 403/404 tới trang lỗi dùng chung (trừ `skipErrorRedirect`).
 *
 * THỨ TỰ interceptor response quan trọng (bài học review F1): axios chạy `onRejected` theo thứ tự đăng ký, nên
 * interceptor làm mới token phải đăng ký TRƯỚC interceptor chuyển `AxiosError` → `ApiError` — sau bước chuyển
 * đổi không còn `err.config` để gửi lại request.
 */
export function createApiClient({
  baseURL,
  withCredentials = false,
  timeout = 15_000,
  getAccessToken,
  refresh,
  onAuthLost,
}: ApiClientOptions): AxiosInstance {
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
    // Gắn Bearer nếu có token trong bộ nhớ và request chưa tự đặt Authorization.
    const token = getAccessToken?.()
    if (token && !config.headers.has('Authorization')) {
      config.headers.set('Authorization', `Bearer ${token}`)
    }
    return config
  })

  // Single-flight trong phạm vi client: nhiều request cùng dính 401 ⇒ chỉ một lần gọi `refresh()`.
  let refreshing: Promise<string> | null = null
  const refreshOnce = (): Promise<string> => {
    if (!refreshing) {
      refreshing = refresh!().finally(() => {
        refreshing = null
      })
    }
    return refreshing
  }

  // (1) Interceptor làm mới token — nhận AxiosError THÔ (còn `config` để gửi lại).
  client.interceptors.response.use(
    (res) => res,
    async (err: AxiosError) => {
      const config = err.config as InternalAxiosRequestConfig | undefined
      if (!refresh || err.response?.status !== 401 || !config) return Promise.reject(err)
      if (config.skipAuthRefresh || isAuthUrl(config.url)) return Promise.reject(err)

      let token: string
      try {
        token = await refreshOnce()
      } catch (refreshErr) {
        // Chỉ coi là MẤT PHIÊN khi identity từ chối refresh (401 REFRESH_INVALID / 403 ACCOUNT_DISABLED).
        // Lỗi mạng/5xx lúc refresh: không đá người dùng ra — token cũ có thể vẫn còn hạn, lần 401 sau sẽ thử lại.
        if (isApiError(refreshErr) && (refreshErr.status === 401 || refreshErr.status === 403)) onAuthLost?.()
        return Promise.reject(err) // trả lỗi 401 gốc của request ban đầu, không phải lỗi của refresh
      }

      // Gửi lại ĐÚNG MỘT lần với token mới; lần này vẫn 401 ⇒ coi như mất phiên.
      config.skipAuthRefresh = true
      config.headers.set('Authorization', `Bearer ${token}`)
      try {
        return await client.request(config)
      } catch (retryErr) {
        if (isApiError(retryErr) && retryErr.status === 401) onAuthLost?.()
        throw retryErr
      }
    },
  )

  // (2) Interceptor chuẩn hoá lỗi + điều hướng trang lỗi. Lỗi từ lần gửi lại đã là ApiError ⇒ cho qua nguyên vẹn.
  client.interceptors.response.use(
    (res) => res,
    (err: AxiosError | ApiError) => {
      if (isApiError(err)) return Promise.reject(err)
      const status = err.response?.status
      // 401 (F3): tới đây nghĩa là KHÔNG làm mới được (không có `refresh`, hoặc làm mới hỏng, hoặc `skipAuthRefresh`).
      // - Client có `onAuthLost` (app có `@af/auth`): mất phiên đã được báo ở (1) ⇒ `RequireAuth` đưa về
      //   `/dang-nhap?returnTo=...&reason=expired` (giữ được trang đang xem) — KHÔNG nhảy `/401` để khỏi mất returnTo.
      // - Client không có quản lý phiên: GET ⇒ `/401` (quy tắc "Trang lỗi 4xx thống nhất"). `/auth/*` là câu trả
      //   lời thật của identity (sai mật khẩu...) nên không điều hướng.
      if (status === 401 && !onAuthLost && !isAuthUrl(err.config?.url)) maybeRedirectToErrorPage(err.config, '/401')
      if (status === 403) maybeRedirectToErrorPage(err.config, '/403')
      if (status === 404) maybeRedirectToErrorPage(err.config, '/404')
      return Promise.reject(toApiError(err))
    },
  )

  return client
}
