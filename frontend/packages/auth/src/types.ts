// Kiểu dữ liệu theo hợp đồng API identity-service §6.2 (JSON camelCase, thời điểm ISO-8601 UTC `Z`).

/** `account` trong phản hồi register/login và `GET /api/account`. */
export interface Account {
  id: string
  email: string
  displayName: string
  /** Múi giờ IANA, vd `Asia/Ho_Chi_Minh`. */
  timeZone: string
  /** Chỉ có khi lấy từ API (register/login/`GET /api/account`); tài khoản dựng từ claim JWT sau khi làm mới thì không có. */
  createdAt?: string
}

/** `POST /api/auth/register` · `POST /api/auth/login` → 200/201. */
export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  account: Account
}

/** `POST /api/auth/refresh` → 200 (cookie `af_rt` được xoay vòng kèm theo). */
export interface RefreshResponse {
  accessToken: string
  accessTokenExpiresAt: string
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
  timeZone: string
}

export interface LoginRequest {
  email: string
  password: string
}

/** `PUT /api/account`. */
export interface UpdateAccountRequest {
  displayName: string
  timeZone: string
}

/**
 * `POST /api/auth/password` (Bearer + cookie `af_rt`) → 200 `ChangePasswordResponse` · 422 `WRONG_PASSWORD` |
 * `PASSWORD_UNCHANGED`. Đặt dưới `/auth/*` để cookie `af_rt` (Path `/api/auth`) đi kèm — server giữ họ refresh token
 * hiện tại, thu hồi họ khác (R-A12). Chốt 17/09/2026.
 */
export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

/** Phản hồi 200 của `POST /api/auth/password`. */
export interface ChangePasswordResponse {
  /** Số phiên (họ refresh token) KHÁC đã bị thu hồi. */
  otherSessionsRevoked: number
  /** `true` khi phiên hiện tại (cookie `af_rt` gửi kèm) được giữ; `false` nếu không có cookie hợp lệ ⇒ mọi phiên bị thu hồi. */
  currentSessionKept: boolean
}

/**
 * Hồ sơ + quyền do SERVICE NGÔN NGỮ trả (vd `GET /chinese/api/me`, §6.3). `@af/auth` chỉ cần `permissions`;
 * phần còn lại app tự đọc qua `useAuth().me`.
 */
export interface MeInfo {
  permissions: string[]
  [key: string]: unknown
}

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

/** Lý do phiên chuyển sang ẩn danh — đưa lên URL `?reason=expired` để trang đăng nhập giải thích. */
export type AuthLostReason = 'expired' | null
