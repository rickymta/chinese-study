import { createApiClient, isApiError } from '@af/api'
import type { Account, RefreshResponse } from './types'

/** Tên khoá Web Locks + kênh BroadcastChannel — dùng chung mọi app cùng origin (hợp đồng §5.3.1). */
const LOCK_NAME = 'af-auth-refresh'
const CHANNEL_NAME = 'af-auth'

export interface AuthSessionOptions {
  /** Gốc API identity — `VITE_IDENTITY_API_URL` (dev `/identity/api`, production `https://id.antfarms.xyz/api`). */
  identityBaseURL: string
  /** Gửi cookie `af_rt` — luôn `true` với identity (khác origin ở production, cùng site). Mặc định `true`. */
  withCredentials?: boolean
  /** Làm mới chủ động trước hạn bao nhiêu giây. Mặc định 60 (hợp đồng §5.3.1). */
  refreshLeadSeconds?: number
}

export type AuthSessionEvent =
  /** Token đổi (làm mới/đăng nhập) hoặc bị xoá (đăng xuất). */
  | { type: 'token'; accessToken: string | null }
  /** Mất phiên: refresh bị từ chối (401/403) hoặc gửi lại vẫn 401 ⇒ AuthProvider chuyển sang ẩn danh, lý do `expired`. */
  | { type: 'lost' }
  /** Tab khác vừa đăng nhập — tab này nếu đang ẩn danh thì nạp lại phiên từ cookie. */
  | { type: 'remote-login' }
  /** Tab khác vừa đăng xuất — tab này xoá token và về ẩn danh. */
  | { type: 'remote-logout' }

/** Phiên xác thực cấp module: nguồn sự thật của access token cho MỌI axios client trong app. */
export interface AuthSession {
  /** Access token đang giữ trong bộ nhớ (không bao giờ ghi localStorage/sessionStorage). */
  getAccessToken(): string | null
  /** Thời điểm hết hạn (epoch ms) hoặc `null`. */
  getExpiresAt(): number | null
  /** Ghi token sau đăng nhập/đăng ký/làm mới; tự đặt hẹn giờ làm mới chủ động. */
  setTokens(tokens: RefreshResponse): void
  /** Xoá token + huỷ hẹn giờ (đăng xuất, mất phiên). Không phát sự kiện `lost`. */
  clear(): void
  /**
   * Làm mới bằng cookie `af_rt`: single-flight trong tab + Web Locks giữa các tab (RK7: hai tab cùng xoay refresh).
   * Trả access token mới. 401/403 ⇒ xoá token, phát `lost`, ném lỗi. Lỗi mạng ⇒ giữ token cũ, ném lỗi.
   */
  refresh(): Promise<string>
  /** Gọi từ `@af/api` (`onAuthLost`): xoá token + phát `lost`. */
  handleAuthLost(): void
  /** Báo tab khác biết đăng nhập/đăng xuất vừa xảy ra ở tab này. */
  broadcast(type: 'login' | 'logout'): void
  subscribe(listener: (event: AuthSessionEvent) => void): () => void
  /** Dọn tài nguyên (đóng kênh, huỷ hẹn giờ) — dùng khi unmount AuthProvider hay trong test. */
  dispose(): void
}

interface BroadcastMessage {
  type: 'login' | 'logout'
}

/** Giải mã payload JWT (base64url) — KHÔNG kiểm chữ ký (chỉ để hiển thị; quyền lấy từ service ngôn ngữ). */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    if (!part) return null
    const base64 = part.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4)
    const json = decodeURIComponent(
      Array.from(atob(padded), (c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(''),
    )
    const obj: unknown = JSON.parse(json)
    return obj && typeof obj === 'object' ? (obj as Record<string, unknown>) : null
  } catch {
    return null
  }
}

/** Dựng `Account` tối thiểu từ claim (`sub`, `email`, `name`, `zoneinfo`) sau khi làm mới — không cần gọi `GET /api/account`. */
export function accountFromToken(token: string): Account | null {
  const claims = decodeJwtPayload(token)
  if (!claims || typeof claims.sub !== 'string') return null
  return {
    id: claims.sub,
    email: typeof claims.email === 'string' ? claims.email : '',
    displayName: typeof claims.name === 'string' ? claims.name : '',
    timeZone: typeof claims.zoneinfo === 'string' ? claims.zoneinfo : 'Asia/Ho_Chi_Minh',
  }
}

export function createAuthSession({
  identityBaseURL,
  withCredentials = true,
  refreshLeadSeconds = 60,
}: AuthSessionOptions): AuthSession {
  // Client RIÊNG cho lời gọi refresh: không có getAccessToken/refresh/onAuthLost ⇒ không đệ quy, không cần token.
  const refreshApi = createApiClient({ baseURL: identityBaseURL, withCredentials })

  let accessToken: string | null = null
  let expiresAt: number | null = null
  let inflight: Promise<string> | null = null
  let timer: ReturnType<typeof setTimeout> | null = null
  const listeners = new Set<(event: AuthSessionEvent) => void>()

  const emit = (event: AuthSessionEvent) => {
    for (const l of listeners) l(event)
  }

  const clearTimer = () => {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  // Hẹn làm mới chủ động `refreshLeadSeconds` trước hạn (tối thiểu 5 giây để không xoay liên tục khi đồng hồ lệch).
  const scheduleRefresh = () => {
    clearTimer()
    if (expiresAt === null) return
    const delay = Math.max(expiresAt - Date.now() - refreshLeadSeconds * 1000, 5_000)
    timer = setTimeout(() => {
      // Lỗi mạng ở đây được nuốt: token cũ vẫn dùng được tới hạn, request 401 kế tiếp sẽ thử làm mới lần nữa.
      void refresh().catch(() => {})
    }, delay)
  }

  const setTokens = (tokens: RefreshResponse) => {
    accessToken = tokens.accessToken
    const parsed = Date.parse(tokens.accessTokenExpiresAt)
    // Backend không trả hạn hợp lệ ⇒ giả định 15 phút (R-A3) để vẫn có lịch làm mới.
    expiresAt = Number.isFinite(parsed) ? parsed : Date.now() + 15 * 60_000
    scheduleRefresh()
    emit({ type: 'token', accessToken })
  }

  const clear = () => {
    clearTimer()
    const had = accessToken !== null
    accessToken = null
    expiresAt = null
    if (had) emit({ type: 'token', accessToken: null })
  }

  const doRefresh = async (): Promise<string> => {
    try {
      const res = await refreshApi.post<RefreshResponse>('/auth/refresh', {})
      // RK25: `VITE_IDENTITY_API_URL` sai ⇒ nhận index.html 200 thay vì JSON — coi như không có phiên, không được
      // ghi token rác rồi nổ ở chỗ giải mã JWT.
      if (typeof res.data?.accessToken !== 'string' || !res.data.accessToken) {
        throw new Error('Phản hồi làm mới phiên không hợp lệ (kiểm tra VITE_IDENTITY_API_URL).')
      }
      setTokens(res.data)
      return res.data.accessToken
    } catch (err) {
      // 401 REFRESH_INVALID / 403 ACCOUNT_DISABLED: phiên thật sự mất. Lỗi khác (mạng, 5xx): giữ nguyên token.
      // Chỉ phát `lost` khi TRƯỚC ĐÓ đã có token (phiên đang dùng bị mất) — lần mở app đầu không có cookie thì
      // chỉ là "chưa đăng nhập", không được hiện "phiên hết hạn".
      if (isApiError(err) && (err.status === 401 || err.status === 403)) {
        const hadSession = accessToken !== null
        clear()
        if (hadSession) emit({ type: 'lost' })
      }
      throw err
    }
  }

  const refresh = (): Promise<string> => {
    if (inflight) return inflight
    // Web Locks: các tab cùng origin xếp hàng xoay refresh — tab sau gửi cookie ĐÃ xoay của tab trước, không dính
    // "dùng lại token" (R-A6). Trình duyệt không có Web Locks ⇒ dựa vào cửa sổ ân hạn 30 giây của backend.
    const run =
      typeof navigator !== 'undefined' && navigator.locks
        ? navigator.locks.request(LOCK_NAME, doRefresh)
        : doRefresh()
    inflight = run.finally(() => {
      inflight = null
    })
    return inflight
  }

  const handleAuthLost = () => {
    clear()
    emit({ type: 'lost' })
  }

  // ── Đồng bộ giữa tab (BroadcastChannel không gửi lại cho chính tab phát) ──
  const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel(CHANNEL_NAME) : null
  const onMessage = (ev: MessageEvent<BroadcastMessage>) => {
    if (ev.data?.type === 'logout') {
      clear()
      emit({ type: 'remote-logout' })
    } else if (ev.data?.type === 'login') {
      emit({ type: 'remote-login' })
    }
  }
  channel?.addEventListener('message', onMessage)

  const broadcast = (type: 'login' | 'logout') => {
    try {
      channel?.postMessage({ type } satisfies BroadcastMessage)
    } catch {
      /* kênh đã đóng — bỏ qua */
    }
  }

  // Tab nền bị trình duyệt hãm setTimeout ⇒ khi quay lại mà token sắp/đã hết hạn thì làm mới ngay.
  const onVisible = () => {
    if (document.visibilityState !== 'visible' || accessToken === null || expiresAt === null) return
    if (expiresAt - Date.now() <= refreshLeadSeconds * 1000) void refresh().catch(() => {})
  }
  if (typeof document !== 'undefined') document.addEventListener('visibilitychange', onVisible)

  return {
    getAccessToken: () => accessToken,
    getExpiresAt: () => expiresAt,
    setTokens,
    clear,
    refresh,
    handleAuthLost,
    broadcast,
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    dispose() {
      clearTimer()
      listeners.clear()
      channel?.removeEventListener('message', onMessage)
      channel?.close()
      if (typeof document !== 'undefined') document.removeEventListener('visibilitychange', onVisible)
    },
  }
}
