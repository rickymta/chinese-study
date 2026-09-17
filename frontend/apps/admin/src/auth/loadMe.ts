import { cmsApi, languageApis } from '@/api/clients'
import { LANGUAGE_MODULES } from '@/modules/registry'
import { CMS_SERVICE } from './permissions'
import { mergeServiceResults, type ServiceMeSource } from './mergeMe'
import type { AdminMeInfo } from './types'

/**
 * `loadMe` của admin (hợp đồng W2 §5.3.1) — gọi SONG SONG `GET /cms/api/me` và `GET /<lang>/api/me` cho mọi
 * module trong registry, rồi gộp bằng `mergeServiceResults` (luật ở đó). Truyền vào `AuthProvider`.
 *
 * `skipErrorRedirect`: lời gọi NỀN lúc mở app — service chưa chạy (404/502) hay từ chối (403) không được kéo cả
 * trang sang `/404`/`/403` rồi lại gọi `/me` thêm lần nữa. 401 vẫn đi qua làm mới token + `onAuthLost` như mọi request.
 *
 * Lưu ý R-W12: gọi `/chinese/api/me` làm chinese-backend provision người này với vai trò mặc định `learner` —
 * chấp nhận (tài khoản dùng chung nền tảng, `learner` không phải quyền admin nên không mở gì thêm ở đây).
 */
export async function loadMe(): Promise<AdminMeInfo> {
  const sources: Array<ServiceMeSource & { call: () => Promise<unknown> }> = [
    {
      code: CMS_SERVICE,
      label: 'CMS',
      call: () => cmsApi.get<unknown>('/me', { skipErrorRedirect: true }).then((r) => r.data),
    },
    ...LANGUAGE_MODULES.map((m) => ({
      code: m.code,
      label: m.label,
      adminPermissions: m.adminPermissions,
      call: () => languageApis[m.code]!.get<unknown>('/me', { skipErrorRedirect: true }).then((r) => r.data),
    })),
  ]

  const outcomes = await Promise.allSettled(sources.map((s) => s.call()))
  return mergeServiceResults(
    sources.map(({ code, label, adminPermissions }, i) => ({ code, label, adminPermissions, outcome: outcomes[i]! })),
  )
}
