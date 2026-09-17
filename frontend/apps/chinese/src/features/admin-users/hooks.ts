import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getAdminUser, getAdminUsers, getRoles, setUserRoles } from './api'
import type { AdminUser, AdminUsersQuery } from './types'

export const ADMIN_USERS_KEYS = {
  all: ['admin-users'] as const,
  list: (q: string, page: number) => ['admin-users', 'list', q, page] as const,
  detail: (id: string) => ['admin-users', 'detail', id] as const,
  roles: ['admin-roles'] as const,
}

export const ADMIN_USERS_PAGE_SIZE = 20

/** Danh sách người dùng theo `?q=`/`?page=`; giữ dữ liệu cũ khi chuyển trang để bảng không nháy trắng. */
export function useAdminUsers(query: Required<Pick<AdminUsersQuery, 'q' | 'page'>>) {
  return useQuery({
    queryKey: ADMIN_USERS_KEYS.list(query.q, query.page),
    queryFn: ({ signal }) => getAdminUsers({ ...query, pageSize: ADMIN_USERS_PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
    staleTime: 0,
  })
}

/** Bản mới nhất của một người dùng (dialog đổi vai trò). `initialData` từ danh sách để mở dialog không chờ. */
export function useAdminUser(id: string | null, initialData?: AdminUser) {
  return useQuery({
    queryKey: ADMIN_USERS_KEYS.detail(id ?? ''),
    queryFn: ({ signal }) => getAdminUser(id!, signal),
    enabled: !!id,
    initialData,
    // initialData được coi là "cũ" ngay ⇒ vẫn gọi API lấy bản mới nhất khi mở.
    initialDataUpdatedAt: 0,
    staleTime: 0,
    retry: false,
  })
}

/** Danh mục vai trò — ít đổi, giữ 5 phút. */
export function useRoles() {
  return useQuery({
    queryKey: ADMIN_USERS_KEYS.roles,
    queryFn: ({ signal }) => getRoles(signal),
    staleTime: 5 * 60_000,
  })
}

/** Thay tập vai trò; thành công ⇒ làm mới danh sách + chi tiết. Lỗi (422 LAST_ADMIN...) do dialog hiển thị. */
export function useSetUserRoles() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roles }: { id: string; roles: string[] }) => setUserRoles(id, { roles }),
    onSuccess: (updated) => {
      queryClient.setQueryData(ADMIN_USERS_KEYS.detail(updated.id), updated)
      void queryClient.invalidateQueries({ queryKey: ADMIN_USERS_KEYS.all })
    },
  })
}
