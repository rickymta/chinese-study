import { identity } from '@/api/clients'
import type { Account, ChangePasswordRequest, ChangePasswordResponse, UpdateAccountRequest } from '@af/auth'

// F4 — hồ sơ gọi identity-service (hợp đồng §6.2) qua `createIdentityClient` của `@af/auth` (Bearer audience
// `af-identity`; `/auth/password` kèm cookie `af_rt` để giữ phiên hiện tại).

/** `PUT /api/account` → 200 account · 400 VALIDATION · 422 INVALID_TIME_ZONE. */
export function updateAccount(body: UpdateAccountRequest): Promise<Account> {
  return identity.updateAccount(body)
}

/** `POST /api/auth/password` → 200 `{ otherSessionsRevoked, currentSessionKept }` · 422 WRONG_PASSWORD | PASSWORD_UNCHANGED. */
export function changePassword(body: ChangePasswordRequest): Promise<ChangePasswordResponse> {
  return identity.changePassword(body)
}
