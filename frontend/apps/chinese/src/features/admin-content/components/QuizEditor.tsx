import { Alert, Box, Button, Card, CardContent, Chip, IconButton, MenuItem, Radio, Stack, TextField, ToggleButton, ToggleButtonGroup, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import { useConfirm } from '@af/ui'
import type { QuizOptionLang, QuizPromptLang, QuizQuestionType } from '@af/chinese-kit'
import {
  isKnownQuestionPath,
  moveItem,
  newQuestionDraft,
  OPTION_IDS,
  OPTIONS_MAX,
  OPTIONS_MIN,
  QUIZ_MAX,
  removeAt,
  replaceAt,
  splitQuestionPath,
  type QuizOptionDraft,
  type QuizQuestionDraft,
} from '../lib/lessonDraft'
import { firstError, listErrorLines, stripPrefix, type PathErrors } from '../lib/validationErrors'
import { HanziField } from './HanziField'
import { PinyinField } from './PinyinField'
import { ReorderButtons } from './ReorderButtons'

const TYPE_LABELS: Record<QuizQuestionType, string> = { single_choice: 'Chọn đáp án', listen_choice: 'Nghe và chọn' }
const OPTION_LANG_LABELS: Record<QuizOptionLang, string> = { vi: 'Tiếng Việt', zh: 'Chữ Hán', pinyin: 'Pinyin' }

export interface QuizEditorProps {
  questions: QuizQuestionDraft[]
  onChange: (questions: QuizQuestionDraft[]) => void
  /** Lỗi 400 tuyệt đối (`questions[1].options[2].text`). */
  errors?: PathErrors
  disabled?: boolean
}

function QuestionCard({
  q,
  index,
  count,
  errors,
  disabled,
  onChange,
  onMove,
  onRemove,
}: {
  q: QuizQuestionDraft
  index: number
  count: number
  errors: PathErrors
  disabled: boolean
  onChange: (q: QuizQuestionDraft) => void
  onMove: (from: number, to: number) => void
  onRemove: () => void
}) {
  const hasError = Object.keys(errors).length > 0
  const setOption = (j: number, patch: Partial<QuizOptionDraft>) => onChange({ ...q, options: replaceAt(q.options, j, { ...q.options[j]!, ...patch }) })
  const removeOption = (j: number) => {
    const options = removeAt(q.options, j)
    const correctIndex = q.correctIndex === j ? 0 : q.correctIndex > j ? q.correctIndex - 1 : q.correctIndex
    onChange({ ...q, options, correctIndex })
  }
  return (
    <Card variant="outlined" sx={{ borderColor: hasError ? 'error.main' : 'divider' }}>
      <CardContent sx={{ px: { xs: 1.5, sm: 2 }, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Chip size="small" label={`Câu ${index + 1}`} color={hasError ? 'error' : 'default'} />
          {q.key && (
            <Typography variant="caption" color="text.secondary">
              {q.key}
            </Typography>
          )}
          <Box sx={{ flex: 1 }} />
          <ReorderButtons index={index} count={count} itemLabel="câu" disabled={disabled} onMove={onMove} onRemove={onRemove} />
        </Box>

        <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
          <TextField
            select
            label="Loại câu"
            value={q.type}
            onChange={(e) => onChange({ ...q, type: e.target.value as QuizQuestionType })}
            size="small"
            disabled={disabled}
            error={!!firstError(errors, 'type')}
            helperText={firstError(errors, 'type')}
          >
            {(Object.keys(TYPE_LABELS) as QuizQuestionType[]).map((t) => (
              <MenuItem key={t} value={t}>
                {TYPE_LABELS[t]}
              </MenuItem>
            ))}
          </TextField>
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
              Ngôn ngữ đề bài
            </Typography>
            <ToggleButtonGroup
              exclusive
              size="small"
              value={q.promptLang}
              onChange={(_e, v: QuizPromptLang | null) => v && onChange({ ...q, promptLang: v })}
              disabled={disabled}
              aria-label="Ngôn ngữ đề bài"
            >
              <ToggleButton value="vi">Tiếng Việt</ToggleButton>
              <ToggleButton value="zh">Chữ Hán</ToggleButton>
            </ToggleButtonGroup>
          </Box>
        </Box>

        {q.promptLang === 'zh' ? (
          <HanziField
            label="Đề bài (chữ Hán)"
            value={q.prompt}
            onChange={(v) => onChange({ ...q, prompt: v })}
            size="small"
            required
            disabled={disabled}
            error={!!firstError(errors, 'prompt')}
            helperText={firstError(errors, 'prompt') ?? '≤ 300 ký tự'}
            slotProps={{ htmlInput: { maxLength: 300 } }}
          />
        ) : (
          <TextField
            label="Đề bài"
            value={q.prompt}
            onChange={(e) => onChange({ ...q, prompt: e.target.value })}
            size="small"
            required
            multiline
            disabled={disabled}
            error={!!firstError(errors, 'prompt')}
            helperText={firstError(errors, 'prompt') ?? '≤ 300 ký tự'}
            slotProps={{ htmlInput: { maxLength: 300 } }}
          />
        )}
        {q.promptLang === 'zh' && (
          <PinyinField
            label="Pinyin đề bài"
            value={q.promptPinyin}
            hanzi={q.prompt}
            onChange={(v) => onChange({ ...q, promptPinyin: v })}
            size="small"
            optional
            disabled={disabled}
            serverError={firstError(errors, 'promptPinyin')}
          />
        )}
        {q.type === 'listen_choice' && (
          <HanziField
            label="Chữ Hán để đọc (audioText)"
            value={q.audioText}
            onChange={(v) => onChange({ ...q, audioText: v })}
            size="small"
            required
            disabled={disabled}
            error={!!firstError(errors, 'audioText')}
            helperText={firstError(errors, 'audioText') ?? 'Học viên chỉ nghe (TTS), không thấy chữ này — ≤ 100 chữ Hán'}
            slotProps={{ htmlInput: { maxLength: 100 } }}
          />
        )}

        <Box>
          <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
            Lựa chọn ({OPTIONS_MIN}–{OPTIONS_MAX}) — chọn nút tròn ở đáp án đúng
          </Typography>
          {firstError(errors, 'options') && (
            <Typography variant="caption" color="error" sx={{ display: 'block', mb: 0.5 }}>
              {firstError(errors, 'options')}
            </Typography>
          )}
          {firstError(errors, 'correctIndex') && (
            <Typography variant="caption" color="error" sx={{ display: 'block', mb: 0.5 }}>
              {firstError(errors, 'correctIndex')}
            </Typography>
          )}
          <Stack sx={{ gap: 1 }}>
            {q.options.map((o, j) => (
              <Box key={j} sx={{ display: 'flex', alignItems: 'flex-start', gap: 0.5 }}>
                <Tooltip title="Đáp án đúng">
                  <Radio
                    checked={q.correctIndex === j}
                    onChange={() => onChange({ ...q, correctIndex: j })}
                    disabled={disabled}
                    size="small"
                    sx={{ mt: 0.25 }}
                    slotProps={{ input: { 'aria-label': `Đáp án đúng là lựa chọn ${OPTION_IDS[j] ?? j + 1}` } }}
                  />
                </Tooltip>
                {o.lang === 'zh' ? (
                  <HanziField
                    label={`Lựa chọn ${OPTION_IDS[j] ?? j + 1}`}
                    value={o.text}
                    onChange={(v) => setOption(j, { text: v })}
                    size="small"
                    fullWidth
                    disabled={disabled}
                    error={!!firstError(errors, `options[${j}].text`)}
                    helperText={firstError(errors, `options[${j}].text`)}
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                  />
                ) : o.lang === 'pinyin' ? (
                  <PinyinField
                    label={`Lựa chọn ${OPTION_IDS[j] ?? j + 1}`}
                    value={o.text}
                    onChange={(v) => setOption(j, { text: v })}
                    size="small"
                    fullWidth
                    disabled={disabled}
                    serverError={firstError(errors, `options[${j}].text`)}
                  />
                ) : (
                  <TextField
                    label={`Lựa chọn ${OPTION_IDS[j] ?? j + 1}`}
                    value={o.text}
                    onChange={(e) => setOption(j, { text: e.target.value })}
                    size="small"
                    fullWidth
                    disabled={disabled}
                    error={!!firstError(errors, `options[${j}].text`)}
                    helperText={firstError(errors, `options[${j}].text`)}
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                  />
                )}
                <TextField
                  select
                  value={o.lang}
                  onChange={(e) => setOption(j, { lang: e.target.value as QuizOptionLang })}
                  size="small"
                  disabled={disabled}
                  error={!!firstError(errors, `options[${j}].lang`)}
                  sx={{ minWidth: 112, flexShrink: 0 }}
                  slotProps={{ htmlInput: { 'aria-label': 'Ngôn ngữ lựa chọn' } }}
                >
                  {(Object.keys(OPTION_LANG_LABELS) as QuizOptionLang[]).map((l) => (
                    <MenuItem key={l} value={l}>
                      {OPTION_LANG_LABELS[l]}
                    </MenuItem>
                  ))}
                </TextField>
                <Tooltip title="Xoá lựa chọn">
                  <span>
                    <IconButton
                      size="small"
                      aria-label={`Xoá lựa chọn ${OPTION_IDS[j] ?? j + 1}`}
                      color="error"
                      disabled={disabled || q.options.length <= OPTIONS_MIN}
                      onClick={() => removeOption(j)}
                      sx={{ mt: 0.5 }}
                    >
                      <DeleteOutlineIcon fontSize="inherit" />
                    </IconButton>
                  </span>
                </Tooltip>
              </Box>
            ))}
          </Stack>
          <Button
            size="small"
            startIcon={<AddIcon />}
            disabled={disabled || q.options.length >= OPTIONS_MAX}
            onClick={() => onChange({ ...q, options: [...q.options, { text: '', lang: q.options[q.options.length - 1]?.lang ?? 'vi' }] })}
            sx={{ mt: 0.5 }}
          >
            Thêm lựa chọn
          </Button>
        </Box>

        <TextField
          label="Lời giải"
          value={q.explanation}
          onChange={(e) => onChange({ ...q, explanation: e.target.value })}
          size="small"
          multiline
          disabled={disabled}
          error={!!firstError(errors, 'explanation')}
          helperText={firstError(errors, 'explanation') ?? 'Tuỳ chọn, ≤ 500 ký tự — hiện ở kết quả; dùng được [[chữ Hán|pinyin]]'}
          slotProps={{ htmlInput: { maxLength: 500 } }}
        />
      </CardContent>
    </Card>
  )
}

/**
 * Danh sách câu quiz (tab Quiz): mỗi câu loại/đề bài/ngôn ngữ/pinyin/audioText/2–4 lựa chọn (id a–d theo vị trí,
 * radio đáp án đúng)/lời giải; lên/xuống/xoá (useConfirm). Lỗi 400 cắt theo `questions[i].`.
 */
export function QuizEditor({ questions, onChange, errors = {}, disabled = false }: QuizEditorProps) {
  const confirm = useConfirm()
  const unmapped: PathErrors = {}
  for (const [path, messages] of Object.entries(errors)) {
    const split = splitQuestionPath(path)
    const q = split ? questions[split.index] : undefined
    if (!split || !q || !isKnownQuestionPath(q, split.rel)) unmapped[path] = messages
  }
  const unmappedLines = listErrorLines(unmapped)
  const listen = questions.filter((q) => q.type === 'listen_choice').length

  const remove = async (i: number) => {
    const ok = await confirm({
      title: `Xoá câu ${i + 1}?`,
      message: questions[i]?.id
        ? 'Câu này đã có trên máy chủ — sau khi Lưu, lịch sử làm bài của học viên vẫn giữ, nhưng người đang làm dở sẽ phải làm lại (quiz đổi).'
        : 'Câu sẽ bị bỏ khỏi bản nháp.',
      confirmText: 'Xoá câu',
      tone: 'danger',
    })
    if (ok) onChange(removeAt(questions, i))
  }

  return (
    <Stack sx={{ gap: 2 }}>
      {unmappedLines.length > 0 && (
        <Alert severity="error">
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            Máy chủ báo lỗi:
          </Typography>
          <Box component="ul" sx={{ pl: 2.5, my: 0.5 }}>
            {unmappedLines.map((l) => (
              <li key={l}>{l}</li>
            ))}
          </Box>
        </Alert>
      )}
      <Typography variant="body2" color="text.secondary">
        Xuất bản cần ≥ 3 câu (nên 5–10), mỗi câu 2–4 lựa chọn; nên có ≥ 30% câu nghe. Hiện có {questions.length} câu, {listen} câu nghe.
      </Typography>
      {questions.length === 0 && <Typography color="text.secondary">Chưa có câu hỏi nào.</Typography>}
      {questions.map((q, i) => (
        <QuestionCard
          key={q.localId}
          q={q}
          index={i}
          count={questions.length}
          errors={stripPrefix(errors, `questions[${i}].`)}
          disabled={disabled}
          onChange={(next) => onChange(replaceAt(questions, i, next))}
          onMove={(from, to) => onChange(moveItem(questions, from, to))}
          onRemove={() => void remove(i)}
        />
      ))}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Button variant="outlined" startIcon={<AddIcon />} disabled={disabled || questions.length >= QUIZ_MAX} onClick={() => onChange([...questions, newQuestionDraft()])}>
          Thêm câu hỏi
        </Button>
        <Typography variant="caption" color="text.secondary">
          {questions.length}/{QUIZ_MAX} câu
        </Typography>
      </Box>
    </Stack>
  )
}
