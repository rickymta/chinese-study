import { useMemo } from 'react'
import { Alert, AlertTitle, Box, Button, Card, CardContent, Chip, Divider, List, ListItemButton, ListItemIcon, ListItemText, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import { useAuth } from '@af/auth'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useAdminMe } from '@/auth/hooks'
import { CMS_SERVICE, labelOfCmsRole, labelOfPermission, perm } from '@/auth/permissions'
import { buildNavGroups, canAccessGroup } from '@/layout/navigation'
import { LANGUAGE_MODULES, findLanguageModule } from '@/modules/registry'
import type { ServiceState } from '@/auth/types'

/** Tên hiển thị của service theo mã (`cms` ⇒ "CMS", ngôn ngữ ⇒ nhãn trong registry). */
const serviceLabel = (code: string): string => (code === CMS_SERVICE ? 'CMS (website & hệ thống)' : (findLanguageModule(code)?.label ?? code))

/** Thứ tự hiển thị: cms trước, rồi các ngôn ngữ theo registry. */
const SERVICE_ORDER = [CMS_SERVICE, ...LANGUAGE_MODULES.map((m) => m.code)]

function ServiceRow({ code, state }: { code: string; state: ServiceState }) {
  if (state.status === 'unavailable') {
    return (
      <Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {serviceLabel(code)}
          </Typography>
          <Chip size="small" color="error" variant="outlined" icon={<ErrorOutlineOutlinedIcon />} label="Không phản hồi" />
        </Box>
        <Typography variant="body2" color="text.secondary">
          {parseApiError(state.error).message}
        </Typography>
      </Box>
    )
  }
  const module = findLanguageModule(code)
  // Với service ngôn ngữ: quyền ngoài `adminPermissions` (vd `study.use`) hiện mờ — có ở đó nhưng không mở gì trong admin.
  const isAdminPerm = (p: string) => !module || module.adminPermissions.includes(p)
  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
          {serviceLabel(code)}
        </Typography>
        <Chip size="small" color="success" variant="outlined" icon={<CheckCircleOutlinedIcon />} label="Đã kết nối" />
      </Box>
      <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 0.75 }}>
        {state.me.roles.length === 0 && state.me.permissions.length === 0 && (
          <Typography variant="body2" color="text.disabled">
            Chưa có vai trò nào ở dịch vụ này.
          </Typography>
        )}
        {state.me.roles.map((r) => (
          <Chip key={`r-${r}`} size="small" color="primary" label={code === CMS_SERVICE ? labelOfCmsRole(r) : r} />
        ))}
        {state.me.permissions.map((p) => (
          <Chip
            key={`p-${p}`}
            size="small"
            variant="outlined"
            label={labelOfPermission(p)}
            title={perm(code, p)}
            sx={{ opacity: isAdminPerm(p) ? 1 : 0.5 }}
          />
        ))}
      </Box>
    </Box>
  )
}

/**
 * `/` — Tổng quan (hợp đồng W2 §5.3.1): lời chào, cảnh báo service không phản hồi, "bạn có quyền gì ở đâu", và với
 * mỗi khu vực quản trị: lối vào (đã có màn) / "sắp có" (có quyền, màn chưa làm) / "chưa có quyền — liên hệ quản
 * trị viên" (mục menu bị ẩn phải có lời giải thích). Không bọc `RequirePermission`: mọi tài khoản qua được
 * `RequireAuth` đều xem được trang này.
 */
export function DashboardPage() {
  const { account, permissions, reloadMe, meLoading } = useAuth()
  const me = useAdminMe()
  const groups = useMemo(() => buildNavGroups(permissions).filter((g) => g.key !== 'tong-quan'), [permissions])

  const services = me?.services ?? {}
  const orderedCodes = [...SERVICE_ORDER.filter((c) => c in services), ...Object.keys(services).filter((c) => !SERVICE_ORDER.includes(c))]
  const unavailable = orderedCodes.filter((c) => services[c]?.status === 'unavailable')

  return (
    <PageContainer title="Tổng quan">
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              Xin chào, {account?.displayName || account?.email || 'bạn'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Đây là trang quản trị chung của AntFarm: website giới thiệu, hộp thư, tài khoản nền tảng và nội dung của
              từng ngôn ngữ. Mục bạn thấy trên menu phụ thuộc vào vai trò được gán ở từng dịch vụ.
            </Typography>
          </CardContent>
        </Card>

        {unavailable.map((code) => (
          <Alert
            key={code}
            severity="warning"
            action={
              <Button color="inherit" size="small" onClick={() => void reloadMe()} disabled={meLoading}>
                {meLoading ? 'Đang thử…' : 'Thử lại'}
              </Button>
            }
          >
            <AlertTitle>{serviceLabel(code)}: không phản hồi</AlertTitle>
            Các mục quản trị của dịch vụ này tạm ẩn cho tới khi kết nối lại. Chi tiết:{' '}
            {parseApiError((services[code] as Extract<ServiceState, { status: 'unavailable' }>).error).message}
          </Alert>
        ))}

        {permissions.size === 0 && (
          <Alert severity="info">
            <AlertTitle>Chưa thấy quyền quản trị nào</AlertTitle>
            Có dịch vụ chưa trả lời nên chưa biết đầy đủ quyền của bạn. Khi mọi dịch vụ hoạt động mà vẫn không có
            quyền, bạn sẽ được đưa tới trang 403 — liên hệ quản trị viên để được gán vai trò.
          </Alert>
        )}

        <Card>
          <CardContent>
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
              Quyền của bạn ở từng dịch vụ
            </Typography>
            <Stack spacing={2} divider={<Divider flexItem />}>
              {orderedCodes.map((code) => (
                <ServiceRow key={code} code={code} state={services[code]!} />
              ))}
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
              Khu vực quản trị
            </Typography>
            <Stack spacing={1.5} divider={<Divider flexItem />}>
              {groups.map((g) => {
                const allowed = canAccessGroup(g, permissions)
                const visibleItems = g.items.filter((it) => !it.requiredPermission || permissions.has(it.requiredPermission))
                return (
                  <Box key={g.key}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                      <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                        {g.label}
                      </Typography>
                      {!allowed ? (
                        <Chip size="small" variant="outlined" icon={<LockOutlinedIcon />} label="Chưa có quyền" />
                      ) : visibleItems.length === 0 ? (
                        <Chip size="small" variant="outlined" color="warning" icon={<ScheduleOutlinedIcon />} label="Sắp có" />
                      ) : null}
                    </Box>
                    {!allowed ? (
                      <Typography variant="body2" color="text.secondary">
                        Bạn chưa có quyền {g.permissions.map(labelOfPermission).join(' / ')} — liên hệ quản trị viên nếu cần.
                      </Typography>
                    ) : visibleItems.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">
                        {g.planned ?? 'Màn hình sẽ được bổ sung ở đợt sau.'}
                      </Typography>
                    ) : (
                      <List dense disablePadding sx={{ mt: 0.5 }}>
                        {visibleItems.map((it) => (
                          <ListItemButton key={it.to} component={RouterLink} to={it.to} sx={{ borderRadius: 2 }}>
                            <ListItemIcon sx={{ minWidth: 36 }}>{it.icon}</ListItemIcon>
                            <ListItemText primary={it.label} />
                          </ListItemButton>
                        ))}
                        {g.planned && (
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', px: 2, pt: 0.5 }}>
                            Sắp có: {g.planned}
                          </Typography>
                        )}
                      </List>
                    )}
                  </Box>
                )
              })}
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </PageContainer>
  )
}
