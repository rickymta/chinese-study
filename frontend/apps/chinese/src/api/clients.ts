import { createApiClient } from '@af/api'
import { createAuthSession, createIdentityClient } from '@af/auth'

/**
 * Gốc API identity — dev `/identity/api` (cùng origin qua Vite proxy); production `https://id.antfarms.xyz/api`
 * (khác origin, cùng site ⇒ `withCredentials` để cookie refresh `af_rt` đi kèm — R-N10).
 * Giá trị nướng vào bundle lúc build qua `VITE_IDENTITY_API_URL` — sai thì chỉ hỏng khi bấm đăng nhập (RK25).
 */
const IDENTITY_BASE_URL = import.meta.env.VITE_IDENTITY_API_URL ?? '/identity/api'

/**
 * Phiên xác thực dùng chung cho mọi client: access token trong bộ nhớ, làm mới bằng cookie HttpOnly
 * (single-flight + Web Locks giữa các tab), đồng bộ đăng xuất qua BroadcastChannel. Truyền vào `AuthProvider`.
 */
export const authSession = createAuthSession({ identityBaseURL: IDENTITY_BASE_URL })

const authOptions = {
  getAccessToken: authSession.getAccessToken,
  refresh: authSession.refresh,
  onAuthLost: authSession.handleAuthLost,
}

/**
 * API của service tiếng Trung — luôn CÙNG ORIGIN với trang đang mở (R-N9): dev `localhost:3280/chinese/api`
 * qua Vite proxy → gateway; production `chinese.antfarms.xyz/chinese/api` do nginx biên tách sang gateway.
 */
export const chineseApi = createApiClient({ baseURL: '/chinese/api', ...authOptions })

/** API identity (đăng nhập/đăng ký/tài khoản). 401 ở `/auth/*` không kéo theo làm mới (`@af/api` loại trừ theo URL). */
export const identityApi = createApiClient({ baseURL: IDENTITY_BASE_URL, withCredentials: true, ...authOptions })

/** Bộ lời gọi identity theo hợp đồng §6.2 — truyền vào `AuthProvider`. */
export const identity = createIdentityClient(identityApi)
