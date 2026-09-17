import { Box, Button, Card, CardContent, Chip, Skeleton, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography, useMediaQuery, useTheme } from '@mui/material'
import { formatRelativeTime } from '@af/utils'
import { labelOfCmsRole } from '@/auth/permissions'
import type { CmsUser } from '../types'

export interface UserListProps {
  users: CmsUser[]
  /** Id tài khoản đang đăng nhập — gắn nhãn "(bạn)". */
  currentUserId: string | null
  loading?: boolean
  onEditRoles: (user: CmsUser) => void
}

/** Chip vai trò: `admin` màu primary, còn lại viền; rỗng ⇒ chữ mờ "Không có vai trò" (fail-closed R-W2). */
function RoleChips({ roles }: { roles: string[] }) {
  if (roles.length === 0) {
    return (
      <Typography variant="body2" color="text.disabled" component="span">
        Không có vai trò
      </Typography>
    )
  }
  return (
    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
      {roles.map((r) => (
        <Chip key={r} size="small" label={labelOfCmsRole(r)} color={r === 'admin' ? 'primary' : 'default'} variant={r === 'admin' ? 'filled' : 'outlined'} />
      ))}
    </Box>
  )
}

/** Nhãn phụ "(bạn)" / "Admin theo cấu hình" — đặt NGOÀI phần tên `noWrap` để không bị cắt ở 375px và không lồng div trong <p>. */
function Badges({ user, isSelf }: { user: CmsUser; isSelf: boolean }) {
  if (!isSelf && !user.isBootstrapAdmin) return null
  return (
    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 0.25 }}>
      {isSelf && <Chip size="small" label="bạn" variant="outlined" color="secondary" />}
      {user.isBootstrapAdmin && <Chip size="small" label="Admin theo cấu hình" variant="outlined" />}
    </Box>
  )
}

/**
 * Danh sách người dùng CMS: bảng ở md+, thẻ ở xs–sm (375px). Mỗi người: tên, email, chip vai trò, lần đầu/lần cuối
 * (tương đối), nhãn "(bạn)" / "Admin theo cấu hình", nút "Sửa vai trò".
 */
export function UserList({ users, currentUserId, loading, onEditRoles }: UserListProps) {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })

  if (loading && users.length === 0) {
    return (
      <Stack spacing={1}>
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} variant="rounded" height={isDesktop ? 56 : 120} />
        ))}
      </Stack>
    )
  }

  if (users.length === 0) {
    return (
      <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
        Không tìm thấy người dùng nào.
      </Typography>
    )
  }

  if (isDesktop) {
    return (
      <Card sx={{ opacity: loading ? 0.6 : 1, transition: 'opacity 150ms' }}>
        <Table size="small" aria-label="Danh sách người dùng CMS">
          <TableHead>
            <TableRow>
              <TableCell>Người dùng</TableCell>
              <TableCell>Vai trò</TableCell>
              <TableCell>Lần đầu</TableCell>
              <TableCell>Lần cuối</TableCell>
              <TableCell align="right" />
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((u) => {
              const isSelf = u.id === currentUserId
              return (
                <TableRow key={u.id} hover>
                  <TableCell sx={{ maxWidth: 360 }}>
                    <Typography component="div" variant="body2" sx={{ fontWeight: 600 }} noWrap>
                      {u.displayName || '—'}
                    </Typography>
                    <Typography component="div" variant="caption" color="text.secondary" noWrap>
                      {u.email}
                    </Typography>
                    <Badges user={u} isSelf={isSelf} />
                  </TableCell>
                  <TableCell>
                    <RoleChips roles={u.roles} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>
                    <Typography variant="body2" color="text.secondary" title={u.firstSeenAt}>
                      {formatRelativeTime(u.firstSeenAt) || '—'}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>
                    <Typography variant="body2" color="text.secondary" title={u.lastSeenAt}>
                      {formatRelativeTime(u.lastSeenAt) || '—'}
                    </Typography>
                  </TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    <Button size="small" variant="outlined" onClick={() => onEditRoles(u)}>
                      Sửa vai trò
                    </Button>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </Card>
    )
  }

  return (
    <Stack spacing={1.5} sx={{ opacity: loading ? 0.6 : 1, transition: 'opacity 150ms' }}>
      {users.map((u) => {
        const isSelf = u.id === currentUserId
        return (
          <Card key={u.id}>
            <CardContent sx={{ '&:last-child': { pb: 2 } }}>
              <Typography component="div" variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>
                {u.displayName || '—'}
              </Typography>
              <Typography component="div" variant="body2" color="text.secondary" sx={{ wordBreak: 'break-all' }}>
                {u.email}
              </Typography>
              <Badges user={u} isSelf={isSelf} />
              <Box sx={{ mt: 1 }}>
                <RoleChips roles={u.roles} />
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, mt: 1.5 }}>
                <Typography variant="caption" color="text.secondary" title={u.lastSeenAt}>
                  Lần cuối: {formatRelativeTime(u.lastSeenAt) || '—'}
                </Typography>
                <Button size="small" variant="outlined" onClick={() => onEditRoles(u)}>
                  Sửa vai trò
                </Button>
              </Box>
            </CardContent>
          </Card>
        )
      })}
    </Stack>
  )
}
