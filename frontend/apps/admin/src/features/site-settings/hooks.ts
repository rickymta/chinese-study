import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getSiteSettings, updateSiteSettings } from './api'
import type { SiteSettingValues } from './types'

export const SITE_SETTINGS_KEY = ['cms', 'site-settings'] as const

/** Cấu hình site — `staleTime: 0` để F5/quay lại luôn lấy bản mới (một người biên tập, last-write-wins). */
export function useSiteSettings() {
  return useQuery({
    queryKey: SITE_SETTINGS_KEY,
    queryFn: ({ signal }) => getSiteSettings(signal),
    staleTime: 0,
  })
}

/** Lưu đủ 11 khoá; thành công ⇒ ghi thẳng cache (form reset theo bản máy chủ trả). Lỗi do form hiển thị. */
export function useUpdateSiteSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: SiteSettingValues) => updateSiteSettings(values),
    onSuccess: (data) => {
      queryClient.setQueryData(SITE_SETTINGS_KEY, data)
    },
  })
}
