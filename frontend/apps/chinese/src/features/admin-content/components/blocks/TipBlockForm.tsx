import { MenuItem, Stack, TextField } from '@mui/material'
import type { TipVariant } from '@/features/lessons/types'
import type { BlockDraft } from '../../lib/lessonDraft'
import { firstError, type PathErrors } from '../../lib/validationErrors'

type TipDraft = Extract<BlockDraft, { type: 'tip' }>

export interface TipBlockFormProps {
  block: TipDraft
  onChange: (block: TipDraft) => void
  errors?: PathErrors
  disabled?: boolean
}

const VARIANTS: { value: TipVariant | ''; label: string }[] = [
  { value: '', label: 'Mặc định' },
  { value: 'pronunciation', label: 'Phát âm' },
  { value: 'culture', label: 'Văn hoá' },
  { value: 'memory', label: 'Mẹo nhớ' },
  { value: 'grammar', label: 'Ngữ pháp' },
]

/** Khối mẹo: loại (tuỳ chọn) + nội dung ≤ 1000 ký tự (hỗ trợ `[[…|…]]`). */
export function TipBlockForm({ block, onChange, errors, disabled = false }: TipBlockFormProps) {
  return (
    <Stack sx={{ gap: 1.5 }}>
      <TextField
        select
        label="Loại mẹo"
        value={block.variant}
        onChange={(e) => onChange({ ...block, variant: e.target.value as TipVariant | '' })}
        size="small"
        disabled={disabled}
        error={!!firstError(errors, 'variant')}
        helperText={firstError(errors, 'variant')}
        sx={{ maxWidth: 240 }}
      >
        {VARIANTS.map((v) => (
          <MenuItem key={v.value} value={v.value}>
            {v.label}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        label="Nội dung"
        value={block.text}
        onChange={(e) => onChange({ ...block, text: e.target.value })}
        multiline
        minRows={2}
        size="small"
        required
        disabled={disabled}
        error={!!firstError(errors, 'text')}
        helperText={firstError(errors, 'text') ?? '≤ 1000 ký tự, dùng được [[chữ Hán|pinyin]]'}
        slotProps={{ htmlInput: { maxLength: 1000 } }}
      />
    </Stack>
  )
}
