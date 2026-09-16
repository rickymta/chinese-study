import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { bindField, describeAuthError, useAuth } from '@af/auth'
import { TimeZoneAutocomplete, useToast } from '@af/ui'
import { detectBrowserTimeZone, displayNameSchema, normalizeTimeZone, parseApiError, timeZoneSchema } from '@af/utils'
import { updateAccount } from '../api'

const profileSchema = z.object({
  displayName: displayNameSchema,
  timeZone: timeZoneSchema,
})
type ProfileFormValues = z.infer<typeof profileSchema>

/**
 * Tab "Thông tin" của `/ho-so` (F4 §5.3.F): email chỉ đọc, tên hiển thị, múi giờ (TimeZoneAutocomplete).
 * Lưu ⇒ `PUT /api/account` ⇒ `refreshSession()` (R4-4: token mới mang tên/múi giờ mới ⇒ `/chinese/api/me` đồng bộ
 * ngay) ⇒ toast. 422 `INVALID_TIME_ZONE` ⇒ lỗi dưới ô múi giờ; lỗi khác ⇒ Alert tại chỗ.
 */
export function ProfileForm() {
  const { account, refreshSession } = useAuth()
  const toast = useToast()
  const [formError, setFormError] = useState<string | null>(null)
  const browserTimeZone = useMemo(() => detectBrowserTimeZone(), [])

  const defaults = useMemo<ProfileFormValues>(
    () => ({
      displayName: account?.displayName ?? '',
      timeZone: account?.timeZone ? normalizeTimeZone(account.timeZone) : '',
    }),
    [account?.displayName, account?.timeZone],
  )

  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileFormValues>({ resolver: zodResolver(profileSchema), defaultValues: defaults })

  // Tài khoản đổi (sau refreshSession, hoặc tab khác cập nhật) ⇒ đồng bộ lại giá trị mặc định (form "không đổi").
  useEffect(() => {
    reset(defaults)
  }, [defaults, reset])

  const currentTimeZone = watch('timeZone')
  const browserDiffers = !!browserTimeZone && normalizeTimeZone(currentTimeZone || '') !== browserTimeZone

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      const saved = await updateAccount({
        displayName: values.displayName.trim(),
        timeZone: normalizeTimeZone(values.timeZone), // R4-2: quy bí danh cũ trước khi gửi
      })
      // Đưa form về trạng thái "không đổi" với giá trị máy chủ đã lưu ngay, không chờ vòng refresh.
      reset({ displayName: saved.displayName, timeZone: normalizeTimeZone(saved.timeZone) })
      try {
        await refreshSession()
      } catch (refreshErr) {
        // Lưu đã thành công. 401/403 khi refresh = MẤT PHIÊN (session đã phát `lost` ⇒ RequireAuth đưa về đăng nhập)
        // ⇒ không toast gì thêm. Lỗi khác (mạng/5xx): token cũ vẫn dùng tới hạn — báo nhẹ, không coi là lỗi lưu.
        const status = parseApiError(refreshErr).status
        if (status === 401 || status === 403) return
        toast.warning('Đã lưu hồ sơ, nhưng chưa làm mới được phiên — tên/múi giờ mới sẽ hiện sau khi tải lại.')
        return
      }
      toast.success('Đã lưu hồ sơ')
    } catch (err) {
      const view = describeAuthError(err)
      for (const [field, message] of Object.entries(view.fieldErrors)) {
        if (field === 'displayName' || field === 'timeZone') setError(field, { type: 'server', message })
      }
      // Lỗi đã gắn vào ô thì không lặp lại ở Alert (INVALID_TIME_ZONE chỉ hiện dưới ô múi giờ).
      if (view.parsed.code !== 'INVALID_TIME_ZONE') setFormError(view.message)
    }
  })

  if (!account) return null

  return (
    <Box component="form" onSubmit={onSubmit} noValidate>
      <Stack spacing={2} sx={{ maxWidth: 560 }}>
        {formError && <Alert severity="error">{formError}</Alert>}

        <TextField
          label="Email"
          value={account.email}
          fullWidth
          disabled
          helperText="Không đổi được email trong phiên bản này."
          slotProps={{ input: { readOnly: true } }}
        />

        <TextField
          {...bindField(register('displayName'))}
          label="Tên hiển thị"
          autoComplete="nickname"
          fullWidth
          error={!!errors.displayName}
          helperText={errors.displayName?.message ?? 'Từ 1 đến 100 ký tự.'}
        />

        <Controller
          name="timeZone"
          control={control}
          render={({ field }) => (
            <TimeZoneAutocomplete
              value={field.value}
              onChange={field.onChange}
              onBlur={field.onBlur}
              name={field.name}
              inputRef={field.ref}
              error={!!errors.timeZone}
              helperText={
                errors.timeZone?.message ??
                'Múi giờ quyết định lúc nào sang ngày học mới (chuỗi ngày học, thẻ đến hạn). Đổi múi giờ không làm thay đổi lịch sử đã ghi.'
              }
              required
            />
          )}
        />

        {browserDiffers && (
          <Alert
            severity="info"
            action={
              <Button
                color="inherit"
                size="small"
                onClick={() => setValue('timeZone', browserTimeZone, { shouldDirty: true, shouldValidate: true })}
              >
                Dùng múi giờ này
              </Button>
            }
          >
            Trình duyệt của bạn đang ở múi giờ <strong>{browserTimeZone}</strong>.
          </Alert>
        )}

        <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end', flexWrap: 'wrap' }}>
          <Button color="inherit" onClick={() => reset(defaults)} disabled={!isDirty || isSubmitting}>
            Hoàn tác
          </Button>
          <Button type="submit" variant="contained" loading={isSubmitting} disabled={!isDirty}>
            Lưu
          </Button>
        </Box>
        {!isDirty && (
          <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'right' }}>
            Chưa có thay đổi.
          </Typography>
        )}
      </Stack>
    </Box>
  )
}
