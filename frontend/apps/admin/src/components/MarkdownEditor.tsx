import { useId, useRef, useState, type ReactElement } from 'react'
import { Box, FormHelperText, FormLabel, IconButton, Paper, Tab, Tabs, TextField, Tooltip, Typography } from '@mui/material'
import FormatBoldIcon from '@mui/icons-material/FormatBold'
import FormatItalicIcon from '@mui/icons-material/FormatItalic'
import TitleIcon from '@mui/icons-material/Title'
import LinkIcon from '@mui/icons-material/Link'
import FormatListBulletedIcon from '@mui/icons-material/FormatListBulleted'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import { MarkdownPreview } from './MarkdownPreview'

export interface MarkdownEditorProps {
  value: string
  onChange: (value: string) => void
  label?: string
  error?: boolean
  helperText?: string
  minRows?: number
  disabled?: boolean
  maxLength?: number
  /** Chuyển tiếp cho react-hook-form (validate lúc blur). */
  onBlur?: () => void
  name?: string
}

type Tool = { key: string; title: string; icon: ReactElement; before: string; after: string; placeholder: string; block?: boolean }

const TOOLS: Tool[] = [
  { key: 'bold', title: 'Đậm', icon: <FormatBoldIcon fontSize="small" />, before: '**', after: '**', placeholder: 'chữ đậm' },
  { key: 'italic', title: 'Nghiêng', icon: <FormatItalicIcon fontSize="small" />, before: '*', after: '*', placeholder: 'chữ nghiêng' },
  { key: 'heading', title: 'Tiêu đề', icon: <TitleIcon fontSize="small" />, before: '## ', after: '', placeholder: 'Tiêu đề', block: true },
  { key: 'link', title: 'Liên kết', icon: <LinkIcon fontSize="small" />, before: '[', after: '](https://)', placeholder: 'chữ' },
  { key: 'list', title: 'Danh sách', icon: <FormatListBulletedIcon fontSize="small" />, before: '- ', after: '', placeholder: 'mục', block: true },
  { key: 'image', title: 'Ảnh', icon: <ImageOutlinedIcon fontSize="small" />, before: '![', after: '](url)', placeholder: 'mô tả ảnh' },
]

/**
 * Trình soạn Markdown (hợp đồng W3b §5.3.3a): tab Soạn / Xem trước (state cục bộ — KHÔNG `useTabParam` vì đây là
 * tab bên trong form/dialog, không phải tab cấp trang; `lint:ui` WARN `tabs-no-url` ở file này là có chủ đích),
 * thanh công cụ chèn cú pháp quanh vùng chọn của textarea và giữ con trỏ. Xem trước dùng `MarkdownPreview`
 * (skipHtml, không rehype-raw) nên HTML dán vào không chạy.
 */
export function MarkdownEditor({ value, onChange, label, error, helperText, minRows = 12, disabled, maxLength, onBlur, name }: MarkdownEditorProps) {
  const [tab, setTab] = useState<'edit' | 'preview'>('edit')
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)
  const id = useId()

  /** Bọc vùng chọn bằng `before`/`after`; không chọn gì ⇒ chèn placeholder và chọn sẵn placeholder để gõ đè. */
  const apply = (tool: Tool) => {
    const el = textareaRef.current
    const start = el?.selectionStart ?? value.length
    const end = el?.selectionEnd ?? value.length
    const selected = value.slice(start, end)
    let prefix = tool.before
    // Công cụ dạng khối (tiêu đề, danh sách) phải đứng đầu dòng.
    if (tool.block && start > 0 && value[start - 1] !== '\n') prefix = '\n' + tool.before
    const inner = selected || tool.placeholder
    const next = value.slice(0, start) + prefix + inner + tool.after + value.slice(end)
    onChange(next)
    const selStart = start + prefix.length
    const selEnd = selStart + inner.length
    // Đặt lại con trỏ sau khi React render giá trị mới.
    requestAnimationFrame(() => {
      const ta = textareaRef.current
      if (!ta) return
      ta.focus()
      ta.setSelectionRange(selStart, selEnd)
    })
  }

  return (
    <Box>
      {label && (
        <FormLabel htmlFor={`${id}-textarea`} error={error} sx={{ display: 'block', mb: 0.5, fontSize: '0.875rem' }}>
          {label}
        </FormLabel>
      )}
      <Paper variant="outlined" sx={{ borderColor: error ? 'error.main' : 'divider', overflow: 'hidden' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 0.5, px: 1, borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tab} onChange={(_e, v: 'edit' | 'preview') => setTab(v)} sx={{ minHeight: 40 }} aria-label="Chế độ soạn Markdown">
            <Tab value="edit" label="Soạn" sx={{ minHeight: 40, py: 0 }} />
            <Tab value="preview" label="Xem trước" sx={{ minHeight: 40, py: 0 }} />
          </Tabs>
          {tab === 'edit' && (
            <Box sx={{ display: 'flex', flexWrap: 'wrap' }} role="toolbar" aria-label="Công cụ Markdown">
              {TOOLS.map((t) => (
                <Tooltip key={t.key} title={t.title}>
                  <span>
                    <IconButton size="small" aria-label={t.title} disabled={disabled} onMouseDown={(e) => e.preventDefault()} onClick={() => apply(t)}>
                      {t.icon}
                    </IconButton>
                  </span>
                </Tooltip>
              ))}
            </Box>
          )}
        </Box>
        {tab === 'edit' ? (
          <TextField
            id={`${id}-textarea`}
            name={name}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onBlur={onBlur}
            fullWidth
            multiline
            minRows={minRows}
            disabled={disabled}
            placeholder="Hỗ trợ Markdown (GFM): **đậm**, *nghiêng*, ## tiêu đề, - danh sách, bảng, [liên kết](https://)… Không dùng HTML."
            inputRef={textareaRef}
            variant="standard"
            slotProps={{
              input: { disableUnderline: true, sx: { p: 1.5, fontFamily: 'monospace', fontSize: '0.9rem', alignItems: 'flex-start' } },
              htmlInput: { maxLength, spellCheck: false, 'aria-invalid': error || undefined },
            }}
          />
        ) : (
          <Box sx={{ p: 1.5, minHeight: `${minRows * 1.5}em` }}>
            {value.trim() ? (
              <MarkdownPreview source={value} />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Chưa có nội dung để xem trước.
              </Typography>
            )}
          </Box>
        )}
      </Paper>
      {helperText && <FormHelperText error={error}>{helperText}</FormHelperText>}
    </Box>
  )
}
