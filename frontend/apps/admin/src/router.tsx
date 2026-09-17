import { createBrowserRouter, Navigate, type RouteObject } from 'react-router-dom'
import { RequireAuth, RequirePermission, type MeInfo } from '@af/auth'
import { NotFoundPage } from '@af/ui'
import { AdminLayout } from './layout/AdminLayout'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { ForbiddenPage } from './pages/ForbiddenPage'
import { UnauthorizedPage } from './pages/UnauthorizedPage'
import { CmsUsersPage } from './features/cms-users/pages/CmsUsersPage'
import { CMS_PERMS } from './auth/permissions'
import { hasUnavailableService } from './auth/mergeMe'
import { LANGUAGE_MODULES } from './modules/registry'
import type { AdminMeInfo } from './auth/types'

// Route slug tiếng Việt không dấu. KHÔNG đặt route bắt đầu bằng /identity, /cms, /chinese (Vite proxy nuốt — xem
// vite.config.ts). Module ngôn ngữ nằm dưới /ngon-ngu/<code>/ (W13 thêm màn; W2 registry không có route nào).
const languageRoutes: RouteObject[] = LANGUAGE_MODULES.filter((m) => m.routes.length > 0).map((m) => ({
  path: `ngon-ngu/${m.code}`,
  children: m.routes,
}))

/**
 * R-W2 + hợp đồng W2 §5.3.1: 0 quyền VÀ mọi service đều trả lời ⇒ `/403`; có service không phản hồi ⇒ vẫn cho vào
 * Dashboard (báo "X: không phản hồi") — không đẩy 403 oan khi service chết.
 */
const allowNoPermissions = (me: MeInfo) => hasUnavailableService(me as AdminMeInfo)

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RequireAuth allowNoPermissions={allowNoPermissions} />,
    children: [
      {
        element: <AdminLayout />,
        children: [
          // Tổng quan: mọi người đăng nhập có ≥1 quyền (hoặc có service chết) — tự giải thích quyền còn thiếu.
          { index: true, element: <DashboardPage /> },
          {
            path: 'nguoi-dung-cms',
            element: (
              <RequirePermission permission={CMS_PERMS.USERS_MANAGE}>
                <CmsUsersPage />
              </RequirePermission>
            ),
          },
          ...languageRoutes,
        ],
      },
    ],
  },
  { path: '/dang-nhap', element: <LoginPage /> },
  { path: '/401', element: <UnauthorizedPage /> },
  { path: '/403', element: <ForbiddenPage /> },
  { path: '/404', element: <NotFoundPage /> },
  { path: '*', element: <Navigate to="/404" replace /> },
])
