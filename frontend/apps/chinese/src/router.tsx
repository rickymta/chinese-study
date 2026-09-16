import { createBrowserRouter, Navigate } from 'react-router-dom'
import { RequireAuth, RequirePermission } from '@af/auth'
import { NotFoundPage } from '@af/ui'
import { AppShell } from './layout/AppShell'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './features/auth/pages/LoginPage'
import { RegisterPage } from './features/auth/pages/RegisterPage'
import { PERMISSIONS } from './features/auth/permissions'
import { AdminHomePage } from './features/admin/pages/AdminHomePage'
import { UnauthorizedPage } from './pages/errors/UnauthorizedPage'
import { ForbiddenPage } from './pages/errors/ForbiddenPage'

// Route slug tiếng Việt không dấu (§5.0.3). KHÔNG đặt route bắt đầu bằng /identity hoặc /chinese (Vite proxy nuốt).
// Mọi route học nằm dưới `RequireAuth` (ẩn danh ⇒ /dang-nhap?returnTo=; 0 quyền ⇒ /403).
export const router = createBrowserRouter([
  {
    path: '/',
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <HomePage /> },
          {
            // Vùng quản trị (F3): mỗi trang con tự khai quyền — F4 thêm `nguoi-dung` (users.manage),
            // F10 thêm `bai-hoc`, `tu-vung` (content.manage). Vào thẳng URL mà thiếu quyền ⇒ /403.
            path: 'quan-tri',
            children: [
              {
                index: true,
                element: (
                  <RequirePermission permission={PERMISSIONS.USERS_MANAGE}>
                    <AdminHomePage />
                  </RequirePermission>
                ),
              },
            ],
          },
        ],
      },
    ],
  },
  { path: '/dang-nhap', element: <LoginPage /> },
  { path: '/dang-ky', element: <RegisterPage /> },
  { path: '/401', element: <UnauthorizedPage /> },
  { path: '/403', element: <ForbiddenPage /> },
  { path: '/404', element: <NotFoundPage /> },
  { path: '*', element: <Navigate to="/404" replace /> },
])
