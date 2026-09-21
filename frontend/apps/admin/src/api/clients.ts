import type { AxiosInstance } from 'axios'
import { createApiClient } from '@af/api'
import { createAuthSession, createIdentityClient } from '@af/auth'
import { LANGUAGE_MODULES } from '@/modules/registry'

/**
 * Gốc API identity — dev `/identity/api` (cùng origin qua Vite proxy); production `https://id.antfarms.xyz/api`
 * (khác origin, cùng site ⇒ `withCredentials` để cookie refresh `af_rt` đi kèm). Nướng vào bundle lúc build qua
 * `VITE_IDENTITY_API_URL` — sai thì chỉ hỏng khi bấm đăng nhập.
 */
const IDENTITY_BASE_URL = import.meta.env.VITE_IDENTITY_API_URL ?? '/identity/api'

/**
 * Phiên xác thực dùng chung cho mọi client: access token trong bộ nhớ, làm mới bằng cookie HttpOnly
 * (single-flight + Web Locks giữa các tab), đồng bộ đăng xuất qua BroadcastChannel. Truyền vào `AuthProvider`.
 * MỘT token dùng cho mọi audience (`af-cms`, `af-chinese`... — R-W6) nên admin chỉ cần một phiên.
 */
export const authSession = createAuthSession({ identityBaseURL: IDENTITY_BASE_URL })

const authOptions = {
  getAccessToken: authSession.getAccessToken,
  refresh: authSession.refresh,
  onAuthLost: authSession.handleAuthLost,
}

/**
 * API của cms-backend — luôn CÙNG ORIGIN với trang đang mở: dev `localhost:3290/cms/api` qua Vite proxy → gateway;
 * production `admin.antfarms.xyz/cms/api` do nginx biên tách sang gateway (R-W7).
 */
export const cmsApi = createApiClient({ baseURL: '/cms/api', ...authOptions })

/**
 * API quản trị của từng service ngôn ngữ (R-W5: admin gọi thẳng `/<code>/api/admin/*`, quyền kiểm cục bộ ở service
 * đó). Khoá = mã ngôn ngữ trong `LANGUAGE_MODULES`; W2 chỉ có `chinese`.
 */
export const languageApis: Record<string, AxiosInstance> = Object.fromEntries(
  LANGUAGE_MODULES.map((m) => [m.code, createApiClient({ baseURL: m.apiBase, ...authOptions })]),
)

/** API identity (đăng nhập/tài khoản). 401 ở `/auth/*` không kéo theo làm mới (`@af/api` loại trừ theo URL). */
export const identityApi = createApiClient({ baseURL: IDENTITY_BASE_URL, withCredentials: true, ...authOptions })

/** Bộ lời gọi identity theo hợp đồng §6.2 — truyền vào `AuthProvider`. */
export const identity = createIdentityClient(identityApi)
