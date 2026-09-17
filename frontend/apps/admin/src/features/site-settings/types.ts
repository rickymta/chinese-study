/**
 * Cấu hình site/SEO (hợp đồng §5.2.3 W3a `SiteSettingKeys`) — 11 khoá, PUT phải gửi ĐỦ. Frontend giữ danh sách
 * khoá để dựng form + bù khoá thiếu khi GET trả thiếu (BE seed bù nhưng phòng hờ).
 */
export const SITE_SETTING_KEYS = [
  'site.name',
  'site.tagline',
  'seo.default_title',
  'seo.default_description',
  'seo.og_image_media_id',
  'contact.email',
  'social.facebook',
  'social.youtube',
  'social.tiktok',
  'footer.text',
  'home.hero_mode',
] as const

export type SiteSettingKey = (typeof SITE_SETTING_KEYS)[number]

export const HERO_MODES = ['static', 'banners'] as const
export type HeroMode = (typeof HERO_MODES)[number]

export type SiteSettingValues = Record<Exclude<SiteSettingKey, 'home.hero_mode'>, string> & { 'home.hero_mode': HeroMode }

export interface SiteSettings {
  values: SiteSettingValues
  updatedAt: string | null
}

/** Mặc định khi máy chủ không trả khoá (giống seed BE). */
export const SITE_SETTING_DEFAULTS: SiteSettingValues = {
  'site.name': 'AntFarm',
  'site.tagline': '',
  'seo.default_title': '',
  'seo.default_description': '',
  'seo.og_image_media_id': '',
  'contact.email': '',
  'social.facebook': '',
  'social.youtube': '',
  'social.tiktok': '',
  'footer.text': '',
  'home.hero_mode': 'static',
}
