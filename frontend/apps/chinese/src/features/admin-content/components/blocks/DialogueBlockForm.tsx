import { Stack, TextField } from '@mui/material'
import type { BlockDraft } from '../../lib/lessonDraft'
import { firstError, type PathErrors } from '../../lib/validationErrors'
import { ZhLinesEditor } from '../ZhLinesEditor'

type DialogueDraft = Extract<BlockDraft, { type: 'dialogue' }>

export interface DialogueBlockFormProps {
  block: DialogueDraft
  onChange: (block: DialogueDraft) => void
  errors?: PathErrors
  disabled?: boolean
}

/** Khối hội thoại: tiêu đề tuỳ chọn + 2–20 dòng (người nói, chữ Hán, pinyin, nghĩa). */
export function DialogueBlockForm({ block, onChange, errors, disabled = false }: DialogueBlockFormProps) {
  return (
    <Stack sx={{ gap: 1.5 }}>
      <TextField
        label="Tiêu đề hội thoại"
        value={block.title}
        onChange={(e) => onChange({ ...block, title: e.target.value })}
        size="small"
        disabled={disabled}
        error={!!firstError(errors, 'title')}
        helperText={firstError(errors, 'title') ?? 'Tuỳ chọn, ≤ 100 ký tự (mặc định "Hội thoại")'}
        slotProps={{ htmlInput: { maxLength: 100 } }}
      />
      <ZhLinesEditor
        lines={block.lines}
        onChange={(lines) => onChange({ ...block, lines })}
        field="lines"
        itemLabel="Dòng"
        withSpeaker
        min={2}
        max={20}
        errors={errors}
        disabled={disabled}
      />
    </Stack>
  )
}
