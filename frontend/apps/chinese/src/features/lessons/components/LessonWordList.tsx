import { Box, Chip, List, ListItem, ListItemButton, Typography } from '@mui/material'
import { Link, useLocation } from 'react-router-dom'
import { linkState } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import { MeaningStatusChip } from '@/features/dictionary/components/MeaningStatusChip'
import type { LessonWord } from '../types'

export interface LessonWordListProps {
  words: LessonWord[]
}

/**
 * Danh sách từ của bài (tab Từ vựng): chữ Hán lớn · pinyin dấu · Hán Việt · ≤ 2 nghĩa (chip "Chưa duyệt" khi
 * `machine`) · chip "Đang ôn" khi đã có thẻ SRS · nút nghe (secondaryAction — không lồng nút trong link).
 * Bấm dòng ⇒ `/tu-dien/:id` mang `state.from` để quay lại đúng bài + tab.
 */
export function LessonWordList({ words }: LessonWordListProps) {
  const location = useLocation()
  if (words.length === 0) {
    return <Typography color="text.secondary">Bài này chưa có từ vựng.</Typography>
  }
  return (
    <List disablePadding sx={{ bgcolor: 'background.paper', borderRadius: 1, border: 1, borderColor: 'divider' }}>
      {words.map((w) => {
        const meanings = (w.meaningsVi ?? []).slice(0, 2).join('; ')
        return (
          <ListItem
            key={w.id}
            disablePadding
            divider
            secondaryAction={<SpeakButton text={w.simplified} size="small" ariaLabel="Nghe" />}
          >
            <ListItemButton
              component={Link}
              to={`/tu-dien/${w.id}`}
              state={linkState(location)}
              sx={{ minHeight: 64, gap: 1.5, alignItems: 'center', px: { xs: 1, sm: 2 }, pr: { xs: 6, sm: 7 } }}
            >
              <Hanzi size="md" sx={{ fontSize: 30, flexShrink: 0, minWidth: 44, textAlign: 'center' }}>
                {w.simplified}
              </Hanzi>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, minWidth: 0, flexWrap: 'wrap' }}>
                  <Typography component="span" variant="body1" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                    {numberedToMarked(w.pinyin)}
                  </Typography>
                  {w.hanViet && (
                    <Typography component="span" variant="caption" color="text.secondary" noWrap sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                      {w.hanViet}
                    </Typography>
                  )}
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, minWidth: 0 }}>
                  <Typography variant="body2" color="text.secondary" noWrap sx={{ minWidth: 0 }}>
                    {meanings || '—'}
                  </Typography>
                  <MeaningStatusChip status={w.meaningViStatus} sx={{ flexShrink: 0 }} />
                  {w.inSrs && <Chip size="small" color="success" variant="outlined" label="Đang ôn" sx={{ flexShrink: 0 }} />}
                </Box>
              </Box>
            </ListItemButton>
          </ListItem>
        )
      })}
    </List>
  )
}
