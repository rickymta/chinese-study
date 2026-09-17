import type { ReactNode } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../AuthProvider'

export interface RequirePermissionProps {
  /** Mã quyền cục bộ của service ngôn ngữ (`study.use`, `content.manage`, `users.manage`...). */
  permission: string
  /** Đích khi thiếu quyền. Mặc định `/403`. */
  forbiddenPath?: string
  children?: ReactNode
}

/** Chặn route theo quyền — đặt BÊN TRONG `RequireAuth` (giả định đã đăng nhập và đã có `me`). */
export function RequirePermission({ permission, forbiddenPath = '/403', children }: RequirePermissionProps) {
  const { hasPermission } = useAuth()
  if (!hasPermission(permission)) return <Navigate to={forbiddenPath} replace />
  return children ?? <Outlet />
}
