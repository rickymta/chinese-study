import { useEffect } from 'react'
import { Alert, Box, Button, Card, CardContent, CardHeader, FormControl, FormControlLabel, FormLabel, Radio, RadioGroup, Skeleton, Stack, TextField, Typography } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { PageContainer, StickyActionBar, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useUnsavedChangesGuard } from '@/hooks/useUnsavedChangesGuard'
import { useSiteSettings, useUpdateSiteSettings } from '../hooks'
import { SITE_SETTING_KEYS, type SiteSettingKey, type SiteSettingValues } from '../types'

const HTTPS_URL = /^https:\/\/\S+$/
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/

const optionalHttps = z
  .string()
  .trim()
  .max(300, 'Tối đa 300 ký tự')
  .refine((v) => v === '' || HTTPS_URL.test(v), 'Phải là URL bắt đầu bằng https://')

/** Phản chiếu đúng luật BE §5.2.3 `SiteSettingKeys` (sau trim). */
// Tên trường form KHÔNG có dấu chấm — react-hook-form coi `site.name` là đường dẫn lồng `site → name`.
const schema = z.object({
  siteName: z.string().trim().min(1, 'Bắt buộc').max(100, 'Tối đa 100 ký tự'),
  siteTagline: z.string().trim().max(160, 'Tối đa 160 ký tự'),
  seoDefaultTitle: z.string().trim().max(70, 'Tối đa 70 ký tự'),
  seoDefaultDescription: z.string().trim().max(160, 'Tối đa 160 ký tự'),
  seoOgImageMediaId: z.string().trim().refine((v) => v === '' || GUID.test(v), 'Phải là GUID'),
  contactEmail: z
    .string()
    .trim()
    .max(254, 'Tối đa 254 ký tự')
    .refine((v) => v === '' || EMAIL.test(v), 'Email không hợp lệ'),
  socialFacebook: optionalHttps,
  socialYoutube: optionalHttps,
  socialTiktok: optionalHttps,
  footerText: z.string().trim().max(500, 'Tối đa 500 ký tự'),
  homeHeroMode: z.enum(['static', 'banners']),
})

type FormValues = z.infer<typeof schema>
type FormKey = keyof FormValues

/** Khoá API ⇄ tên trường form. */
const KEY_OF_FIELD: Record<FormKey, SiteSettingKey> = {
  siteName: 'site.name',
  siteTagline: 'site.tagline',
  seoDefaultTitle: 'seo.default_title',
  seoDefaultDescription: 'seo.default_description',
  seoOgImageMediaId: 'seo.og_image_media_id',
  contactEmail: 'contact.email',
  socialFacebook: 'social.facebook',
  socialYoutube: 'social.youtube',
  socialTiktok: 'social.tiktok',
  footerText: 'footer.text',
  homeHeroMode: 'home.hero_mode',
}
const FIELD_OF_KEY = Object.fromEntries(Object.entries(KEY_OF_FIELD).map(([f, k]) => [k, f])) as Record<SiteSettingKey, FormKey>

const toForm = (values: SiteSettingValues): FormValues => {
  const out = {} as Record<FormKey, string>
  for (const [field, key] of Object.entries(KEY_OF_FIELD) as [FormKey, SiteSettingKey][]) out[field] = values[key]
  return out as unknown as FormValues
}
const fromForm = (values: FormValues): SiteSettingValues => {
  const out = {} as SiteSettingValues
  for (const [field, key] of Object.entries(KEY_OF_FIELD) as [FormKey, SiteSettingKey][]) {
    if (key === 'home.hero_mode') out[key] = values.homeHeroMode
    else out[key] = values[field]
  }
  return out
}

/**
 * `/website/cau-hinh` (hợp đồng W3a §5.3.3a, cần `cms:site.manage`): form 11 khoá chia 5 thẻ, Lưu ở
 * `StickyActionBar` (disabled khi không đổi), 400 VALIDATION map `details` về đúng ô, chặn rời trang khi chưa lưu.
 * Ô ảnh OG (`seo.og_image_media_id`) ẨN tới W4 nhưng vẫn gửi lại giá trị hiện có (PUT phải đủ 11 khoá).
 */
export function SiteSettingsPage() {
  const toast = useToast()
  const settings = useSiteSettings()
  const mutation = useUpdateSiteSettings()

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: settings.data ? toForm(settings.data.values) : undefined,
    mode: 'onBlur',
  })
  const { control, handleSubmit, reset, setError, formState, watch } = form

  // Dữ liệu máy chủ về (hoặc lưu xong) mà form chưa bị đụng ⇒ đồng bộ giá trị form.
  useEffect(() => {
    if (settings.data && !formState.isDirty) reset(toForm(settings.data.values))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [settings.data])

  useUnsavedChangesGuard(formState.isDirty)

  const onSubmit = async (values: FormValues) => {
    try {
      const saved = await mutation.mutateAsync(fromForm(values))
      reset(toForm(saved.values))
      toast.success('Đã lưu cấu hình website')
    } catch (err) {
      const parsed = parseApiError(err)
      let mapped = false
      if (parsed.fieldErrors) {
        // `parseApiError` rút phần sau dấu chấm cuối (`site.name` ⇒ `name`, `values.seo.default_title` ⇒
        // `default_title`) — 11 khoá có đuôi khác nhau nên khớp theo đuôi là đủ; vẫn thử khoá đầy đủ trước.
        for (const [key, messages] of Object.entries(parsed.fieldErrors)) {
          const match = SITE_SETTING_KEYS.find((k) => key === k || key === k.split('.').pop())
          if (match) {
            setError(FIELD_OF_KEY[match], { type: 'server', message: messages.join(' ') })
            mapped = true
          }
        }
      }
      if (!mapped) toast.error(parsed.message)
    }
  }

  const err = settings.error ? parseApiError(settings.error) : null
  const title = watch('seoDefaultTitle') ?? ''
  const description = watch('seoDefaultDescription') ?? ''

  const textField = (name: Exclude<FormKey, 'homeHeroMode'>, label: string, opts?: { multiline?: boolean; helper?: string; type?: string; counterMax?: number; placeholder?: string }) => (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => {
        const len = field.value?.length ?? 0
        const helper = fieldState.error?.message ?? (opts?.counterMax ? `${len}/${opts.counterMax} ký tự` : opts?.helper)
        return (
          <TextField
            {...field}
            value={field.value ?? ''}
            label={label}
            type={opts?.type}
            placeholder={opts?.placeholder}
            fullWidth
            multiline={opts?.multiline}
            minRows={opts?.multiline ? 3 : undefined}
            error={!!fieldState.error}
            helperText={helper}
            disabled={mutation.isPending}
            slotProps={{ htmlInput: { autoCapitalize: 'none', spellCheck: opts?.multiline ?? false } }}
          />
        )
      }}
    />
  )

  return (
    <PageContainer title="Cấu hình website" maxWidth={800}>
      {err && (
        <Alert
          severity="error"
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={() => void settings.refetch()}>
              Thử lại
            </Button>
          }
        >
          Không tải được cấu hình: {err.message}
        </Alert>
      )}

      {settings.isPending ? (
        <Stack spacing={2}>
          <Skeleton variant="rounded" height={160} />
          <Skeleton variant="rounded" height={200} />
          <Skeleton variant="rounded" height={200} />
        </Stack>
      ) : settings.data ? (
        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2}>
            <Card variant="outlined">
              <CardHeader title="Thông tin chung" slotProps={{ title: { variant: 'h6' } }} />
              <CardContent>
                <Stack spacing={2}>
                  {textField('siteName', 'Tên website')}
                  {textField('siteTagline', 'Khẩu hiệu', { counterMax: 160 })}
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title="SEO mặc định" subheader="Dùng khi trang không tự đặt tiêu đề/mô tả riêng." slotProps={{ title: { variant: 'h6' } }} />
              <CardContent>
                <Stack spacing={2}>
                  {textField('seoDefaultTitle', 'Tiêu đề mặc định', { counterMax: 70 })}
                  {textField('seoDefaultDescription', 'Mô tả mặc định', { multiline: true, counterMax: 160 })}
                  {(title.length > 60 || description.length > 150) && (
                    <Alert severity="info">Google thường cắt tiêu đề quá ~60 và mô tả quá ~155 ký tự.</Alert>
                  )}
                  {/* Ô ảnh OG (`seo.og_image_media_id`) ẩn tới W4 — thư viện ảnh chưa có; giá trị hiện tại vẫn được gửi lại. */}
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title="Liên hệ & mạng xã hội" slotProps={{ title: { variant: 'h6' } }} />
              <CardContent>
                <Stack spacing={2}>
                  {textField('contactEmail', 'Email liên hệ', { type: 'email', helper: 'Hiện ở chân trang và trang Liên hệ.' })}
                  {textField('socialFacebook', 'Facebook', { type: 'url', placeholder: 'https://facebook.com/...' })}
                  {textField('socialYoutube', 'YouTube', { type: 'url', placeholder: 'https://youtube.com/...' })}
                  {textField('socialTiktok', 'TikTok', { type: 'url', placeholder: 'https://tiktok.com/@...' })}
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title="Chân trang" slotProps={{ title: { variant: 'h6' } }} />
              <CardContent>{textField('footerText', 'Dòng chữ chân trang', { multiline: true, counterMax: 500 })}</CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title="Trang chủ" slotProps={{ title: { variant: 'h6' } }} />
              <CardContent>
                <Controller
                  name="homeHeroMode"
                  control={control}
                  render={({ field, fieldState }) => (
                    <FormControl error={!!fieldState.error} disabled={mutation.isPending}>
                      <FormLabel id="hero-mode-label">Phần đầu trang chủ</FormLabel>
                      <RadioGroup aria-labelledby="hero-mode-label" {...field}>
                        <FormControlLabel value="static" control={<Radio />} label="Nội dung tĩnh" />
                        <FormControlLabel value="banners" control={<Radio />} label="Banner (cần W5 — chưa có banner thì website hiện nội dung tĩnh)" />
                      </RadioGroup>
                      {fieldState.error && (
                        <Typography variant="caption" color="error">
                          {fieldState.error.message}
                        </Typography>
                      )}
                    </FormControl>
                  )}
                />
              </CardContent>
            </Card>

            <StickyActionBar>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Button type="submit" variant="contained" loading={mutation.isPending} disabled={!formState.isDirty}>
                  Lưu
                </Button>
                <Button color="inherit" onClick={() => reset(toForm(settings.data.values))} disabled={!formState.isDirty || mutation.isPending}>
                  Huỷ thay đổi
                </Button>
                {settings.data.updatedAt && (
                  <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
                    Cập nhật lần cuối: {new Date(settings.data.updatedAt).toLocaleString('vi-VN')}
                  </Typography>
                )}
              </Box>
            </StickyActionBar>
          </Stack>
        </Box>
      ) : null}
    </PageContainer>
  )
}
