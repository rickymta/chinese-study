import { useMemo, useState } from 'react'
import { Alert, Box, Button, Chip } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import PauseCircleOutlinedIcon from '@mui/icons-material/PauseCircleOutlined'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import StyleOutlinedIcon from '@mui/icons-material/StyleOutlined'
import { useAuth } from '@af/auth'
import { useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import type { WordDetail } from '@/features/dictionary/types'
import { useAddCards, useSetCardSuspension } from '../hooks'

/** `dd/MM` theo múi giờ người học (claim `zoneinfo` của tài khoản); lỗi múi giờ ⇒ múi giờ trình duyệt. */
function formatDueDate(iso: string, timeZone: string | undefined): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  try {
    return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', timeZone }).format(d)
  } catch {
    return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit' }).format(d)
  }
}

/**
 * Nút/chip trạng thái SRS ở chi tiết từ (hợp đồng §5.3.2): chưa có thẻ ⇒ "Thêm vào ôn tập"; có thẻ ⇒ chip
 * "Chờ học" (`new`) / "Đang ôn · đến hạn dd/MM"; tạm dừng ⇒ chip "Đã tạm dừng" + nút "Tiếp tục ôn".
 * Lỗi ghi báo tại chỗ (không điều hướng).
 */
export function AddToSrsButton({ word }: { word: WordDetail }) {
  const { account } = useAuth()
  const toast = useToast()
  const addCards = useAddCards()
  const suspension = useSetCardSuspension()
  const [error, setError] = useState<string | null>(null)
  const srs = word.srs ?? null
  const timeZone = account?.timeZone || undefined
  const dueLabel = useMemo(() => (srs?.dueAt ? formatDueDate(srs.dueAt, timeZone) : ''), [srs?.dueAt, timeZone])

  const handleAdd = async () => {
    setError(null)
    try {
      const res = await addCards.mutateAsync([word.id])
      const created = res.cards?.find((c) => c.wordId === word.id)?.created ?? res.added > 0
      toast.success(created ? 'Đã thêm — thẻ sẽ xuất hiện trong lượt từ mới' : 'Từ này đã có trong ôn tập')
    } catch (err) {
      const parsed = parseApiError(err)
      setError(parsed.code === 'UNKNOWN_WORD' ? 'Từ này chưa có trong kho học liệu — không thêm được.' : parsed.message)
    }
  }

  const handleSuspend = async (suspended: boolean) => {
    if (!srs) return
    setError(null)
    try {
      await suspension.mutateAsync({ cardId: srs.cardId, suspended, wordId: word.id })
      toast.success(suspended ? 'Đã tạm dừng thẻ này' : 'Đã tiếp tục ôn thẻ này')
    } catch (err) {
      setError(parseApiError(err).message)
    }
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        {srs === null ? (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => void handleAdd()}
            loading={addCards.isPending}
            sx={{ minHeight: 44 }}
          >
            Thêm vào ôn tập
          </Button>
        ) : srs.isSuspended ? (
          <>
            <Chip icon={<PauseCircleOutlinedIcon />} color="default" label="Đã tạm dừng" />
            <Button
              size="small"
              variant="outlined"
              startIcon={<PlayArrowIcon />}
              onClick={() => void handleSuspend(false)}
              loading={suspension.isPending}
            >
              Tiếp tục ôn
            </Button>
          </>
        ) : (
          <>
            <Chip
              icon={<StyleOutlinedIcon />}
              color="primary"
              variant="outlined"
              label={srs.state === 'new' ? 'Chờ học' : dueLabel ? `Đang ôn · đến hạn ${dueLabel}` : 'Đang ôn'}
            />
            <Button
              size="small"
              color="inherit"
              startIcon={<PauseCircleOutlinedIcon />}
              onClick={() => void handleSuspend(true)}
              loading={suspension.isPending}
            >
              Tạm dừng
            </Button>
          </>
        )}
      </Box>
      {error && (
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
    </Box>
  )
}
