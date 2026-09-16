import { createBrowserRouter, Navigate } from 'react-router-dom'
import { NotFoundPage } from '@af/ui'
import { AppShell } from './layout/AppShell'
import { HomePage } from './pages/HomePage'

// Route slug tiếng Việt không dấu (§5.0.3). KHÔNG đặt route bắt đầu bằng /identity hoặc /chinese (Vite proxy nuốt).
// F3 thêm /401, /403 (ErrorPage kèm nút Đăng nhập/Đăng xuất); F2 thêm /dang-nhap, /dang-ky.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [{ index: true, element: <HomePage /> }],
  },
  { path: '/404', element: <NotFoundPage /> },
  { path: '*', element: <Navigate to="/404" replace /> },
])
