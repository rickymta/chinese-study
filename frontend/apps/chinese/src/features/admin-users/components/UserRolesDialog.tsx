import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, Checkbox, Chip, FormControlLabel, Skeleton, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@af/auth'
import { AppDialog, useConfirm, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { labelOfPermission, PERMISSIONS } from '@/features/auth/permissions'
import { useAdminUser, useRoles, useSetUserRoles } from '../hooks'
import type { AdminUser } from '../types'

const ADMIN = 'admin'

export interface UserRolesDialogProps {
  /** Người đang sửa (từ danh sách) — `null` ⇒ đóng. */
  user: AdminUser | null
  onClose: () => void
}

const sameSet = (a: readonly string[], b: readonly string[]) => a.length === b.length && a.every((x) => b.includes(x))

/**
 * Hộp thoại đổi vai trò (F4 §5.3.F): `AppDialog` KHÔNG `closeOnBackdrop` (có thao tác ghi). Checkbox mỗi vai trò
 * (tên + mô tả + chip quyền). Cảnh báo động: bỏ hết vai trò / tự gỡ admin / gỡ admin của người bootstrap.
 * Tự gỡ admin ⇒ `useConfirm` (danger) trước khi lưu. 422 `LAST_ADMIN` ⇒ Alert lỗi TRONG dialog, không đóng.
 * Thành công ⇒ đóng, toast, invalidate; sửa chính mình ⇒ `reloadMe()`, mất `users.manage` ⇒ về `/`.
 */
export function UserRolesDialog({ user, onClose }: UserRolesDialogProps) {
  const { account, reloadMe } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const navigate = useNavigate()
  const roles = useRoles()
  const detail = useAdminUser(user?.id ?? null, user ?? undefined)
  const mutation = useSetUserRoles()

  const target = detail.data ?? user
  const isSelf = !!user && user.id === account?.id

  const [selected, setSelected] = useState<string[]>([])
  const [touched, setTouched] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  // Mở dialog / bản mới nhất về mà người dùng chưa đụng vào ⇒ đồng bộ lựa chọn với vai trò thật.
  useEffect(() => {
    if (!user) return
    if (!touched) setSelected(target?.roles ?? user.roles)
  }, [user, target, touched])

  // Đổi người ⇒ reset trạng thái.
  useEffect(() => {
    setTouched(false)
    setServerError(null)
    mutation.reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id])

  const original = target?.roles ?? []
  const unchanged = sameSet(selected, original)
  const removingAdmin = original.includes(ADMIN) && !selected.includes(ADMIN)
  const removingAll = selected.length === 0 && original.length > 0
  const selfLosesAdmin = isSelf && removingAdmin
  const selfLosesManage = useMemo(() => {
    if (!isSelf || !roles.data) return false
    const perms = new Set(roles.data.filter((r) => selected.includes(r.code)).flatMap((r) => r.permissions))
    return !perms.has(PERMISSIONS.USERS_MANAGE)
  }, [isSelf, roles.data, selected])

  const toggle = (code: string) => {
    setTouched(true)
    setServerError(null)
    setSelected((prev) => (prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]))
  }

  const save = async () => {
    if (!target) return
    setServerError(null)
    if (selfLosesAdmin || selfLosesManage) {
      const ok = await confirm({
        title: 'Gỡ quyền quản trị của chính bạn?',
        message: 'Bạn sẽ mất quyền quản trị ngay sau khi lưu và không tự cấp lại được. Chỉ tiếp tục nếu còn quản trị viên khác.',
        confirmText: 'Gỡ quyền của tôi',
        tone: 'danger',
      })
      if (!ok) return
    }
    try {
      await mutation.mutateAsync({ id: target.id, roles: selected })
      toast.success(`Đã cập nhật vai trò của ${target.displayName || target.email}`)
      onClose()
      if (isSelf) {
        await reloadMe()
        if (selfLosesManage) navigate('/', { replace: true })
      }
    } catch (err) {
      const parsed = parseApiError(err)
      if (parsed.code === 'LAST_ADMIN') setServerError('Không thể gỡ quản trị viên cuối cùng.')
      else if (parsed.code === 'UNKNOWN_ROLE') setServerError(`Vai trò không tồn tại: ${(parsed.details?.roles as string[] | undefined)?.join(', ') ?? ''}`.trim())
      else if (parsed.status === 404) setServerError('Người dùng này không còn tồn tại ở dịch vụ tiếng Trung.')
      else setServerError(parsed.message)
    }
  }

  const detailError = detail.error ? parseApiError(detail.error) : null

  return (
    <AppDialog
      open={user !== null}
      onClose={onClose}
      title="Đổi vai trò"
      actions={
        <>
          <Button color="inherit" onClick={onClose} disabled={mutation.isPending}>
            Huỷ
          </Button>
          <Button variant="contained" onClick={() => void save()} loading={mutation.isPending} disabled={unchanged || !target}>
            Lưu
          </Button>
        </>
      }
    >
      {target && (
        <Stack spacing={2}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {target.displayName || '—'}
              {isSelf && (
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>
                  (bạn)
                </Typography>
              )}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ wordBreak: 'break-all' }}>
              {target.email}
            </Typography>
          </Box>

          {detailError && detailError.status !== 404 && (
            <Alert severity="warning">Không tải được bản mới nhất ({detailError.message}) — đang dùng dữ liệu từ danh sách.</Alert>
          )}
          {detailError?.status === 404 && <Alert severity="error">Người dùng này không còn tồn tại ở dịch vụ tiếng Trung.</Alert>}

          {roles.isPending ? (
            <Stack spacing={1}>
              <Skeleton variant="rounded" height={72} />
              <Skeleton variant="rounded" height={72} />
            </Stack>
          ) : roles.error ? (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={() => void roles.refetch()}>
                  Thử lại
                </Button>
              }
            >
              Không tải được danh sách vai trò: {parseApiError(roles.error).message}
            </Alert>
          ) : (
            <Stack spacing={1}>
              {roles.data?.map((role) => (
                <Box
                  key={role.code}
                  sx={{ border: 1, borderColor: selected.includes(role.code) ? 'primary.main' : 'divider', borderRadius: 2, px: 1.5, py: 1 }}
                >
                  <FormControlLabel
                    control={<Checkbox checked={selected.includes(role.code)} onChange={() => toggle(role.code)} disabled={mutation.isPending} />}
                    label={
                      <Box>
                        <Typography variant="body1" sx={{ fontWeight: 600 }}>
                          {role.name}{' '}
                          <Typography component="span" variant="caption" color="text.secondary">
                            ({role.code})
                          </Typography>
                        </Typography>
                        {role.description && (
                          <Typography variant="body2" color="text.secondary">
                            {role.description}
                          </Typography>
                        )}
                      </Box>
                    }
                    sx={{ alignItems: 'flex-start', m: 0, '& .MuiCheckbox-root': { pt: 0.25 } }}
                  />
                  {role.permissions.length > 0 && (
                    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 0.5, pl: 5 }}>
                      {role.permissions.map((p) => (
                        <Chip key={p} size="small" variant="outlined" label={labelOfPermission(p)} sx={{ height: 22, fontSize: 11 }} />
                      ))}
                    </Box>
                  )}
                </Box>
              ))}
            </Stack>
          )}

          {removingAll && <Alert severity="warning">Người này sẽ không vào được ứng dụng tiếng Trung.</Alert>}
          {selfLosesAdmin && <Alert severity="warning">Bạn sẽ mất quyền quản trị ngay sau khi lưu.</Alert>}
          {removingAdmin && target.isBootstrapAdmin && (
            <Alert severity="info">Email này nằm trong cấu hình quản trị — khởi động lại dịch vụ sẽ gán lại quyền admin.</Alert>
          )}
          {serverError && <Alert severity="error">{serverError}</Alert>}
        </Stack>
      )}
    </AppDialog>
  )
}
