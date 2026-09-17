import { useState } from 'react'
import { Alert, Box, List, ListItem, Stack, Typography } from '@mui/material'
import { AppAutocomplete } from '@af/ui'
import { Hanzi } from '@/components/Hanzi'
import { numberedToMarked } from '@/lib/pinyin'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useWordSearch } from '@/features/dictionary/hooks'
import { MeaningStatusChip } from '@/features/dictionary/components/MeaningStatusChip'
import type { WordSummary } from '@/features/dictionary/types'
import { moveItem, removeAt, WORDS_MAX, WORDS_WARN_OVER } from '../lib/lessonDraft'
import type { AdminLessonWord } from '../types'
import { ReorderButtons } from './ReorderButtons'

export interface LessonWordsEditorProps {
  words: AdminLessonWord[]
  onChange: (words: AdminLessonWord[]) => void
  disabled?: boolean
  /** Lỗi từ server (422 `UNKNOWN_WORD`, 400). */
  error?: string | null
}

const SEARCH_DEBOUNCE_MS = 300

/**
 * Từ của bài (tab Từ vựng): ô tìm từ điển (`GET /api/dictionary/search`, debounce 300 ms) thêm vào danh sách; danh
 * sách đã chọn có lên/xuống/xoá; cảnh báo > 15 từ (R-CA4 warning), chặn > 30 (giới hạn API).
 * `AppAutocomplete` của `@af/ui` đã trải `params.slotProps` trước trong `renderInput` (bẫy mất ref).
 */
export function LessonWordsEditor({ words, onChange, disabled = false, error }: LessonWordsEditorProps) {
  const [input, setInput] = useState('')
  const q = useDebouncedValue(input.trim(), SEARCH_DEBOUNCE_MS)
  const search = useWordSearch({ q, page: 1, pageSize: 20 })
  const chosen = new Set(words.map((w) => w.id))
  const options = (search.data?.items ?? []).filter((w) => !chosen.has(w.id))
  const full = words.length >= WORDS_MAX

  const add = (w: WordSummary | null) => {
    if (!w || chosen.has(w.id) || full) return
    onChange([...words, { id: w.id, simplified: w.simplified, pinyin: w.pinyin, meaningsVi: w.meaningsVi ?? [], meaningViStatus: w.meaningViStatus }])
    setInput('')
  }

  return (
    <Stack sx={{ gap: 2 }}>
      {error && <Alert severity="error">{error}</Alert>}
      <AppAutocomplete<WordSummary, false, false, false>
        options={options}
        value={null}
        inputValue={input}
        onInputChange={(_e, v, reason) => {
          if (reason === 'reset') return // chọn xong ⇒ tự xoá ô ở `add`
          setInput(v)
        }}
        onChange={(_e, v) => add(v)}
        // Lọc ở server — không để Autocomplete lọc lại theo nhãn (nhãn là chữ Hán, người dùng có thể gõ pinyin/nghĩa).
        filterOptions={(x) => x}
        getOptionLabel={(w) => w.simplified}
        isOptionEqualToValue={(a, b) => a.id === b.id}
        loading={search.isFetching}
        disabled={disabled || full}
        noOptionsText={q ? 'Không tìm thấy từ nào' : 'Gõ chữ Hán, pinyin hoặc nghĩa tiếng Việt'}
        loadingText="Đang tìm…"
        label="Thêm từ vựng"
        placeholder="你好 · ni3 hao3 · xin chào"
        helperText={full ? `Đã đủ ${WORDS_MAX} từ — giới hạn của một bài.` : 'Chọn từ trong từ điển HSK (từ được thêm sẽ thành thẻ ôn khi học viên hoàn thành bài).'}
        textFieldProps={{ autoComplete: 'off', slotProps: { htmlInput: { autoCapitalize: 'none', spellCheck: false } } }}
        renderOption={(props, w) => {
          const { key, ...rest } = props as typeof props & { key: string }
          return (
            <li key={key} {...rest}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, minWidth: 0, width: '100%' }}>
                <Hanzi size="md" sx={{ flexShrink: 0, minWidth: 40 }}>
                  {w.simplified}
                </Hanzi>
                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {numberedToMarked(w.pinyin)}
                    {w.hsk3Level != null && (
                      <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                        HSK {w.hsk3Level}
                      </Typography>
                    )}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" noWrap>
                    {(w.meaningsVi ?? []).slice(0, 2).join('; ') || '—'}
                  </Typography>
                </Box>
              </Box>
            </li>
          )
        }}
      />

      {words.length > WORDS_WARN_OVER && (
        <Alert severity="warning">
          Bài có {words.length} từ — nên giữ ≤ {WORDS_WARN_OVER} từ để học viên không quá tải (xuất bản vẫn được, chỉ cảnh báo).
        </Alert>
      )}

      {words.length === 0 ? (
        <Typography color="text.secondary">Bài chưa có từ vựng. Cần ít nhất 1 từ để xuất bản.</Typography>
      ) : (
        <List disablePadding sx={{ bgcolor: 'background.paper', borderRadius: 1, border: 1, borderColor: 'divider' }}>
          {words.map((w, i) => (
            <ListItem key={w.id} divider sx={{ gap: 1.5, px: { xs: 1, sm: 2 }, minHeight: 64 }}>
              <Typography variant="caption" color="text.secondary" sx={{ width: 20, flexShrink: 0, textAlign: 'right' }}>
                {i + 1}
              </Typography>
              <Hanzi size="md" sx={{ fontSize: 28, flexShrink: 0, minWidth: 44, textAlign: 'center' }}>
                {w.simplified}
              </Hanzi>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {numberedToMarked(w.pinyin)}
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, minWidth: 0 }}>
                  <Typography variant="body2" color="text.secondary" noWrap sx={{ minWidth: 0 }}>
                    {(w.meaningsVi ?? []).slice(0, 2).join('; ') || '—'}
                  </Typography>
                  <MeaningStatusChip status={w.meaningViStatus} sx={{ flexShrink: 0 }} />
                </Box>
              </Box>
              <Box sx={{ display: 'flex', flexShrink: 0 }}>
                <ReorderButtons
                  index={i}
                  count={words.length}
                  itemLabel="từ"
                  disabled={disabled}
                  onMove={(from, to) => onChange(moveItem(words, from, to))}
                  onRemove={() => onChange(removeAt(words, i))}
                />
              </Box>
            </ListItem>
          ))}
        </List>
      )}
      <Typography variant="caption" color="text.secondary">
        {words.length}/{WORDS_MAX} từ · thứ tự trong danh sách là thứ tự hiện ở bài.
      </Typography>
    </Stack>
  )
}
