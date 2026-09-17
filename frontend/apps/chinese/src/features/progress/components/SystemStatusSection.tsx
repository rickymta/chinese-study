import { Accordion, AccordionDetails, AccordionSummary, Alert, AlertTitle, Box, Typography } from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { ServiceStatusChip } from '@/features/system/components/ServiceStatusChip'
import { useSystemInfo } from '@/features/system/hooks'

/** Nội dung bên trong — chỉ mount khi mở (query trạng thái tự hỏi lại mỗi 30 giây, không chạy khi thu gọn). */
function StatusBody() {
  const chinese = useSystemInfo('chinese')
  const identity = useSystemInfo('identity')
  const failed = [chinese.error && 'chinese-backend', identity.error && 'identity-service'].filter(Boolean)
  return (
    <>
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        <ServiceStatusChip service="chinese" label="Tiếng Trung" />
        <ServiceStatusChip service="identity" label="Tài khoản" />
      </Box>
      {failed.length > 0 && (
        <Alert severity="warning" sx={{ mt: 1.5 }}>
          <AlertTitle>Không tới được: {failed.join(', ')}</AlertTitle>
          Ở máy dev cần chạy đủ 3 tiến trình theo thứ tự <strong>identity-service → chinese-backend → gateway</strong> (cổng 5281,
          5282, 5280) rồi app này (3280). Chip tự kiểm tra lại mỗi 30 giây, hoặc bấm vào chip để thử ngay.
        </Alert>
      )}
    </>
  )
}

/**
 * Khối "Trạng thái hệ thống" thu gọn cuối trang chủ (F11 §5.3.4) — thay thẻ trạng thái của HomePage F1.
 * Trang chủ CHỈ render khối này khi có `users.manage`; `unmountOnExit` để không gọi `/system/info` khi chưa mở.
 */
export function SystemStatusSection() {
  return (
    <Accordion disableGutters variant="outlined" slotProps={{ transition: { unmountOnExit: true } }}>
      <AccordionSummary expandIcon={<ExpandMoreIcon />} aria-controls="system-status-content" id="system-status-header">
        <Typography variant="subtitle2" color="text.secondary">
          Trạng thái hệ thống (qua gateway)
        </Typography>
      </AccordionSummary>
      <AccordionDetails id="system-status-content">
        <StatusBody />
      </AccordionDetails>
    </Accordion>
  )
}
