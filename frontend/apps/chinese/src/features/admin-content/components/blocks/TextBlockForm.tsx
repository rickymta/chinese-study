import { Box, Button, IconButton, Stack, TextField, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import { removeAt, replaceAt, type BlockDraft } from '../../lib/lessonDraft'
import { firstError, type PathErrors } from '../../lib/validationErrors'

type TextDraft = Extract<BlockDraft, { type: 'text' }>

export interface TextBlockFormProps {
  block: TextDraft
  onChange: (block: TextDraft) => void
  errors?: PathErrors
  disabled?: boolean
}

const PARAGRAPHS_MAX = 10

/** Khối văn bản: 1–10 đoạn (≤ 2000 ký tự), hỗ trợ token chữ Hán nội dòng `[[你好|ni3 hao3]]` (§5.4.3). */
export function TextBlockForm({ block, onChange, errors, disabled = false }: TextBlockFormProps) {
  const listError = firstError(errors, 'paragraphs')
  return (
    <Stack sx={{ gap: 1.5 }}>
      <Typography variant="caption" color="text.secondary">
        Chèn chữ Hán trong đoạn bằng cú pháp <code>[[你好|ni3 hao3]]</code> — hiện thành chữ Hán có pinyin phía trên, bấm để nghe.
      </Typography>
      {listError && (
        <Typography variant="caption" color="error">
          {listError}
        </Typography>
      )}
      {block.paragraphs.map((p, i) => (
        <Box key={i} sx={{ display: 'flex', alignItems: 'flex-start', gap: 0.5 }}>
          <TextField
            label={`Đoạn ${i + 1}`}
            value={p}
            onChange={(e) => onChange({ ...block, paragraphs: replaceAt(block.paragraphs, i, e.target.value) })}
            multiline
            minRows={2}
            fullWidth
            size="small"
            disabled={disabled}
            error={!!firstError(errors, `paragraphs[${i}]`)}
            helperText={firstError(errors, `paragraphs[${i}]`)}
            slotProps={{ htmlInput: { maxLength: 2000 } }}
          />
          <Tooltip title="Xoá đoạn">
            <span>
              <IconButton
                size="small"
                aria-label={`Xoá đoạn ${i + 1}`}
                color="error"
                disabled={disabled || block.paragraphs.length <= 1}
                onClick={() => onChange({ ...block, paragraphs: removeAt(block.paragraphs, i) })}
                sx={{ mt: 0.5 }}
              >
                <DeleteOutlineIcon fontSize="inherit" />
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      ))}
      <Box>
        <Button
          size="small"
          startIcon={<AddIcon />}
          disabled={disabled || block.paragraphs.length >= PARAGRAPHS_MAX}
          onClick={() => onChange({ ...block, paragraphs: [...block.paragraphs, ''] })}
        >
          Thêm đoạn
        </Button>
      </Box>
    </Stack>
  )
}
