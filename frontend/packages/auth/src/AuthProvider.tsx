import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import type { IdentityClient } from './identityClient'
import { accountFromToken, type AuthSession } from './session'
import type { Account, AuthLostReason, AuthStatus, LoginRequest, MeInfo, RegisterRequest } from './types'

export interface AuthProviderProps {
  /** Phiên xác thực (token trong bộ nhớ) — cùng đối tượng đã truyền cho `createApiClient` của app. */
  session: AuthSession
  /** `createIdentityClient(identityApi)`. */
  identity: IdentityClient
  /**
   * Hồ sơ + quyền do APP truyền — gọi `GET /<ngôn-ngữ>/api/me` của service ngôn ngữ (phân quyền cục bộ, không
   * suy từ JWT). Được gọi sau mỗi lần có phiên (mount, đăng nhập, đăng ký) và khi `reloadMe()`.
   */
  loadMe: () => Promise<MeInfo>
  children: ReactNode
}

export interface AuthContextValue {
  status: AuthStatus
  account: Account | null
  /** Kết quả `loadMe` (null khi chưa có/lỗi). */
  me: MeInfo | null
  /** Đang gọi `loadMe`. */
  meLoading: boolean
  /** Lỗi lần `loadMe` gần nhất (null khi thành công). */
  meError: unknown
  permissions: ReadonlySet<string>
  hasPermission: (permission: string) => boolean
  /** Bí danh của `hasPermission` — dùng `can('content.manage')` cho gọn trong JSX. */
  can: (permission: string) => boolean
  /** Lý do gần nhất phiên chuyển sang ẩn danh (`expired` ⇒ trang đăng nhập hiện thông báo). */
  lostReason: AuthLostReason
  login: (body: LoginRequest) => Promise<void>
  register: (body: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
  reloadMe: () => Promise<void>
  /** Lấy lại `account` đầy đủ từ `GET /api/account` (F4: sau khi sửa hồ sơ). */
  reloadAccount: () => Promise<void>
  /**
   * F4 (R4-4): làm mới phiên NGAY — xoay refresh token ⇒ access token mới mang `name`/`zoneinfo` vừa sửa ⇒
   * `GET /api/account` cập nhật `account` ⇒ `loadMe()` (service ngôn ngữ thấy claim khác bản ghi thì đồng bộ
   * `access.users` trong chính request đó, không chờ cache 5 phút). Gọi sau khi `PUT /api/account` thành công.
   */
  refreshSession: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

/**
 * Nguồn sự thật về phiên đăng nhập cho cả app (hợp đồng §5.3.1). Đặt GIỮA QueryClientProvider và RouterProvider
 * — không dùng hook của router ở đây; điều hướng khi mất phiên do `RequireAuth` (trong router) đảm nhiệm.
 *
 * Khi mount: `session.refresh()` (cookie `af_rt`) ⇒ dựng `account` từ claim JWT ⇒ `loadMe()`. Không có cookie ⇒
 * ẩn danh. Token chỉ nằm trong bộ nhớ của `session` — không localStorage.
 */
export function AuthProvider({ session, identity, loadMe, children }: AuthProviderProps) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [account, setAccount] = useState<Account | null>(null)
  const [me, setMe] = useState<MeInfo | null>(null)
  const [meLoading, setMeLoading] = useState(false)
  const [meError, setMeError] = useState<unknown>(null)
  const [lostReason, setLostReason] = useState<AuthLostReason>(null)

  // Ref để callback trong subscribe/bootstrap luôn thấy giá trị mới nhất mà không phải đăng ký lại.
  const statusRef = useRef(status)
  statusRef.current = status
  const loadMeRef = useRef(loadMe)
  loadMeRef.current = loadMe

  const becomeAnonymous = useCallback(
    (reason: AuthLostReason) => {
      session.clear()
      setAccount(null)
      setMe(null)
      setMeError(null)
      setMeLoading(false)
      setLostReason(reason)
      setStatus('anonymous')
    },
    [session],
  )

  const runLoadMe = useCallback(async () => {
    setMeLoading(true)
    try {
      const info = await loadMeRef.current()
      setMe(info)
      setMeError(null)
    } catch (err) {
      setMe(null)
      setMeError(err)
    } finally {
      setMeLoading(false)
    }
  }, [])

  /** Nạp phiên từ cookie (mount, hoặc khi tab khác vừa đăng nhập). */
  const bootstrap = useCallback(async () => {
    setStatus('loading')
    let token: string
    try {
      token = await session.refresh()
    } catch {
      // Không có cookie / cookie hỏng / identity chưa chạy ⇒ ẩn danh. Lý do giữ nguyên (đã đặt bởi sự kiện `lost` nếu có).
      setAccount(null)
      setMe(null)
      setStatus('anonymous')
      return
    }
    setAccount(accountFromToken(token))
    await runLoadMe()
    setStatus('authenticated')
  }, [session, runLoadMe])

  useEffect(() => {
    void bootstrap()
    const unsubscribe = session.subscribe((event) => {
      switch (event.type) {
        case 'lost':
          if (statusRef.current !== 'anonymous') becomeAnonymous('expired')
          break
        case 'remote-logout':
          if (statusRef.current !== 'anonymous') becomeAnonymous(null)
          break
        case 'remote-login':
          if (statusRef.current === 'anonymous') void bootstrap()
          break
        default:
          break
      }
    })
    return unsubscribe
    // StrictMode mount 2 lần ⇒ 2 lần bootstrap, nhưng `session.refresh()` single-flight nên chỉ một lời gọi mạng.
  }, [session, bootstrap, becomeAnonymous])

  const login = useCallback(
    async (body: LoginRequest) => {
      const res = await identity.login(body)
      session.setTokens(res)
      setAccount(res.account)
      setLostReason(null)
      await runLoadMe()
      setStatus('authenticated')
      session.broadcast('login')
    },
    [identity, session, runLoadMe],
  )

  const register = useCallback(
    async (body: RegisterRequest) => {
      const res = await identity.register(body)
      session.setTokens(res)
      setAccount(res.account)
      setLostReason(null)
      await runLoadMe()
      setStatus('authenticated')
      session.broadcast('login')
    },
    [identity, session, runLoadMe],
  )

  const logout = useCallback(async () => {
    try {
      await identity.logout() // thu hồi refresh token + xoá cookie; lỗi mạng cũng vẫn đăng xuất phía client
    } catch {
      /* bỏ qua */
    }
    becomeAnonymous(null)
    session.broadcast('logout')
  }, [identity, session, becomeAnonymous])

  const reloadAccount = useCallback(async () => {
    setAccount(await identity.getAccount())
  }, [identity])

  const refreshSession = useCallback(async () => {
    // `session.refresh()` 401/403 ⇒ đã phát `lost` ⇒ subscribe ở trên chuyển sang ẩn danh; ném lỗi để bên gọi biết.
    const token = await session.refresh()
    try {
      setAccount(await identity.getAccount())
    } catch {
      // Không lấy được hồ sơ đầy đủ (mạng) ⇒ vẫn có claim mới trong token để hiển thị tên/múi giờ mới.
      setAccount(accountFromToken(token))
    }
    await runLoadMe()
  }, [session, identity, runLoadMe])

  const permissions = useMemo(() => new Set(me?.permissions ?? []), [me])
  const hasPermission = useCallback((p: string) => permissions.has(p), [permissions])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      account,
      me,
      meLoading,
      meError,
      permissions,
      hasPermission,
      can: hasPermission,
      lostReason,
      login,
      register,
      logout,
      reloadMe: runLoadMe,
      reloadAccount,
      refreshSession,
    }),
    [status, account, me, meLoading, meError, permissions, hasPermission, lostReason, login, register, logout, runLoadMe, reloadAccount, refreshSession],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth phải được dùng bên trong <AuthProvider> của @af/auth')
  return ctx
}
