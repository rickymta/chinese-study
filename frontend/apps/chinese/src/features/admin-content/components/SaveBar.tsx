import { Box, Button, Typography } from '@mui/material'
import { StickyActionBar } from '@af/ui'

export interface SaveBarProps {
  dirty: boolean
  saving: boolean
  disabled?: boolean
  onSave: () => void
  onRevert: () => void
  /** Lời giải thích khi khoá (vd bài lưu trữ). */
  disabledReason?: string
}

/** Thanh "Lưu / Hoàn tác" dính đáy cho mỗi tab của trình soạn — ở 375px không bị bottom nav che. */
export function SaveBar({ dirty, saving, disabled = false, onSave, onRevert, disabledReason }: SaveBarProps) {
  return (
    <StickyActionBar>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Typography variant="caption" color={dirty ? 'warning.main' : 'text.secondary'} sx={{ flex: 1, minWidth: 120 }}>
          {disabled ? disabledReason : dirty ? 'Có thay đổi chưa lưu.' : 'Chưa có thay đổi.'}
        </Typography>
        <Button color="inherit" onClick={onRevert} disabled={!dirty || saving || disabled}>
          Hoàn tác
        </Button>
        <Button variant="contained" onClick={onSave} loading={saving} disabled={!dirty || disabled} sx={{ minWidth: 96 }}>
          Lưu
        </Button>
      </Box>
    </StickyActionBar>
  )
}
