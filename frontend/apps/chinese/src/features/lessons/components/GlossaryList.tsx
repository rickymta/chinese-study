import { Box, Card, CardContent, Typography } from '@mui/material'
import { Hanzi } from '@/components/Hanzi'
import { SpeakButton } from '@/components/speech/SpeakButton'
import { numberedToMarked } from '@/lib/pinyin'
import type { GlossaryEntry } from '../types'

/** "Từ bổ sung (không vào ôn tập)": tên riêng, địa danh... chỉ để hiểu bài — không phải từ HSK, không thành thẻ. */
export function GlossaryList({ entries }: { entries: GlossaryEntry[] }) {
  if (entries.length === 0) return null
  return (
    <Card variant="outlined">
      <CardContent sx={{ px: { xs: 1.5, sm: 2 } }}>
        <Typography component="h3" variant="subtitle1" sx={{ fontWeight: 700 }}>
          Từ bổ sung
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
          Chỉ để hiểu bài — không vào ôn tập.
        </Typography>
        {entries.map((g, i) => (
          <Box key={i} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 0.5, minWidth: 0 }}>
            <Hanzi size="md" sx={{ flexShrink: 0 }}>
              {g.hanzi}
            </Hanzi>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {numberedToMarked(g.pinyin)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {g.vi}
              </Typography>
            </Box>
            <SpeakButton text={g.hanzi} size="small" ariaLabel="Nghe" />
          </Box>
        ))}
      </CardContent>
    </Card>
  )
}
