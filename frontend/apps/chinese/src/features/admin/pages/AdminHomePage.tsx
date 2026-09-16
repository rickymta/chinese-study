import { Alert, AlertTitle, Box, Button, Card, CardContent, Chip, CircularProgress, Stack, Typography } from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import { useAuth } from '@af/auth'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { MeCard } from '@/features/auth/components/MeCard'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useAdminPing } from '../hooks'

/**
 * `/quan-tri` (F3, cần `users.manage`): trang quản trị tối thiểu — gọi `GET /chinese/api/admin/ping` để chứng minh
 * phân quyền cục bộ hoạt động đầu-cuối. F4 thêm "Người dùng", F10 thêm "Bài học"/"Từ vựng" thành các thẻ điều hướng.
 */
export function AdminHomePage() {
  const { can } = useAuth()
  const ping = useAdminPing()

  return (
    <PageContainer title="Quản trị">
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
              Kết nối vùng quản trị
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
              Gọi <code>GET /chinese/api/admin/ping</code> — chỉ tài khoản có quyền “Quản lý người dùng” mới được trả lời.
              Bị 403 nghĩa là quyền vừa bị thu hồi.
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              {ping.isPending ? (
                <Chip icon={<CircularProgress size={14} color="inherit" />} label="Đang kiểm tra…" variant="outlined" />
              ) : ping.error ? (
                <Chip icon={<ErrorOutlineOutlinedIcon />} label={`Lỗi: ${parseApiError(ping.error).message}`} color="error" variant="outlined" />
              ) : ping.data?.ok ? (
                <Chip icon={<CheckCircleOutlinedIcon />} label="Phân quyền hoạt động (ok: true)" color="success" variant="outlined" />
              ) : (
                <Chip icon={<ErrorOutlineOutlinedIcon />} label="Phản hồi không đúng dạng { ok: true }" color="warning" variant="outlined" />
              )}
              <Button size="small" variant="outlined" onClick={() => void ping.refetch()} disabled={ping.isFetching}>
                Kiểm tra lại
              </Button>
            </Box>
          </CardContent>
        </Card>

        <MeCard />

        {/* Khối chức năng soạn nội dung (F10) ẩn theo quyền — báo rõ để người dùng không tưởng thiếu tính năng. */}
        {!can(PERMISSIONS.CONTENT_MANAGE) && (
          <Alert severity="info">
            Bạn chưa có quyền “Soạn nội dung” nên các mục quản trị bài học/từ vựng sẽ không hiện ở đây — chế độ chỉ xem.
          </Alert>
        )}

        <Alert severity="info" variant="outlined">
          <AlertTitle>Sắp có</AlertTitle>
          Quản lý người dùng &amp; gán vai trò (F4) · Soạn bài học, duyệt nghĩa từ vựng (F10).
        </Alert>
      </Stack>
    </PageContainer>
  )
}
