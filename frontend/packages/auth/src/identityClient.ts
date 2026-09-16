import type { AxiosInstance } from 'axios'
import type {
  Account,
  AuthResponse,
  ChangePasswordRequest,
  ChangePasswordResponse,
  LoginRequest,
  RefreshResponse,
  RegisterRequest,
  UpdateAccountRequest,
} from './types'

/** Bộ lời gọi identity-service (hợp đồng §6.2). Mọi app ngôn ngữ dùng chung, chỉ khác `identityApi` truyền vào. */
export interface IdentityClient {
  register(body: RegisterRequest): Promise<AuthResponse>
  login(body: LoginRequest): Promise<AuthResponse>
  refresh(): Promise<RefreshResponse>
  logout(): Promise<void>
  getAccount(): Promise<Account>
  updateAccount(body: UpdateAccountRequest): Promise<Account>
  changePassword(body: ChangePasswordRequest): Promise<ChangePasswordResponse>
}

/**
 * Bọc `identityApi` (axios của `@af/api`, `baseURL` = `VITE_IDENTITY_API_URL`, `withCredentials: true` để cookie
 * `af_rt` đi kèm). Bốn lời gọi phiên (`/auth/login|register|refresh|logout`) KHÔNG đi qua bước làm mới token khi 401
 * (`@af/api` tự loại trừ theo URL); `/auth/password` và `/account*` cần Bearer nên vẫn được làm mới + gửi lại.
 */
export function createIdentityClient(identityApi: AxiosInstance): IdentityClient {
  return {
    async register(body) {
      const res = await identityApi.post<AuthResponse>('/auth/register', body)
      return res.data
    },
    async login(body) {
      const res = await identityApi.post<AuthResponse>('/auth/login', body)
      return res.data
    },
    async refresh() {
      // Body rỗng `{}` để axios vẫn gửi Content-Type JSON — một số proxy từ chối POST không có thân.
      const res = await identityApi.post<RefreshResponse>('/auth/refresh', {})
      return res.data
    },
    async logout() {
      await identityApi.post('/auth/logout', {})
    },
    async getAccount() {
      const res = await identityApi.get<Account>('/account')
      return res.data
    },
    async updateAccount(body) {
      const res = await identityApi.put<Account>('/account', body)
      return res.data
    },
    async changePassword(body) {
      // Nằm dưới /auth/* (không phải /account/*) vì cookie `af_rt` chỉ đi kèm tới `/api/auth/*`: server cần cookie để
      // giữ họ refresh token HIỆN TẠI và thu hồi các họ khác (R-A12). `identityApi` đã bật withCredentials.
      // 200 { otherSessionsRevoked, currentSessionKept } · 422 WRONG_PASSWORD | PASSWORD_UNCHANGED.
      const res = await identityApi.post<ChangePasswordResponse>('/auth/password', body)
      return res.data
    },
  }
}
