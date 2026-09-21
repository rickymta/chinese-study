import { cmsApi } from '@/api/clients'
import { HERO_MODES, SITE_SETTING_DEFAULTS, SITE_SETTING_KEYS, type HeroMode, type SiteSettingValues, type SiteSettings } from './types'

// GET KHÔNG đặt `skipErrorRedirect`: 403 (mất `cms:site.manage`) kéo sang `/403` theo quy tắc trang lỗi thống nhất.
// PUT giữ lỗi để form báo tại chỗ (400 VALIDATION map `details` về ô).

/** Phản hồi thô có thể thiếu khoá ⇒ bù mặc định; khoá lạ bị bỏ (PUT gửi khoá lạ ⇒ BE 400). */
function normalize(raw: Partial<{ values: Record<string, unknown>; updatedAt: string | null }> | undefined): SiteSettings {
  const src = raw?.values && typeof raw.values === 'object' ? raw.values : {}
  const values = { ...SITE_SETTING_DEFAULTS }
  for (const key of SITE_SETTING_KEYS) {
    const v = src[key]
    if (typeof v !== 'string') continue
    if (key === 'home.hero_mode') values[key] = (HERO_MODES as readonly string[]).includes(v) ? (v as HeroMode) : 'static'
    else values[key] = v
  }
  return { values, updatedAt: typeof raw?.updatedAt === 'string' ? raw.updatedAt : null }
}

/** `GET /cms/api/admin/site-settings` → `{ values, updatedAt }`. */
export async function getSiteSettings(signal?: AbortSignal): Promise<SiteSettings> {
  const res = await cmsApi.get('/admin/site-settings', { signal })
  return normalize(res.data)
}

/** `PUT /cms/api/admin/site-settings { values }` → 200 như GET · 400 VALIDATION (`details` theo khoá). */
export async function updateSiteSettings(values: SiteSettingValues): Promise<SiteSettings> {
  const res = await cmsApi.put('/admin/site-settings', { values })
  return normalize(res.data)
}
