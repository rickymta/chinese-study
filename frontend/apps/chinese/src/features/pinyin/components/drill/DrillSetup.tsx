import { Alert, Box, Button, Card, CardContent, Stack, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import type { DrillMode, DrillTone } from '../../types'

export interface DrillSetupProps {
  mode: DrillMode
  onModeChange: (mode: DrillMode) => void
  /** `recommendedFocus` từ thống kê (rỗng ⇒ ngẫu nhiên đều). */
  focus: readonly DrillTone[]
  onStart: () => void
  /** Chưa tải xong bảng / học liệu lỗi ⇒ không bắt đầu được. */
  disabled?: boolean
  disabledReason?: string
  /** Không có giọng ⇒ khoá “Bắt đầu” kèm lời giải thích (không nghe được thì không luyện được). */
  canSpeak: boolean
}

/** Màn thiết lập bài luyện: chọn chế độ (URL `?che-do=mot|cap`), dòng thanh tập trung, nút "Bắt đầu (20 câu)". */
export function DrillSetup({ mode, onModeChange, focus, onStart, disabled, disabledReason, canSpeak }: DrillSetupProps) {
  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              Luyện nghe – chọn thanh
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Nghe một chữ (hoặc hai chữ liền nhau) rồi chọn thanh. Mỗi bài 20 câu, khoảng 3–5 phút.
            </Typography>
          </Box>

          <ToggleButtonGroup
            exclusive
            value={mode}
            onChange={(_e, v: DrillMode | null) => v && onModeChange(v)}
            fullWidth
            color="primary"
            aria-label="Chế độ luyện"
          >
            <ToggleButton value="listen_tone" sx={{ minHeight: 48 }}>
              Một âm tiết
            </ToggleButton>
            <ToggleButton value="tone_pair" sx={{ minHeight: 48 }}>
              Cặp thanh
            </ToggleButton>
          </ToggleButtonGroup>

          <Typography variant="body2" color="text.secondary">
            {mode === 'listen_tone'
              ? 'Mỗi câu phát một chữ, bạn chọn thanh 1–4. Phím tắt: 1–4 chọn, Space nghe lại, Enter sang câu kế.'
              : 'Mỗi câu phát hai chữ liền nhau, bạn chọn thanh cho từng chữ (gõ hai số liên tiếp). Không có cặp 3-3.'}
          </Typography>

          {focus.length > 0 && (
            <Alert severity="info">
              Bài này sẽ tập trung vào thanh {focus.join(', ')} (một nửa số câu) — vì đây là thanh bạn hay nghe nhầm.
            </Alert>
          )}

          {!canSpeak && (
            <Alert severity="warning">
              Máy chưa có giọng tiếng Trung nên không phát được câu hỏi — nút “Bắt đầu” tạm khoá. Cài giọng theo hướng dẫn ở
              đầu trang rồi mở lại trình duyệt.
            </Alert>
          )}
          {disabled && disabledReason && <Alert severity="warning">{disabledReason}</Alert>}

          <Button
            variant="contained"
            size="large"
            startIcon={<PlayArrowIcon />}
            onClick={onStart}
            disabled={disabled || !canSpeak}
            sx={{ minHeight: 52, alignSelf: { xs: 'stretch', sm: 'flex-start' } }}
          >
            Bắt đầu (20 câu)
          </Button>
        </Stack>
      </CardContent>
    </Card>
  )
}
