import { Alert, AlertTitle, Button } from '@mui/material'
import { isApiError } from '@af/api'

export interface QueryErrorAlertProps {
  error: unknown
  onRetry?: () => void
}

/**
 * Alert lỗi cho các màn từ điển: 503 `CONTENT_UNAVAILABLE` (học liệu chưa nạp) là trạng thái của server, không
 * phải lỗi thoáng qua ⇒ cảnh báo, không nút thử lại; 400 `VALIDATION` hiện thông điệp của `details` (vd `q` quá
 * 64 ký tự); lỗi khác ⇒ thông điệp tiếng Việt của `ApiError` + nút "Thử lại".
 */
export function QueryErrorAlert({ error, onRetry }: QueryErrorAlertProps) {
  const apiErr = isApiError(error) ? error : null
  if (apiErr?.status === 503) {
    return (
      <Alert severity="warning">
        <AlertTitle>Học liệu chưa sẵn sàng</AlertTitle>
        Từ điển chưa được nạp dữ liệu. Vui lòng thử lại sau ít phút hoặc báo quản trị viên.
      </Alert>
    )
  }
  if (apiErr?.status === 400) {
    // `details` theo §6.0: `{ q: ["…"], pageSize: ["…"] }` (mảng chuỗi mỗi trường); phòng cả dạng chuỗi đơn.
    let first: string | undefined
    for (const v of Object.values(apiErr.details ?? {})) {
      if (typeof v === 'string' && v) first = v
      else if (Array.isArray(v) && typeof v[0] === 'string' && v[0]) first = v[0]
      if (first) break
    }
    return <Alert severity="error">{first ?? apiErr.message}</Alert>
  }
  const message = apiErr?.message ?? (error instanceof Error ? error.message : 'Đã xảy ra lỗi không xác định.')
  return (
    <Alert
      severity="error"
      action={
        onRetry && (
          <Button color="inherit" size="small" onClick={onRetry}>
            Thử lại
          </Button>
        )
      }
    >
      {message}
    </Alert>
  )
}
