import { Alert, Box, Button, FormControl, InputLabel, MenuItem, Select, Slider, Stack, Typography } from '@mui/material'
import { AppDialog } from '@af/ui'
import { useChineseSpeech } from '@/components/speech/ChineseSpeech'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { TTS_RATE_DEFAULT, TTS_RATE_MAX, TTS_RATE_MIN } from '@/components/speech/useTtsRate'

const RATE_MARKS = [
  { value: 0.5, label: 'Chậm' },
  { value: 0.8, label: '0,8' },
  { value: 1, label: 'Thường' },
  { value: 1.2, label: 'Nhanh' },
]

/** Hộp thoại "Cài đặt giọng đọc": chọn giọng zh + tốc độ (localStorage), nút nghe thử. Chỉ đọc-cài đặt nhẹ nên `closeOnBackdrop`. */
export function VoiceSettings({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { status, voices, voice, setVoiceUri, rate, setRate } = useChineseSpeech()

  return (
    <AppDialog
      open={open}
      onClose={onClose}
      title="Cài đặt giọng đọc"
      // Không có thao tác ghi máy chủ, đổi là áp dụng ngay ⇒ cho bấm ra ngoài để đóng nhanh.
      closeOnBackdrop
      actions={
        <>
          <Button onClick={() => setRate(TTS_RATE_DEFAULT)} color="inherit">
            Mặc định
          </Button>
          <Button variant="contained" onClick={onClose}>
            Xong
          </Button>
        </>
      }
    >
      <Stack spacing={3} sx={{ pt: 1 }}>
        {status === 'ready' ? (
          <FormControl fullWidth>
            <InputLabel id="voice-select-label">Giọng tiếng Trung</InputLabel>
            <Select
              labelId="voice-select-label"
              label="Giọng tiếng Trung"
              value={voice?.voiceURI ?? ''}
              onChange={(e) => setVoiceUri(String(e.target.value))}
            >
              {voices.map((v) => (
                <MenuItem key={v.voiceURI} value={v.voiceURI}>
                  {v.name} · {v.lang}
                  {v.localService ? '' : ' (trực tuyến)'}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        ) : (
          <Alert severity="info">
            {status === 'loading' ? 'Đang tìm giọng tiếng Trung…' : 'Chưa có giọng tiếng Trung — xem hướng dẫn cài ở đầu trang.'}
          </Alert>
        )}

        <Box>
          <Typography gutterBottom>Tốc độ đọc: {rate.toFixed(1).replace('.', ',')}</Typography>
          <Slider
            value={rate}
            min={TTS_RATE_MIN}
            max={TTS_RATE_MAX}
            step={0.1}
            marks={RATE_MARKS}
            valueLabelDisplay="auto"
            onChange={(_e, v) => setRate(Array.isArray(v) ? v[0]! : v)}
            aria-label="Tốc độ đọc"
          />
          <Typography variant="caption" color="text.secondary">
            Mới học nên để 0,7–0,8 để nghe rõ đường nét thanh; quen rồi tăng dần lên 1,0.
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <SpeakButton text="你好，我是学生。" variant="button" label="Nghe thử" />
          <Typography variant="body2" color="text.secondary">
            nǐ hǎo, wǒ shì xuéshēng
          </Typography>
        </Box>
      </Stack>
    </AppDialog>
  )
}
