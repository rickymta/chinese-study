import { Box, Card, CardContent, Typography } from '@mui/material'
import type { GrammarBlockPayload } from '../types'
import { InlineZh } from '../InlineZh'
import { ZhLineRow } from '../ZhLineRow'

/** Khối ngữ pháp: tiêu đề, mẫu câu (nền nổi), giải thích (chữ Hán nội dòng), ví dụ (dòng như hội thoại + ghi chú). */
export function GrammarBlock({ payload }: { payload: GrammarBlockPayload }) {
  return (
    <Card variant="outlined">
      <CardContent sx={{ px: { xs: 1.5, sm: 2 } }}>
        <Typography component="h3" variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {payload.title}
        </Typography>
        {payload.pattern && (
          <Box sx={{ bgcolor: 'action.hover', borderLeft: 3, borderColor: 'primary.main', borderRadius: 1, px: 1.5, py: 0.75, mb: 1.5 }}>
            <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
              Mẫu câu
            </Typography>
            <InlineZh text={payload.pattern} sx={{ fontWeight: 600 }} />
          </Box>
        )}
        <InlineZh text={payload.explanation} />
        {payload.examples.length > 0 && (
          <Box sx={{ mt: 1.5 }}>
            <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5, px: 1 }}>
              Ví dụ
            </Typography>
            {payload.examples.map((ex, i) => (
              <ZhLineRow key={i} line={ex} hanziSize={22} />
            ))}
          </Box>
        )}
      </CardContent>
    </Card>
  )
}
