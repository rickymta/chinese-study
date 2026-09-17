import { useState } from 'react'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { parseApiError } from '@af/utils'
import { Hanzi, Pinyin, SpeakButton } from '@af/chinese-kit'
import { usePinyinGuide } from '../hooks'
import type { GuideBlock, GuideExample } from '../types'
import { ToneContour } from './ToneContour'

/** Một dòng ví dụ: chữ Hán lớn · pinyin dấu (kèm gợi ý biến điệu) · nghĩa · nút nghe. */
function ExampleRow({ ex, showSandhi }: { ex: GuideExample; showSandhi: boolean }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 0.5 }}>
      <Hanzi size="lg" sx={{ minWidth: 48 }}>
        {ex.hanzi}
      </Hanzi>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Pinyin value={ex.pinyin} hanzi={ex.hanzi} showSandhi={showSandhi} sx={{ fontWeight: 600 }} />
        <Typography variant="body2" color="text.secondary">
          {ex.meaningVi}
        </Typography>
      </Box>
      <SpeakButton text={ex.hanzi} />
    </Box>
  )
}

function Block({ block, topicId }: { block: GuideBlock; topicId: string }) {
  // Chủ đề biến điệu: hiện gợi ý dưới pinyin để người học thấy đúng cái đang được giảng.
  const showSandhi = topicId === 'bien-dieu'
  switch (block.type) {
    case 'paragraph':
      return <Typography sx={{ lineHeight: 1.7 }}>{block.text}</Typography>
    case 'tip':
      return <Alert severity="info">{block.text}</Alert>
    case 'tone_contour':
      return <ToneContour tones={block.tones} />
    case 'examples':
      return (
        <Stack divider={<Box sx={{ borderBottom: 1, borderColor: 'divider' }} />}>
          {block.items.map((ex, i) => (
            <ExampleRow key={`${ex.pinyin}-${i}`} ex={ex} showSandhi={showSandhi} />
          ))}
        </Stack>
      )
    case 'compare':
      return (
        <Box>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            {block.title}
          </Typography>
          <Stack spacing={1}>
            {block.pairs.map((pair, i) => (
              <Box key={i} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 1 }}>
                <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                  {[pair.left, pair.right].map((side, j) => (
                    <Box key={j} sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 0 }}>
                      <Hanzi size="lg">{side.hanzi}</Hanzi>
                      <Box sx={{ minWidth: 0, flex: 1 }}>
                        <Pinyin value={side.pinyin} sx={{ fontWeight: 600 }} />
                        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                          {side.meaningVi}
                        </Typography>
                      </Box>
                      <SpeakButton text={side.hanzi} size="small" />
                    </Box>
                  ))}
                </Box>
                {pair.noteVi && (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
                    {pair.noteVi}
                  </Typography>
                )}
              </Box>
            ))}
          </Stack>
        </Box>
      )
    default:
      return null
  }
}

/** Tab Hướng dẫn: chủ đề dạng Accordion (mở sẵn chủ đề đầu), khối theo `guide.json`. Cuối tab: nút sang bảng. */
export function GuideView({ onGoToChart }: { onGoToChart: () => void }) {
  const guide = usePinyinGuide()
  const [expanded, setExpanded] = useState<string | null>(null)

  if (guide.isPending) {
    return (
      <Stack spacing={1}>
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} variant="rounded" height={56} />
        ))}
      </Stack>
    )
  }
  if (guide.isError) {
    const err = parseApiError(guide.error)
    return (
      <Alert
        severity={err.status === 503 ? 'warning' : 'error'}
        action={
          <Button color="inherit" size="small" onClick={() => void guide.refetch()}>
            Thử lại
          </Button>
        }
      >
        {err.status === 503 ? 'Học liệu pinyin chưa sẵn sàng — báo quản trị viên.' : err.message}
      </Alert>
    )
  }

  const topics = guide.data.topics
  const current = expanded ?? topics[0]?.id ?? null

  return (
    <Stack spacing={1}>
      {topics.map((topic, idx) => (
        <Accordion
          key={topic.id}
          expanded={current === topic.id}
          onChange={(_e, open) => setExpanded(open ? topic.id : '')}
          disableGutters
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />} aria-controls={`guide-${topic.id}`} id={`guide-h-${topic.id}`}>
            <Typography sx={{ fontWeight: 600 }}>
              {idx + 1}. {topic.title}
            </Typography>
          </AccordionSummary>
          <AccordionDetails id={`guide-${topic.id}`}>
            <Stack spacing={2}>
              {topic.blocks.map((block, i) => (
                <Block key={i} block={block} topicId={topic.id} />
              ))}
            </Stack>
          </AccordionDetails>
        </Accordion>
      ))}
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', pt: 1 }}>
        <Button variant="contained" endIcon={<ArrowForwardIcon />} onClick={onGoToChart}>
          Sang bảng âm tiết
        </Button>
      </Box>
    </Stack>
  )
}
