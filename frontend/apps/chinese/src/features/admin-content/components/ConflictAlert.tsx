import { Alert, AlertTitle, Button, Stack } from '@mui/material'

export interface ConflictAlertProps {
  open: boolean
  /** Tải bản mới nhất, GIỮ bản nháp đang sửa (người dùng bấm Lưu lại với version mới). */
  onReloadKeep: () => void
  /** Bỏ thay đổi cục bộ, lấy bản trên server (đã qua `useConfirm` ở bên gọi). */
  onDiscard: () => void
  busy?: boolean
}

/**
 * 409 `CONCURRENCY_CONFLICT` (R-CA3): bài/từ đã bị sửa ở nơi khác. KHÔNG tự ghi đè — cho hai lối: tải lại giữ bản nháp
 * (lưu lại sau khi xem) hoặc bỏ thay đổi.
 */
export function ConflictAlert({ open, onReloadKeep, onDiscard, busy = false }: ConflictAlertProps) {
  if (!open) return null
  return (
    <Alert severity="error">
      <AlertTitle>Đã bị sửa ở nơi khác</AlertTitle>
      Bản trên máy chủ mới hơn bản bạn đang xem nên chưa lưu được. Tải lại để lấy phiên bản mới — bản nháp của bạn được giữ;
      <strong> lưu lại sẽ ghi đè thay đổi của người kia — hãy kiểm tra trước</strong>. Hoặc bỏ thay đổi để lấy bản trên máy chủ.
      <Stack direction="row" sx={{ gap: 1, mt: 1, flexWrap: 'wrap' }}>
        <Button size="small" variant="contained" color="error" onClick={onReloadKeep} loading={busy}>
          Tải lại, giữ bản nháp
        </Button>
        <Button size="small" color="inherit" onClick={onDiscard} disabled={busy}>
          Bỏ thay đổi
        </Button>
      </Stack>
    </Alert>
  )
}
