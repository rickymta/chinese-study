import { Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { emptyLine, moveItem, removeAt, replaceAt, type ZhLineDraft } from '../lib/lessonDraft'
import { firstError, type PathErrors } from '../lib/validationErrors'
import { HanziField } from './HanziField'
import { PinyinField } from './PinyinField'
import { ReorderButtons } from './ReorderButtons'

export interface ZhLinesEditorProps {
  lines: ZhLineDraft[]
  onChange: (lines: ZhLineDraft[]) => void
  /** Tên trường trong payload (`lines` hội thoại, `examples` ngữ pháp) — khớp đường dẫn lỗi 400. */
  field: 'lines' | 'examples'
  /** Nhãn mỗi dòng: "Dòng" / "Ví dụ". */
  itemLabel: string
  withSpeaker?: boolean
  withNote?: boolean
  min: number
  max: number
  /** Lỗi tương đối tới payload khối (`lines[0].pinyin`). */
  errors?: PathErrors
  disabled?: boolean
}

/**
 * Danh sách dòng chữ Hán (hội thoại / ví dụ ngữ pháp): mỗi dòng [người nói] · chữ Hán · pinyin số thanh (kèm bản dấu)
 * · nghĩa Việt · [ghi chú]; nút lên/xuống/xoá. Xếp dọc ở 375px, hai cột từ `sm`.
 */
export function ZhLinesEditor({ lines, onChange, field, itemLabel, withSpeaker = false, withNote = false, min, max, errors, disabled = false }: ZhLinesEditorProps) {
  const update = (i: number, patch: Partial<ZhLineDraft>) => onChange(replaceAt(lines, i, { ...lines[i]!, ...patch }))
  const listError = firstError(errors, field)
  return (
    <Stack sx={{ gap: 1.5 }}>
      {listError && (
        <Typography variant="caption" color="error">
          {listError}
        </Typography>
      )}
      {lines.map((line, i) => {
        const p = (name: string) => `${field}[${i}].${name}`
        return (
          <Paper key={i} variant="outlined" sx={{ p: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">
                {itemLabel} {i + 1}
              </Typography>
              <Box>
                <ReorderButtons
                  index={i}
                  count={lines.length}
                  itemLabel={itemLabel.toLowerCase()}
                  disabled={disabled}
                  onMove={(from, to) => onChange(moveItem(lines, from, to))}
                  onRemove={() => onChange(removeAt(lines, i))}
                />
              </Box>
            </Box>
            <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
              {withSpeaker && (
                <TextField
                  label="Người nói"
                  value={line.speaker}
                  onChange={(e) => update(i, { speaker: e.target.value })}
                  size="small"
                  disabled={disabled}
                  error={!!firstError(errors, p('speaker'))}
                  helperText={firstError(errors, p('speaker')) ?? 'Tuỳ chọn, ≤ 20 ký tự (vd A, B, 小明)'}
                  slotProps={{ htmlInput: { maxLength: 20 } }}
                />
              )}
              <HanziField
                label="Chữ Hán"
                value={line.hanzi}
                onChange={(v) => update(i, { hanzi: v })}
                size="small"
                required
                disabled={disabled}
                error={!!firstError(errors, p('hanzi'))}
                helperText={firstError(errors, p('hanzi')) ?? 'Chữ Hán + dấu câu, không chữ Latin'}
                sx={withSpeaker ? undefined : { gridColumn: { sm: '1 / -1' } }}
              />
              <PinyinField
                label="Pinyin (số thanh)"
                value={line.pinyin}
                hanzi={line.hanzi}
                onChange={(v) => update(i, { pinyin: v })}
                size="small"
                required
                disabled={disabled}
                serverError={firstError(errors, p('pinyin'))}
              />
              <TextField
                label="Nghĩa tiếng Việt"
                value={line.vi}
                onChange={(e) => update(i, { vi: e.target.value })}
                size="small"
                required
                disabled={disabled}
                error={!!firstError(errors, p('vi'))}
                helperText={firstError(errors, p('vi'))}
                slotProps={{ htmlInput: { maxLength: 300 } }}
              />
              {withNote && (
                <TextField
                  label="Ghi chú"
                  value={line.note}
                  onChange={(e) => update(i, { note: e.target.value })}
                  size="small"
                  disabled={disabled}
                  error={!!firstError(errors, p('note'))}
                  helperText={firstError(errors, p('note')) ?? 'Tuỳ chọn, ≤ 200 ký tự'}
                  slotProps={{ htmlInput: { maxLength: 200 } }}
                  sx={{ gridColumn: { sm: '1 / -1' } }}
                />
              )}
            </Box>
          </Paper>
        )
      })}
      <Box>
        <Button
          size="small"
          startIcon={<AddIcon />}
          onClick={() => onChange([...lines, emptyLine()])}
          disabled={disabled || lines.length >= max}
        >
          Thêm {itemLabel.toLowerCase()}
        </Button>
        <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
          {min}–{max} {itemLabel.toLowerCase()}
        </Typography>
      </Box>
    </Stack>
  )
}
