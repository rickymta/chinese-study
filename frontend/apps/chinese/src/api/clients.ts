import { createApiClient } from '@af/api'

/**
 * API của service tiếng Trung — luôn CÙNG ORIGIN với trang đang mở (R-N9): dev `localhost:3280/chinese/api`
 * qua Vite proxy → gateway; production `chinese.antfarms.xyz/chinese/api` do nginx biên tách sang gateway.
 */
export const chineseApi = createApiClient({ baseURL: '/chinese/api' })

/**
 * API identity — dev `/identity/api` (cùng origin qua Vite proxy); production `https://id.antfarms.xyz/api`
 * (khác origin, cùng site ⇒ `withCredentials` để cookie refresh `af_rt` đi kèm — R-N10).
 * Giá trị nướng vào bundle lúc build qua `VITE_IDENTITY_API_URL` — sai thì chỉ hỏng khi bấm đăng nhập.
 */
export const identityApi = createApiClient({
  baseURL: import.meta.env.VITE_IDENTITY_API_URL ?? '/identity/api',
  withCredentials: true,
})
