import { createBrowserRouter, Navigate } from 'react-router-dom'
import { RequireAuth, RequirePermission } from '@af/auth'
import { NotFoundPage } from '@af/ui'
import { AppShell } from './layout/AppShell'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './features/auth/pages/LoginPage'
import { RegisterPage } from './features/auth/pages/RegisterPage'
import { PERMISSIONS } from './features/auth/permissions'
import { AdminHomePage } from './features/admin/pages/AdminHomePage'
import { AdminUsersPage } from './features/admin-users/pages/AdminUsersPage'
import { PinyinPage } from './features/pinyin/pages/PinyinPage'
import { DictionarySearchPage } from './features/dictionary/pages/DictionarySearchPage'
import { WordDetailPage } from './features/dictionary/pages/WordDetailPage'
import { CharacterDetailPage } from './features/dictionary/pages/CharacterDetailPage'
import { ProfilePage } from './features/profile/pages/ProfilePage'
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
            // F5: pinyin & thanh điệu (`?tab=huong-dan|bang|luyen`) — cần `study.use` như mọi route học.
            path: 'pinyin',
            element: (
              <RequirePermission permission={PERMISSIONS.STUDY_USE}>
                <PinyinPage />
              </RequirePermission>
            ),
          },
          {
            // F6: từ điển — tra từ (`?q=&hsk=&page=`), chi tiết từ, chi tiết chữ. `chu/:hanzi` khai TRƯỚC `:id`
            // cho dễ đọc (router xếp hạng theo đoạn tĩnh nên thứ tự không quyết định, nhưng giữ rõ ý).
            path: 'tu-dien',
            element: <RequirePermission permission={PERMISSIONS.STUDY_USE} />,
            children: [
              { index: true, element: <DictionarySearchPage /> },
              { path: 'chu/:hanzi', element: <CharacterDetailPage /> },
              { path: ':id', element: <WordDetailPage /> },
            ],
          },
          {
            // F4: hồ sơ (`?tab=thong-tin|mat-khau`; F7 thêm `hoc-tap`) — chỉ cần đăng nhập, không cần quyền riêng
            // (người 0 quyền đã bị `RequireAuth` đưa tới /403 trước khi tới đây).
            path: 'ho-so',
            element: <ProfilePage />,
          },
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
              {
                path: 'nguoi-dung',
                element: (
                  <RequirePermission permission={PERMISSIONS.USERS_MANAGE}>
                    <AdminUsersPage />
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
