import { useState } from 'react'
import { Alert, Box, Button, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { bindField, describeAuthError, PasswordField, useAuth } from '@af/auth'
import { useToast } from '@af/ui'
import { passwordSchema } from '@af/utils'
import { changePassword } from '../api'

const schema = z
  .object({
    currentPassword: z.string().min(1, { error: 'Vui lòng nhập mật khẩu hiện tại' }),
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, { error: 'Vui lòng nhập lại mật khẩu mới' }),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    error: 'Mật khẩu nhập lại không khớp',
  })
  .refine((v) => v.newPassword !== v.currentPassword, {
    path: ['newPassword'],
    error: 'Mật khẩu mới phải khác mật khẩu hiện tại',
  })
type FormValues = z.infer<typeof schema>

/**
 * Tab "Mật khẩu" của `/ho-so` (F4 §5.3.F, R4-5): `POST /api/auth/password` (Bearer + cookie `af_rt`).
 * - `currentSessionKept=true` ⇒ reset form + toast (thiết bị khác bị đăng xuất, phiên này giữ).
 * - `false` (không nhận ra cookie ⇒ mọi phiên bị thu hồi) ⇒ `logout()` rồi về `/dang-nhap?reason=password-changed`.
 * 422 `WRONG_PASSWORD` ⇒ lỗi ô hiện tại; `PASSWORD_UNCHANGED` ⇒ lỗi ô mới (qua `describeAuthError`).
 */
export function ChangePasswordForm() {
  const { logout } = useAuth()
  const toast = useToast()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    setFormError(null)
    try {
      const res = await changePassword({ currentPassword, newPassword })
      if (res.currentSessionKept) {
        reset()
        toast.success(
          res.otherSessionsRevoked > 0
            ? 'Đã đổi mật khẩu. Các thiết bị khác đã bị đăng xuất.'
            : 'Đã đổi mật khẩu.',
        )
        return
      }
      // Mọi phiên (kể cả phiên này) đã bị thu hồi ⇒ đăng xuất phía client và giải thích ở trang đăng nhập.
      await logout()
      queryClient.clear()
      navigate('/dang-nhap?reason=password-changed', { replace: true })
    } catch (err) {
      const view = describeAuthError(err)
      for (const [field, message] of Object.entries(view.fieldErrors)) {
        if (field === 'currentPassword' || field === 'newPassword') setError(field, { type: 'server', message })
      }
      // Lỗi đã gắn vào ô (WRONG_PASSWORD / PASSWORD_UNCHANGED) thì không lặp lại ở Alert.
      if (!['WRONG_PASSWORD', 'PASSWORD_UNCHANGED'].includes(view.parsed.code ?? '')) setFormError(view.message)
    }
  })

  return (
    <Box component="form" onSubmit={onSubmit} noValidate>
      <Stack spacing={2} sx={{ maxWidth: 560 }}>
        {formError && <Alert severity="error">{formError}</Alert>}

        <PasswordField
          {...bindField(register('currentPassword'))}
          label="Mật khẩu hiện tại"
          autoComplete="current-password"
          fullWidth
          error={!!errors.currentPassword}
          helperText={errors.currentPassword?.message}
        />
        <PasswordField
          {...bindField(register('newPassword'))}
          label="Mật khẩu mới"
          autoComplete="new-password"
          fullWidth
          error={!!errors.newPassword}
          helperText={errors.newPassword?.message ?? 'Từ 8 đến 128 ký tự, không bắt buộc ký tự đặc biệt.'}
        />
        <PasswordField
          {...bindField(register('confirmPassword'))}
          label="Nhập lại mật khẩu mới"
          autoComplete="new-password"
          fullWidth
          error={!!errors.confirmPassword}
          helperText={errors.confirmPassword?.message}
        />

        <Typography variant="caption" color="text.secondary">
          Sau khi đổi, các thiết bị khác đang đăng nhập sẽ bị đăng xuất; thiết bị này vẫn dùng tiếp.
        </Typography>

        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button type="submit" variant="contained" loading={isSubmitting}>
            Đổi mật khẩu
          </Button>
        </Box>
      </Stack>
    </Box>
  )
}
