import { useMemo, useState } from 'react'
import { Alert, Box, Card, CardContent, Chip, Divider, FormControlLabel, List, ListItem, ListItemIcon, ListItemText, Stack, Switch, Typography } from '@mui/material'
import CheckOutlinedIcon from '@mui/icons-material/CheckOutlined'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import RadioButtonUncheckedIcon from '@mui/icons-material/RadioButtonUnchecked'
import VolumeUpOutlinedIcon from '@mui/icons-material/VolumeUpOutlined'
import {
  Hanzi,
  numberedToMarked,
  LessonContent,
  LessonDisplayProvider,
  InlineZh,
  OptionText,
  QuestionPrompt,
  MeaningStatusChip,
} from '@af/chinese-kit'
import { blocksToPreview, parseObjectives, quizToPreview, type BlockDraft, type MetaDraft, type QuizQuestionDraft } from '../lib/lessonDraft'
import type { AdminLessonWord } from '../types'

export interface LessonPreviewProps {
  meta: MetaDraft
  blocks: BlockDraft[]
  words: AdminLessonWord[]
  quiz: QuizQuestionDraft[]
  /** Có tab chưa lưu ⇒ nhắc rằng xem trước là BẢN NHÁP. */
  dirtyAny: boolean
}

/**
 * Tab "Xem trước": dựng lại trang bài của học viên từ BẢN NHÁP hiện tại — dùng nguyên `LessonContent` (F9, ruby
 * pinyin trên chữ Hán nội dòng, nút nghe) + danh sách từ + quiz có đánh dấu đáp án đúng và lời giải.
 * Cần nằm trong `ChineseSpeechProvider` (trang bọc).
 */
export function LessonPreview({ meta, blocks, words, quiz, dirtyAny }: LessonPreviewProps) {
  const [showPinyin, setShowPinyin] = useState(true)
  const [showVi, setShowVi] = useState(true)
  const previewBlocks = useMemo(() => blocksToPreview(blocks), [blocks])
  const previewQuiz = useMemo(() => quizToPreview(quiz), [quiz])
  const objectives = parseObjectives(meta.objectivesText)
  const glossary = meta.glossary.filter((g) => g.hanzi.trim())

  return (
    <LessonDisplayProvider value={{ showPinyin, showVi }}>
      <Stack sx={{ gap: 2 }}>
        <Alert severity={dirtyAny ? 'warning' : 'info'} variant="outlined">
          {dirtyAny ? 'Đang xem BẢN NHÁP có thay đổi chưa lưu — học viên vẫn thấy bản đã lưu.' : 'Xem như học viên sẽ thấy (bản đã lưu).'}
        </Alert>

        <Box>
          <Typography variant="caption" color="text.secondary">
            Bài {meta.orderIndex || '?'}
          </Typography>
          <Typography component="h2" variant="h5" sx={{ fontWeight: 700, lineHeight: 1.3 }}>
            {meta.title.trim() || '(chưa có tiêu đề)'}
          </Typography>
          <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 0.75 }}>
            <Chip size="small" variant="outlined" label={`~${meta.estimatedMinutes || '?'} phút`} />
            <Chip size="small" variant="outlined" label={`${words.length} từ · ${quiz.length} câu hỏi`} />
          </Box>
          {meta.summary.trim() && (
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              {meta.summary}
            </Typography>
          )}
          {objectives.length > 0 && (
            <Box sx={{ mt: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                Sau bài này bạn sẽ
              </Typography>
              <List dense disablePadding>
                {objectives.map((o, i) => (
                  <ListItem key={i} disableGutters sx={{ py: 0.25 }}>
                    <ListItemIcon sx={{ minWidth: 28 }}>
                      <CheckOutlinedIcon fontSize="small" color="success" />
                    </ListItemIcon>
                    <ListItemText primary={o} slotProps={{ primary: { variant: 'body2' } }} />
                  </ListItem>
                ))}
              </List>
            </Box>
          )}
        </Box>

        <Divider />
        <Typography component="h3" variant="h6" sx={{ fontWeight: 700 }}>
          Nội dung
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <FormControlLabel control={<Switch checked={showPinyin} onChange={(_e, v) => setShowPinyin(v)} size="small" />} label="Pinyin" />
          <FormControlLabel control={<Switch checked={showVi} onChange={(_e, v) => setShowVi(v)} size="small" />} label="Nghĩa tiếng Việt" />
        </Box>
        <LessonContent blocks={previewBlocks} glossary={glossary} />

        <Divider />
        <Typography component="h3" variant="h6" sx={{ fontWeight: 700 }}>
          Từ vựng ({words.length})
        </Typography>
        {words.length === 0 ? (
          <Typography color="text.secondary">Bài chưa có từ vựng.</Typography>
        ) : (
          <List disablePadding sx={{ bgcolor: 'background.paper', borderRadius: 1, border: 1, borderColor: 'divider' }}>
            {words.map((w) => (
              <ListItem key={w.id} divider sx={{ gap: 1.5, minHeight: 56 }}>
                <Hanzi size="md" sx={{ fontSize: 28, minWidth: 44, textAlign: 'center' }}>
                  {w.simplified}
                </Hanzi>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {numberedToMarked(w.pinyin)}
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, minWidth: 0 }}>
                    <Typography variant="body2" color="text.secondary" noWrap>
                      {(w.meaningsVi ?? []).slice(0, 2).join('; ') || '—'}
                    </Typography>
                    <MeaningStatusChip status={w.meaningViStatus} />
                  </Box>
                </Box>
              </ListItem>
            ))}
          </List>
        )}

        <Divider />
        <Typography component="h3" variant="h6" sx={{ fontWeight: 700 }}>
          Quiz ({quiz.length}) — có đánh dấu đáp án
        </Typography>
        {previewQuiz.length === 0 && <Typography color="text.secondary">Chưa có câu hỏi.</Typography>}
        {previewQuiz.map((q, i) => (
          <Card key={q.id} variant="outlined">
            <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Typography variant="caption" color="text.secondary">
                Câu {i + 1} · {q.type === 'listen_choice' ? 'Nghe và chọn' : 'Chọn đáp án'}
              </Typography>
              <QuestionPrompt question={q} compact />
              {q.type === 'listen_choice' && q.audioText && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <VolumeUpOutlinedIcon fontSize="small" color="action" />
                  <Hanzi size="sm" sx={{ fontSize: 20 }}>
                    {q.audioText}
                  </Hanzi>
                  <Typography variant="caption" color="text.secondary">
                    (học viên chỉ nghe)
                  </Typography>
                </Box>
              )}
              <Stack sx={{ gap: 0.5 }}>
                {q.options.map((o) => {
                  const correct = o.id === q.correctOptionId
                  return (
                    <Box
                      key={o.id}
                      sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 1, py: 0.5, borderRadius: 1, bgcolor: correct ? 'success.light' : 'transparent', color: correct ? 'success.contrastText' : 'inherit', '& *': { color: 'inherit' } }}
                    >
                      {correct ? <CheckCircleIcon fontSize="small" /> : <RadioButtonUncheckedIcon fontSize="small" sx={{ opacity: 0.4 }} />}
                      <Typography variant="caption" sx={{ width: 16 }}>
                        {o.id}
                      </Typography>
                      <OptionText option={o} size="sm" />
                    </Box>
                  )
                })}
              </Stack>
              {q.explanation && <InlineZh text={q.explanation} variant="body2" color="text.secondary" />}
            </CardContent>
          </Card>
        ))}
      </Stack>
    </LessonDisplayProvider>
  )
}
