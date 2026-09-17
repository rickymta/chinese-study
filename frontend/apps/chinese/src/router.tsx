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
import { AdminLessonListPage } from './features/admin-content/pages/AdminLessonListPage'
import { AdminLessonEditPage } from './features/admin-content/pages/AdminLessonEditPage'
import { AdminWordReviewPage } from './features/admin-content/pages/AdminWordReviewPage'
import { PinyinPage } from './features/pinyin/pages/PinyinPage'
import { DictionarySearchPage } from './features/dictionary/pages/DictionarySearchPage'
import { WordDetailPage } from './features/dictionary/pages/WordDetailPage'
import { CharacterDetailPage } from './features/dictionary/pages/CharacterDetailPage'
import { ReviewHomePage } from './features/srs/pages/ReviewHomePage'
import { ReviewSessionPage } from './features/srs/pages/ReviewSessionPage'
import { LessonListPage } from './features/lessons/pages/LessonListPage'
import { LessonDetailPage } from './features/lessons/pages/LessonDetailPage'
import { WritingHomePage } from './features/writing/pages/WritingHomePage'
import { WritingPracticePage } from './features/writing/pages/WritingPracticePage'
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
            // F7: ôn tập thẻ SRS — trang tổng quan + phiên ôn (AppShell ẩn bottom nav ở `phien`).
            path: 'on-tap',
            element: <RequirePermission permission={PERMISSIONS.STUDY_USE} />,
            children: [
              { index: true, element: <ReviewHomePage /> },
              { path: 'phien', element: <ReviewSessionPage /> },
            ],
          },
          {
            // F9: bài học chủ đề — danh sách + trang bài (`?tab=noi-dung|tu-vung|quiz`). Bài không published ⇒ 404
            // do `createApiClient` điều hướng (R-LS1).
            path: 'bai-hoc',
            element: <RequirePermission permission={PERMISSIONS.STUDY_USE} />,
            children: [
              { index: true, element: <LessonListPage /> },
              { path: ':slug', element: <LessonDetailPage /> },
            ],
          },
          {
            // F8: luyện viết chữ Hán — danh sách theo bộ (`?tab=hsk1|bai-hoc|can-luyen|da-luyen&bai=&page=`) + trang
            // luyện một chữ (`:hanzi` encode, `?tab=xem|to-theo|tu-viet&tu=<bộ>`). Chữ không có trong kho ⇒ 404.
            path: 'luyen-viet',
            element: <RequirePermission permission={PERMISSIONS.STUDY_USE} />,
            children: [
              { index: true, element: <WritingHomePage /> },
              { path: ':hanzi', element: <WritingPracticePage /> },
            ],
          },
          {
            // F4: hồ sơ (`?tab=thong-tin|mat-khau|hoc-tap` — F7 thêm `hoc-tap`) — chỉ cần đăng nhập, không cần quyền riêng
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
              {
                // F10: soạn/duyệt bài học (`?trang-thai=&q=&page=`; trang soạn `?tab=thong-tin|noi-dung|tu-vung|quiz|xem-truoc`).
                path: 'bai-hoc',
                element: <RequirePermission permission={PERMISSIONS.CONTENT_MANAGE} />,
                children: [
                  { index: true, element: <AdminLessonListPage /> },
                  { path: ':id', element: <AdminLessonEditPage /> },
                ],
              },
              {
                // F10: duyệt nghĩa từ vựng (`?trang-thai=&han-viet=&hsk=&q=&page=&sua=<id>`).
                path: 'tu-vung',
                element: (
                  <RequirePermission permission={PERMISSIONS.CONTENT_MANAGE}>
                    <AdminWordReviewPage />
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
