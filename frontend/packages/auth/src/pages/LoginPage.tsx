import { useState, type ReactNode } from 'react'
import { Alert, Box, Button, Link, Stack, TextField, Typography } from '@mui/material'
import { Link as RouterLink, Navigate, useLocation, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { emailSchema } from '@af/utils'
import { useAuth } from '../AuthProvider'
import { describeAuthError } from '../authErrors'
import { sanitizeReturnTo } from '../returnTo'
import { FullScreenLoading } from '../components/FullScreenLoading'
import { PasswordField } from '../components/PasswordField'
import { bindField } from '../components/formUtils'
import { AuthShell } from './AuthShell'

// Đăng nhập chỉ cần "có nhập" — không nhắc luật 8 ký tự ở đây để không gợi ý kẻ dò mật khẩu (R-A8: thông báo không phân biệt).
const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, { error: 'Vui lòng nhập mật khẩu' }),
})
type LoginForm = z.infer<typeof loginSchema>

export interface LoginPageProps {
  /** Tên app hiện trên đầu thẻ, vd "AntFarm · Tiếng Trung". */
  brand: string
  /** Đích sau khi đăng nhập nếu URL không có `returnTo` hợp lệ. Mặc định `/`. */
  afterLogin?: string
  /** Đường dẫn trang đăng ký; `null` ⇒ ẩn liên kết (khi hệ thống đóng đăng ký — D4). Mặc định `/dang-ky`. */
  registerPath?: string | null
  logo?: ReactNode
}

/**
 * Trang đăng nhập DÙNG CHUNG mọi app ngôn ngữ (hợp đồng §5.3.1): MUI + react-hook-form + zod; lỗi 401/423/403/429
 * hiện `Alert` tại chỗ; `?returnTo=` (chỉ đường dẫn nội bộ), `?reason=expired` (phiên hết hạn) và
 * `?reason=password-changed` (F4: đổi mật khẩu xong phải đăng nhập lại).
 */
export function LoginPage({ brand, afterLogin = '/', registerPath = '/dang-ky', logo }: LoginPageProps) {
  const { status, login } = useAuth()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const returnTo = sanitizeReturnTo(searchParams.get('returnTo')) ?? afterLogin
  const reason = searchParams.get('reason')
  // `expired`: mất phiên (RequireAuth); `password-changed` (F4): đổi mật khẩu mà không giữ được phiên hiện tại.
  const reasonMessage =
    reason === 'expired'
      ? 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại để tiếp tục.'
      : reason === 'password-changed'
        ? 'Bạn vừa đổi mật khẩu. Vui lòng đăng nhập lại bằng mật khẩu mới.'
        : null
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })

  if (status === 'loading') return <FullScreenLoading />
  if (status === 'authenticated') return <Navigate to={returnTo} replace />

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await login(values)
      // Thành công ⇒ `status` chuyển `authenticated` ⇒ nhánh <Navigate> ở trên tự đưa về `returnTo`.
    } catch (err) {
      const view = describeAuthError(err)
      for (const [field, message] of Object.entries(view.fieldErrors)) {
        if (field === 'email' || field === 'password') setError(field, { type: 'server', message })
      }
      setFormError(view.message)
    }
  })

  return (
    <AuthShell brand={brand} title="Đăng nhập" logo={logo}>
      <Box component="form" onSubmit={onSubmit} noValidate>
        <Stack spacing={2}>
          {reasonMessage && !formError && <Alert severity="info">{reasonMessage}</Alert>}
          {formError && <Alert severity="error">{formError}</Alert>}

          <TextField
            {...bindField(register('email'))}
            label="Email"
            type="email"
            autoComplete="email"
            autoFocus
            fullWidth
            error={!!errors.email}
            helperText={errors.email?.message}
            slotProps={{ htmlInput: { inputMode: 'email', autoCapitalize: 'none', spellCheck: false } }}
          />
          <PasswordField
            {...bindField(register('password'))}
            label="Mật khẩu"
            autoComplete="current-password"
            fullWidth
            error={!!errors.password}
            helperText={errors.password?.message}
          />

          <Button type="submit" variant="contained" size="large" fullWidth loading={isSubmitting}>
            Đăng nhập
          </Button>

          {registerPath && (
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
              Chưa có tài khoản?{' '}
              <Link component={RouterLink} to={`${registerPath}${location.search}`} sx={{ fontWeight: 600 }}>
                Đăng ký
              </Link>
            </Typography>
          )}
        </Stack>
      </Box>
    </AuthShell>
  )
}
