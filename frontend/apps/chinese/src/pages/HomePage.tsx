import { Alert, AlertTitle, Box, Card, CardContent, Stack, Typography } from '@mui/material'
import { LangText, PageContainer } from '@af/ui'
import { useSystemInfo } from '@/features/system/hooks'
import { ServiceStatusChip } from '@/features/system/components/ServiceStatusChip'
import { MeCard } from '@/features/auth/components/MeCard'

/**
 * Trang chủ F1: lời chào + 2 chip trạng thái service qua gateway; F3 thêm thẻ vai trò/quyền từ `/api/me`.
 * F11 thay bằng tổng quan tiến độ/streak.
 */
export function HomePage() {
  const chinese = useSystemInfo('chinese')
  const identity = useSystemInfo('identity')
  const failed = [chinese.error && 'chinese-backend', identity.error && 'identity-service'].filter(Boolean)

  return (
    <PageContainer title="Trang chủ">
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Typography variant="h4" component="p" sx={{ fontWeight: 700, mb: 0.5 }}>
              <LangText lang="zh-CN">你好</LangText>
              <Typography component="span" variant="h6" color="text.secondary" sx={{ ml: 1.5 }}>
                nǐ hǎo
              </Typography>
            </Typography>
            <Typography color="text.secondary">
              Chào mừng đến AntFarm — nền tảng tự học tiếng Trung từ số 0. Các mục học (pinyin, tra từ, ôn tập
              thẻ, luyện viết, bài học) sẽ lần lượt xuất hiện ở menu.
            </Typography>
          </CardContent>
        </Card>

        <MeCard />

        <Card>
          <CardContent>
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
              Trạng thái dịch vụ (qua gateway)
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              <ServiceStatusChip service="chinese" label="Tiếng Trung" />
              <ServiceStatusChip service="identity" label="Tài khoản" />
            </Box>
          </CardContent>
        </Card>

        {failed.length > 0 && (
          <Alert severity="warning">
            <AlertTitle>Không tới được: {failed.join(', ')}</AlertTitle>
            Ở máy dev cần chạy đủ 3 tiến trình theo thứ tự <strong>identity-service → chinese-backend → gateway</strong>{' '}
            (cổng 5281, 5282, 5280) rồi app này (3280). Chip tự kiểm tra lại mỗi 30 giây, hoặc bấm vào chip để thử ngay.
          </Alert>
        )}
      </Stack>
    </PageContainer>
  )
}
