import { Chip, CircularProgress, Tooltip } from '@mui/material'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutlineOutlined'
import { useSystemInfo } from '../hooks'
import type { SystemService } from '../types'

interface Props {
  service: SystemService
  /** Nhãn hiển thị trước dấu hai chấm: "Tiếng Trung" / "Tài khoản". */
  label: string
}

/** Chip trạng thái một service: đang kiểm tra → đang chạy (kèm version/môi trường) → lỗi (kèm thông điệp). */
export function ServiceStatusChip({ service, label }: Props) {
  const { data, error, isPending, refetch, isFetching } = useSystemInfo(service)

  if (isPending) {
    return <Chip icon={<CircularProgress size={14} color="inherit" />} label={`${label}: đang kiểm tra…`} variant="outlined" />
  }

  if (error || !data) {
    return (
      <Tooltip title={`${error?.message ?? 'Không rõ lỗi'} — bấm để thử lại`}>
        <Chip
          icon={<ErrorOutlineIcon />}
          label={`${label}: lỗi`}
          color="error"
          variant="outlined"
          onClick={() => void refetch()}
          disabled={isFetching}
        />
      </Tooltip>
    )
  }

  return (
    <Tooltip title={`${data.service} v${data.version} · ${data.environment}`}>
      <Chip icon={<CheckCircleOutlineIcon />} label={`${label}: đang chạy`} color="success" variant="outlined" />
    </Tooltip>
  )
}
