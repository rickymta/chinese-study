import { useMemo } from 'react'
import { AppLayout } from '@af/ui'
import { useAuth } from '@af/auth'
import { APP_BRAND } from '@/constants'
import { buildNavItems } from './navigation'
import { UserMenu } from './UserMenu'

/**
 * Khung admin — bọc `AppLayout` dùng chung (Drawer md+ / AppBar + bottom nav xs–sm); mục menu lọc theo quyền gộp
 * (đã có tiền tố service) từ `loadMe`. Nhóm bị ẩn được Dashboard giải thích (không để người dùng tưởng app hỏng).
 */
export function AdminLayout() {
  const { hasPermission, permissions } = useAuth()
  const navItems = useMemo(() => buildNavItems(permissions), [permissions])
  return <AppLayout title={APP_BRAND} navItems={navItems} userMenu={<UserMenu />} hasPermission={hasPermission} />
}
