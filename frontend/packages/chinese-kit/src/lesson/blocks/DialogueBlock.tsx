import { useMemo } from 'react'
import { Box, Button, Card, CardContent, Tooltip, Typography } from '@mui/material'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import StopIcon from '@mui/icons-material/Stop'
import { NO_VOICE_TOOLTIP } from '../../speech/SpeakButton'
import type { DialogueBlockPayload } from '../types'
import { ZhLineRow } from '../ZhLineRow'
import { useSpeakQueue } from '../useSpeakQueue'

/** Hai màu luân phiên cho tên người nói (theo thứ tự xuất hiện) — dễ theo dõi ai đang nói. */
const SPEAKER_COLORS = ['primary.main', 'secondary.main']

/**
 * Khối hội thoại: tiêu đề + nút "Nghe cả đoạn" (đọc lần lượt, bấm lại để dừng) + từng dòng (`ZhLineRow`).
 * Dòng đang đọc được tô nền. Dừng khi rời trang (`useSpeakQueue` cleanup).
 */
export function DialogueBlock({ payload }: { payload: DialogueBlockPayload }) {
  const { playing, activeIndex, playAll, stop, canSpeak } = useSpeakQueue()

  // Màu theo người nói: ánh xạ tên → màu theo thứ tự xuất hiện lần đầu.
  const colorBySpeaker = useMemo(() => {
    const map = new Map<string, string>()
    for (const l of payload.lines) {
      if (l.speaker && !map.has(l.speaker)) map.set(l.speaker, SPEAKER_COLORS[map.size % SPEAKER_COLORS.length]!)
    }
    return map
  }, [payload.lines])

  const handlePlayAll = () => {
    if (playing) stop()
    else playAll(payload.lines.map((l) => l.hanzi))
  }

  const playAllButton = (
    <Button
      size="small"
      variant={playing ? 'contained' : 'outlined'}
      color={playing ? 'warning' : 'primary'}
      startIcon={playing ? <StopIcon /> : <PlayArrowIcon />}
      onClick={handlePlayAll}
      disabled={!canSpeak || payload.lines.length === 0}
      sx={{ flexShrink: 0 }}
    >
      {playing ? 'Dừng' : 'Nghe cả đoạn'}
    </Button>
  )

  return (
    <Card variant="outlined">
      <CardContent sx={{ px: { xs: 1, sm: 2 } }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 1, px: 1 }}>
          <Typography component="h3" variant="subtitle1" sx={{ fontWeight: 700 }}>
            {payload.title || 'Hội thoại'}
          </Typography>
          {canSpeak ? (
            playAllButton
          ) : (
            <Tooltip title={NO_VOICE_TOOLTIP}>
              <span style={{ display: 'inline-flex' }}>{playAllButton}</span>
            </Tooltip>
          )}
        </Box>
        {payload.lines.map((line, i) => (
          <ZhLineRow
            key={i}
            line={line}
            speakerColor={line.speaker ? colorBySpeaker.get(line.speaker) : undefined}
            hanziSize={24}
            active={activeIndex === i}
          />
        ))}
      </CardContent>
    </Card>
  )
}
