import { Box, Button, IconButton, Paper, Stack, TextField, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import type { GlossaryEntry } from '@/features/lessons/types'
import { GLOSSARY_MAX, OBJECTIVES_MAX, removeAt, replaceAt, type MetaDraft } from '../lib/lessonDraft'
import { slugify } from '../lib/slug'
import { firstError, type PathErrors } from '../lib/validationErrors'
import { HanziField } from './HanziField'
import { PinyinField } from './PinyinField'

export interface LessonMetaFormProps {
  value: MetaDraft
  onChange: (value: MetaDraft) => void
  /** Bài đã từng xuất bản (`publishedAt` có) ⇒ slug khoá (R-CA8). */
  slugLocked: boolean
  /** Lỗi theo đường dẫn: kiểm cục bộ + 400/409/422 từ server (`slug`, `title`, `glossary[1].pinyin`...). */
  errors?: PathErrors
  disabled?: boolean
}

const toInt = (raw: string, fallback: number) => {
  const n = Number(raw)
  return raw.trim() === '' ? fallback : Number.isFinite(n) ? Math.trunc(n) : fallback
}

/**
 * Tab "Thông tin" của trình soạn: tiêu đề, slug (khoá sau xuất bản — helperText nêu lý do), chủ đề, thứ tự, tóm tắt,
 * mục tiêu (mỗi dòng một mục), số phút, từ bổ sung (glossary). Controlled — trang giữ state để so "có thay đổi?".
 */
export function LessonMetaForm({ value, onChange, slugLocked, errors, disabled = false }: LessonMetaFormProps) {
  const set = (patch: Partial<MetaDraft>) => onChange({ ...value, ...patch })
  const setGlossary = (i: number, patch: Partial<GlossaryEntry>) => set({ glossary: replaceAt(value.glossary, i, { ...value.glossary[i]!, ...patch }) })
  const objectivesCount = value.objectivesText.split(/\r?\n/).filter((s) => s.trim()).length

  return (
    <Stack sx={{ gap: 2 }}>
      <TextField
        label="Tiêu đề"
        value={value.title}
        onChange={(e) => set({ title: e.target.value })}
        required
        disabled={disabled}
        error={!!firstError(errors, 'title')}
        helperText={firstError(errors, 'title') ?? '1–200 ký tự'}
        slotProps={{ htmlInput: { maxLength: 200 } }}
      />
      <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start', flexWrap: 'wrap' }}>
        <TextField
          label="Slug (đường dẫn)"
          value={value.slug}
          onChange={(e) => set({ slug: e.target.value })}
          required
          disabled={disabled || slugLocked}
          error={!!firstError(errors, 'slug')}
          helperText={
            firstError(errors, 'slug') ??
            (slugLocked
              ? 'Bài đã từng xuất bản nên slug bị khoá — đổi sẽ gãy liên kết /bai-hoc/<slug> và bộ chữ luyện viết của bài.'
              : 'Chữ thường a–z, số, gạch ngang; 3–64 ký tự. Đường dẫn học viên: /bai-hoc/<slug>')
          }
          sx={{ flex: 1, minWidth: 220 }}
          slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none', spellCheck: false } }}
        />
        {!slugLocked && (
          <Button size="small" onClick={() => set({ slug: slugify(value.title) })} disabled={disabled || !value.title.trim()} sx={{ mt: 1 }}>
            Sinh từ tiêu đề
          </Button>
        )}
      </Box>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '2fr 1fr 1fr' } }}>
        <TextField
          label="Chủ đề"
          value={value.topic}
          onChange={(e) => set({ topic: e.target.value })}
          required
          disabled={disabled}
          error={!!firstError(errors, 'topic')}
          helperText={firstError(errors, 'topic') ?? 'Mã chủ đề, vd giao-tiep, gia-dinh'}
          slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none', spellCheck: false } }}
        />
        <TextField
          label="Thứ tự"
          type="number"
          value={value.orderIndex}
          onChange={(e) => set({ orderIndex: toInt(e.target.value, 0) })}
          required
          disabled={disabled}
          error={!!firstError(errors, 'orderIndex')}
          helperText={firstError(errors, 'orderIndex') ?? '1–999'}
          slotProps={{ htmlInput: { min: 1, max: 999, inputMode: 'numeric' } }}
        />
        <TextField
          label="Số phút"
          type="number"
          value={value.estimatedMinutes}
          onChange={(e) => set({ estimatedMinutes: toInt(e.target.value, 0) })}
          required
          disabled={disabled}
          error={!!firstError(errors, 'estimatedMinutes')}
          helperText={firstError(errors, 'estimatedMinutes') ?? '1–120'}
          slotProps={{ htmlInput: { min: 1, max: 120, inputMode: 'numeric' } }}
        />
      </Box>
      <TextField
        label="Tóm tắt"
        value={value.summary}
        onChange={(e) => set({ summary: e.target.value })}
        multiline
        minRows={2}
        disabled={disabled}
        error={!!firstError(errors, 'summary')}
        helperText={firstError(errors, 'summary') ?? 'Tuỳ chọn, ≤ 1000 ký tự — hiện ở thẻ bài'}
        slotProps={{ htmlInput: { maxLength: 1000 } }}
      />
      <TextField
        label="Mục tiêu (mỗi dòng một mục)"
        value={value.objectivesText}
        onChange={(e) => set({ objectivesText: e.target.value })}
        multiline
        minRows={3}
        disabled={disabled}
        error={!!firstError(errors, 'objectives')}
        helperText={firstError(errors, 'objectives') ?? `${objectivesCount}/${OBJECTIVES_MAX} mục — hiện ở "Sau bài này bạn sẽ…"`}
      />

      <Box>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          Từ bổ sung
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
          Tên riêng, địa danh… chỉ để hiểu bài — không vào ôn tập. Tối đa {GLOSSARY_MAX}.
        </Typography>
        {firstError(errors, 'glossary') && (
          <Typography variant="caption" color="error" sx={{ display: 'block', mb: 1 }}>
            {firstError(errors, 'glossary')}
          </Typography>
        )}
        <Stack sx={{ gap: 1.5 }}>
          {value.glossary.map((g, i) => (
            <Paper key={i} variant="outlined" sx={{ p: 1.5, display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr 1fr auto' }, alignItems: 'start' }}>
              <HanziField
                label="Chữ Hán"
                value={g.hanzi}
                onChange={(v) => setGlossary(i, { hanzi: v })}
                size="small"
                disabled={disabled}
                error={!!firstError(errors, `glossary[${i}].hanzi`)}
                helperText={firstError(errors, `glossary[${i}].hanzi`)}
                slotProps={{ htmlInput: { maxLength: 10 } }}
              />
              <PinyinField
                label="Pinyin"
                value={g.pinyin}
                hanzi={g.hanzi}
                onChange={(v) => setGlossary(i, { pinyin: v })}
                size="small"
                disabled={disabled}
                serverError={firstError(errors, `glossary[${i}].pinyin`)}
              />
              <TextField
                label="Nghĩa"
                value={g.vi}
                onChange={(e) => setGlossary(i, { vi: e.target.value })}
                size="small"
                disabled={disabled}
                error={!!firstError(errors, `glossary[${i}].vi`)}
                helperText={firstError(errors, `glossary[${i}].vi`)}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              <Tooltip title="Xoá từ bổ sung">
                <span>
                  <IconButton size="small" color="error" aria-label={`Xoá từ bổ sung ${i + 1}`} disabled={disabled} onClick={() => set({ glossary: removeAt(value.glossary, i) })} sx={{ mt: 0.5 }}>
                    <DeleteOutlineIcon fontSize="inherit" />
                  </IconButton>
                </span>
              </Tooltip>
            </Paper>
          ))}
        </Stack>
        <Button
          size="small"
          startIcon={<AddIcon />}
          disabled={disabled || value.glossary.length >= GLOSSARY_MAX}
          onClick={() => set({ glossary: [...value.glossary, { hanzi: '', pinyin: '', vi: '' }] })}
          sx={{ mt: 1 }}
        >
          Thêm từ bổ sung
        </Button>
      </Box>
    </Stack>
  )
}
