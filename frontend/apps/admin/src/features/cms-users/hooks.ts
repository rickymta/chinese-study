import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getCmsRoles, getCmsUser, getCmsUsers, setCmsUserRoles } from './api'
import type { CmsUser, CmsUsersQuery } from './types'

export const CMS_USERS_KEYS = {
  all: ['cms-users'] as const,
  list: (q: string, page: number) => ['cms-users', 'list', q, page] as const,
  detail: (id: string) => ['cms-users', 'detail', id] as const,
  roles: ['cms-roles'] as const,
}

export const CMS_USERS_PAGE_SIZE = 20

/** Danh sách người dùng theo `?q=`/`?page=`; giữ dữ liệu cũ khi chuyển trang để bảng không nháy trắng. */
export function useCmsUsers(query: Required<Pick<CmsUsersQuery, 'q' | 'page'>>) {
  return useQuery({
    queryKey: CMS_USERS_KEYS.list(query.q, query.page),
    queryFn: ({ signal }) => getCmsUsers({ ...query, pageSize: CMS_USERS_PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
    staleTime: 0,
  })
}

/** Bản mới nhất của một người dùng (dialog đổi vai trò). `initialData` từ danh sách để mở dialog không chờ. */
export function useCmsUser(id: string | null, initialData?: CmsUser) {
  return useQuery({
    queryKey: CMS_USERS_KEYS.detail(id ?? ''),
    queryFn: ({ signal }) => getCmsUser(id!, signal),
    enabled: !!id,
    initialData,
    // initialData được coi là "cũ" ngay ⇒ vẫn gọi API lấy bản mới nhất khi mở.
    initialDataUpdatedAt: 0,
    staleTime: 0,
    retry: false,
  })
}

/** Danh mục vai trò CMS — ít đổi, giữ 5 phút. */
export function useCmsRoles() {
  return useQuery({
    queryKey: CMS_USERS_KEYS.roles,
    queryFn: ({ signal }) => getCmsRoles(signal),
    staleTime: 5 * 60_000,
  })
}

/** Thay tập vai trò; thành công ⇒ làm mới danh sách + chi tiết. Lỗi (422 LAST_ADMIN...) do dialog hiển thị. */
export function useSetCmsUserRoles() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roles }: { id: string; roles: string[] }) => setCmsUserRoles(id, { roles }),
    onSuccess: (updated) => {
      queryClient.setQueryData(CMS_USERS_KEYS.detail(updated.id), updated)
      void queryClient.invalidateQueries({ queryKey: CMS_USERS_KEYS.all })
    },
  })
}
