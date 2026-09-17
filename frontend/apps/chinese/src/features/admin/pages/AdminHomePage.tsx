import { Alert, AlertTitle, Box, Button, Card, CardActionArea, CardContent, Chip, CircularProgress, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined'
import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined'
import SpellcheckOutlinedIcon from '@mui/icons-material/SpellcheckOutlined'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
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
        {/* F4: lối vào "Người dùng" — ở điện thoại mục này KHÔNG có trên bottom nav (tránh 2 ô quản trị), vào từ đây. */}
        <Card>
          <CardActionArea component={RouterLink} to="/quan-tri/nguoi-dung">
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <ManageAccountsOutlinedIcon color="primary" sx={{ fontSize: 32 }} />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                  Người dùng &amp; vai trò
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Tìm người đã vào dịch vụ tiếng Trung, gán hoặc gỡ vai trò (admin, learner).
                </Typography>
              </Box>
              <ChevronRightIcon color="action" />
            </CardContent>
          </CardActionArea>
        </Card>

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

        {/* F10: lối vào soạn bài / duyệt nghĩa — ẩn theo quyền `content.manage`, kèm dải giải thích khi thiếu quyền. */}
        {can(PERMISSIONS.CONTENT_MANAGE) ? (
          <>
            <Card>
              <CardActionArea component={RouterLink} to="/quan-tri/bai-hoc">
                <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                  <AutoStoriesOutlinedIcon color="primary" sx={{ fontSize: 32 }} />
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                      Bài học
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Soạn, sửa, xuất bản bài học và quiz; duyệt bài có sẵn.
                    </Typography>
                  </Box>
                  <ChevronRightIcon color="action" />
                </CardContent>
              </CardActionArea>
            </Card>
            <Card>
              <CardActionArea component={RouterLink} to="/quan-tri/tu-vung">
                <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                  <SpellcheckOutlinedIcon color="primary" sx={{ fontSize: 32 }} />
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                      Từ vựng
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Duyệt nghĩa tiếng Việt dịch máy và Hán Việt theo thứ tự lộ trình học.
                    </Typography>
                  </Box>
                  <ChevronRightIcon color="action" />
                </CardContent>
              </CardActionArea>
            </Card>
          </>
        ) : (
          <Alert severity="info">
            <AlertTitle>Chế độ chỉ xem</AlertTitle>
            Bạn chưa có quyền “Soạn nội dung” nên các mục quản trị bài học/từ vựng không hiện ở đây.
          </Alert>
        )}
      </Stack>
    </PageContainer>
  )
}
