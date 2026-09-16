import type { ReactNode } from 'react'
import { Box, Card, CardContent, Chip, Stack, Typography } from '@mui/material'
import { useMe } from '../hooks'
import { labelOfPermission, labelOfRole } from '../permissions'

/** Định dạng mốc UTC theo múi giờ của người học; chuỗi hỏng/múi giờ lạ ⇒ trả nguyên chuỗi. */
function formatInZone(iso: string, timeZone: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  try {
    return d.toLocaleString('vi-VN', { timeZone, dateStyle: 'medium', timeStyle: 'short' })
  } catch {
    return d.toLocaleString('vi-VN')
  }
}

/** Một dòng nhãn–giá trị, xếp dọc ở 375px và ngang ở sm+. */
function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: { xs: 0.25, sm: 2 } }}>
      <Typography variant="body2" color="text.secondary" sx={{ minWidth: 120, flexShrink: 0 }}>
        {label}
      </Typography>
      <Box sx={{ minWidth: 0, flex: 1 }}>{children}</Box>
    </Box>
  )
}

/**
 * Thẻ "Tài khoản của bạn ở dịch vụ tiếng Trung" — hiển thị nguyên văn `GET /chinese/api/me` (F3) để người dùng
 * (và người kiểm thử) thấy ngay vai trò/quyền cục bộ: email bootstrap ⇒ `admin`, tài khoản khác ⇒ `learner`.
 */
export function MeCard() {
  const me = useMe()
  if (!me) return null

  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
          Tài khoản của bạn ở dịch vụ tiếng Trung
        </Typography>
        <Stack spacing={1.25}>
          <Row label="Tên hiển thị">
            <Typography variant="body2" noWrap>
              {me.displayName || '—'}
            </Typography>
          </Row>
          <Row label="Email">
            <Typography variant="body2" noWrap>
              {me.email}
            </Typography>
          </Row>
          <Row label="Múi giờ">
            <Typography variant="body2">{me.timeZone}</Typography>
          </Row>
          <Row label="Vai trò">
            <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
              {me.roles.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  Chưa có vai trò
                </Typography>
              ) : (
                me.roles.map((r) => <Chip key={r} size="small" color="primary" variant="outlined" label={labelOfRole(r)} />)
              )}
            </Box>
          </Row>
          <Row label="Quyền">
            <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
              {me.permissions.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  Chưa có quyền
                </Typography>
              ) : (
                me.permissions.map((p) => <Chip key={p} size="small" label={labelOfPermission(p)} />)
              )}
            </Box>
          </Row>
          {me.firstSeenAt && (
            <Row label="Vào lần đầu">
              <Typography variant="body2">{formatInZone(me.firstSeenAt, me.timeZone)}</Typography>
            </Row>
          )}
        </Stack>
      </CardContent>
    </Card>
  )
}
