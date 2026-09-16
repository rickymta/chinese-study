// @af/auth — xác thực dùng chung cho MỌI app ngôn ngữ của AntFarm (hợp đồng §5.3.1).
// Gọi identity-service (đăng nhập/đăng ký/làm mới/đăng xuất); QUYỀN do service ngôn ngữ trả qua `loadMe` của app.
// Access token chỉ nằm trong bộ nhớ (`createAuthSession`), refresh token là cookie HttpOnly `af_rt` do identity đặt.

export { createAuthSession, accountFromToken, decodeJwtPayload } from './session'
export type { AuthSession, AuthSessionOptions, AuthSessionEvent } from './session'

export { createIdentityClient } from './identityClient'
export type { IdentityClient } from './identityClient'

export { AuthProvider, useAuth } from './AuthProvider'
export type { AuthProviderProps, AuthContextValue } from './AuthProvider'

export { RequireAuth } from './components/RequireAuth'
export type { RequireAuthProps } from './components/RequireAuth'
export { RequirePermission } from './components/RequirePermission'
export type { RequirePermissionProps } from './components/RequirePermission'
export { FullScreenLoading } from './components/FullScreenLoading'
export { PasswordField } from './components/PasswordField'
export type { PasswordFieldProps } from './components/PasswordField'
export { bindField } from './components/formUtils'

export { LoginPage } from './pages/LoginPage'
export type { LoginPageProps } from './pages/LoginPage'
export { RegisterPage } from './pages/RegisterPage'
export type { RegisterPageProps } from './pages/RegisterPage'
export { AuthShell } from './pages/AuthShell'
export type { AuthShellProps } from './pages/AuthShell'

export { describeAuthError } from './authErrors'
export type { AuthErrorView } from './authErrors'
export { sanitizeReturnTo, buildLoginUrl } from './returnTo'

export type * from './types'
