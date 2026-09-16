import { createBrowserRouter, Navigate } from 'react-router-dom'
import { RequireAuth } from '@af/auth'
import { NotFoundPage } from '@af/ui'
import { AppShell } from './layout/AppShell'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './features/auth/pages/LoginPage'
import { RegisterPage } from './features/auth/pages/RegisterPage'
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
        children: [{ index: true, element: <HomePage /> }],
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
