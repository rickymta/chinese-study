import { Box, Button, Chip, Divider, Stack, Typography } from '@mui/material'
import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import { Hanzi } from '@/components/Hanzi'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import { MeaningStatusChip } from '@/features/dictionary/components/MeaningStatusChip'
import type { SrsCardState, SrsQueueCard } from '../types'

const STATE_CHIP: Partial<Record<SrsCardState, { label: string; color: 'primary' | 'warning' | 'default' }>> = {
  new: { label: 'Mới', color: 'primary' },
  learning: { label: 'Đang học', color: 'default' },
  relearning: { label: 'Học lại', color: 'warning' },
}

export interface FlashcardProps {
  card: SrsQueueCard
  flipped: boolean
  /** Mở chi tiết từ (AppDrawer) — không rời phiên. */
  onOpenDetail: (wordId: string) => void
}

/**
 * Thẻ ôn: mặt trước = chữ Hán lớn + loa + chip trạng thái; lật ⇒ mặt sau hiện DƯỚI mặt trước (pinyin dấu, Hán Việt,
 * tối đa 3 nghĩa Việt, chip "Chưa duyệt", nút "Xem chi tiết"). Không có nút lật ở đây — nút nằm ở thanh đáy để
 * ngón cái không phải di chuyển giữa "Hiện đáp án" và 4 nút chấm.
 */
export function Flashcard({ card, flipped, onOpenDetail }: FlashcardProps) {
  const { word } = card
  const chip = STATE_CHIP[card.state]
  const meanings = (word.meaningsVi ?? []).slice(0, 3)

  return (
    <Stack sx={{ alignItems: 'center', textAlign: 'center', gap: 1.5, width: '100%' }}>
      {chip && <Chip size="small" color={chip.color} variant="outlined" label={chip.label} />}

      {/* ── Mặt trước ── */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Hanzi component="p" size="xxl" sx={{ fontSize: { xs: 72, sm: 96 }, m: 0, lineHeight: 1.15, wordBreak: 'keep-all' }}>
          {word.simplified}
        </Hanzi>
        <SpeakButton text={word.simplified} size="large" ariaLabel="Nghe" />
      </Box>

      {/* ── Mặt sau ── */}
      {flipped && (
        <Stack sx={{ alignItems: 'center', gap: 1, width: '100%' }}>
          <Divider sx={{ width: '60%', maxWidth: 240 }} />
          <Typography component="p" sx={{ fontSize: 24, fontWeight: 500 }} title={word.pinyin}>
            {numberedToMarked(word.pinyin)}
          </Typography>
          {word.hanViet && (
            <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
              {word.hanViet}
            </Typography>
          )}
          {meanings.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Chưa có nghĩa tiếng Việt — bấm "Xem chi tiết" để xem nghĩa tiếng Anh.
            </Typography>
          ) : (
            <Box component="ol" sx={{ pl: 0, my: 0, listStylePosition: 'inside', maxWidth: 480, '& li': { mb: 0.5 } }}>
              {meanings.map((m, i) => (
                <Typography component="li" key={i} sx={{ fontSize: 18 }}>
                  {m}
                </Typography>
              ))}
            </Box>
          )}
          <MeaningStatusChip status={word.meaningViStatus} />
          <Button size="small" endIcon={<OpenInNewIcon />} onClick={() => onOpenDetail(word.id)}>
            Xem chi tiết
          </Button>
        </Stack>
      )}
    </Stack>
  )
}
