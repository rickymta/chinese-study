import { Stack, TextField, Typography } from '@mui/material'
import type { BlockDraft } from '../../lib/lessonDraft'
import { firstError, type PathErrors } from '../../lib/validationErrors'
import { ZhLinesEditor } from '../ZhLinesEditor'

type GrammarDraft = Extract<BlockDraft, { type: 'grammar' }>

export interface GrammarBlockFormProps {
  block: GrammarDraft
  onChange: (block: GrammarDraft) => void
  errors?: PathErrors
  disabled?: boolean
}

/** Khối ngữ pháp: tiêu đề, mẫu câu (tuỳ chọn), giải thích (hỗ trợ `[[…|…]]`), 1–6 ví dụ có ghi chú. */
export function GrammarBlockForm({ block, onChange, errors, disabled = false }: GrammarBlockFormProps) {
  return (
    <Stack sx={{ gap: 1.5 }}>
      <TextField
        label="Tiêu đề"
        value={block.title}
        onChange={(e) => onChange({ ...block, title: e.target.value })}
        size="small"
        required
        disabled={disabled}
        error={!!firstError(errors, 'title')}
        helperText={firstError(errors, 'title') ?? '≤ 100 ký tự'}
        slotProps={{ htmlInput: { maxLength: 100 } }}
      />
      <TextField
        label="Mẫu câu"
        value={block.pattern}
        onChange={(e) => onChange({ ...block, pattern: e.target.value })}
        size="small"
        disabled={disabled}
        error={!!firstError(errors, 'pattern')}
        helperText={firstError(errors, 'pattern') ?? 'Tuỳ chọn, ≤ 200 ký tự — vd [[是|shi4]] + danh từ'}
        slotProps={{ htmlInput: { maxLength: 200 } }}
      />
      <TextField
        label="Giải thích"
        value={block.explanation}
        onChange={(e) => onChange({ ...block, explanation: e.target.value })}
        multiline
        minRows={3}
        size="small"
        required
        disabled={disabled}
        error={!!firstError(errors, 'explanation')}
        helperText={firstError(errors, 'explanation') ?? '≤ 2000 ký tự, dùng được [[chữ Hán|pinyin]]'}
        slotProps={{ htmlInput: { maxLength: 2000 } }}
      />
      <Typography variant="subtitle2">Ví dụ</Typography>
      <ZhLinesEditor
        lines={block.examples}
        onChange={(examples) => onChange({ ...block, examples })}
        field="examples"
        itemLabel="Ví dụ"
        withNote
        min={1}
        max={6}
        errors={errors}
        disabled={disabled}
      />
    </Stack>
  )
}
