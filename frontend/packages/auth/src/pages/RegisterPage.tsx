import { useMemo, useState, type ReactNode } from 'react'
import { Alert, Box, Button, Link, Stack, TextField, Typography } from '@mui/material'
import { Link as RouterLink, Navigate, useLocation, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { displayNameSchema, emailSchema, passwordSchema } from '@af/utils'
import { useAuth } from '../AuthProvider'
import { describeAuthError } from '../authErrors'
import { sanitizeReturnTo } from '../returnTo'
import { FullScreenLoading } from '../components/FullScreenLoading'
import { PasswordField } from '../components/PasswordField'
import { bindField } from '../components/formUtils'
import { AuthShell } from './AuthShell'

const registerSchema = z
  .object({
    displayName: displayNameSchema,
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z.string().min(1, { error: 'Vui lòng nhập lại mật khẩu' }),
  })
  .refine((v) => v.password === v.confirmPassword, {
    path: ['confirmPassword'],
    error: 'Mật khẩu nhập lại không khớp',
  })
type RegisterForm = z.infer<typeof registerSchema>

const DEFAULT_TIME_ZONE = 'Asia/Ho_Chi_Minh'

/** Múi giờ của thiết bị theo `Intl` (hợp đồng §5.3.1) — người học đổi lại được ở hồ sơ (F4). */
function detectTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || DEFAULT_TIME_ZONE
  } catch {
    return DEFAULT_TIME_ZONE
  }
}

export interface RegisterPageProps {
  brand: string
  /** Đích sau khi đăng ký nếu URL không có `returnTo` hợp lệ. Mặc định `/`. */
  afterLogin?: string
  /** Đường dẫn trang đăng nhập. Mặc định `/dang-nhap`. */
  loginPath?: string
  logo?: ReactNode
}

/** Trang đăng ký DÙNG CHUNG (hợp đồng §5.3.1): gửi `timeZone` từ `Intl`; 409 `EMAIL_TAKEN` gắn vào ô email; 403 `REGISTRATION_CLOSED` hiện Alert. */
export function RegisterPage({ brand, afterLogin = '/', loginPath = '/dang-nhap', logo }: RegisterPageProps) {
  const { status, register: registerAccount } = useAuth()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const returnTo = sanitizeReturnTo(searchParams.get('returnTo')) ?? afterLogin
  const timeZone = useMemo(detectTimeZone, [])
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
  })

  if (status === 'loading') return <FullScreenLoading />
  if (status === 'authenticated') return <Navigate to={returnTo} replace />

  const onSubmit = handleSubmit(async ({ displayName, email, password }) => {
    setFormError(null)
    try {
      await registerAccount({ displayName, email, password, timeZone })
    } catch (err) {
      const view = describeAuthError(err)
      for (const [field, message] of Object.entries(view.fieldErrors)) {
        if (field === 'email' || field === 'password' || field === 'displayName') setError(field, { type: 'server', message })
      }
      setFormError(view.message)
    }
  })

  return (
    <AuthShell brand={brand} title="Tạo tài khoản" logo={logo}>
      <Box component="form" onSubmit={onSubmit} noValidate>
        <Stack spacing={2}>
          {formError && <Alert severity="error">{formError}</Alert>}

          <TextField
            {...bindField(register('displayName'))}
            label="Tên hiển thị"
            autoComplete="nickname"
            autoFocus
            fullWidth
            error={!!errors.displayName}
            helperText={errors.displayName?.message}
          />
          <TextField
            {...bindField(register('email'))}
            label="Email"
            type="email"
            autoComplete="email"
            fullWidth
            error={!!errors.email}
            helperText={errors.email?.message}
            slotProps={{ htmlInput: { inputMode: 'email', autoCapitalize: 'none', spellCheck: false } }}
          />
          <PasswordField
            {...bindField(register('password'))}
            label="Mật khẩu"
            autoComplete="new-password"
            fullWidth
            error={!!errors.password}
            helperText={errors.password?.message ?? 'Từ 8 đến 128 ký tự, không bắt buộc ký tự đặc biệt.'}
          />
          <PasswordField
            {...bindField(register('confirmPassword'))}
            label="Nhập lại mật khẩu"
            autoComplete="new-password"
            fullWidth
            error={!!errors.confirmPassword}
            helperText={errors.confirmPassword?.message}
          />

          <Typography variant="caption" color="text.secondary">
            Múi giờ theo thiết bị: <strong>{timeZone}</strong> — dùng để tính "hôm nay" cho thẻ ôn tập và chuỗi ngày
            học; có thể đổi trong hồ sơ.
          </Typography>

          <Button type="submit" variant="contained" size="large" fullWidth loading={isSubmitting}>
            Đăng ký
          </Button>

          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            Đã có tài khoản?{' '}
            <Link component={RouterLink} to={`${loginPath}${location.search}`} sx={{ fontWeight: 600 }}>
              Đăng nhập
            </Link>
          </Typography>
        </Stack>
      </Box>
    </AuthShell>
  )
}
