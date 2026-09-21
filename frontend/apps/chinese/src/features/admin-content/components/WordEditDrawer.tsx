import { useEffect, useState } from 'react'
import { Alert, Box, Button, Chip, FormControlLabel, IconButton, Radio, RadioGroup, Skeleton, Stack, TextField, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import { AppDrawer, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { Hanzi, SpeakButton, numberedToMarked, meaningViSourceLabel } from '@af/chinese-kit'
import type { HanVietStatus, MeaningViStatus } from '@af/chinese-kit'
import { useAdminWord, useUpdateWord } from '../hooks'
import { removeAt, replaceAt } from '../lib/lessonDraft'
import { MEANINGS_MAX, meaningsChanged, normalizeHanViet, normalizeMeanings, validateWordEdit, type WordEditProblems } from '../lib/wordReview'
import { flattenValidationDetails } from '../lib/validationErrors'
import type { AdminWord } from '../types'

export interface WordEditDrawerProps {
  /** Id từ đang sửa (`?sua=`) — `null` ⇒ đóng. */
  wordId: string | null
  /** Bản từ danh sách (nếu có) để mở không chờ; drawer vẫn tải bản mới nhất lấy `version`. */
  initial?: AdminWord
  onClose: () => void
}

interface FormState {
  /** `version` của bản dùng để dựng form — gửi đúng giá trị này (không lấy version của bản chi tiết tải về sau). */
  baseVersion: number
  meanings: string[]
  hanViet: string
  meaningViStatus: MeaningViStatus
  hanVietStatus: HanVietStatus
}

const formFromWord = (w: AdminWord): FormState => ({
  baseVersion: w.version,
  meanings: w.meaningsVi?.length ? [...w.meaningsVi] : [''],
  hanViet: w.hanViet ?? '',
  meaningViStatus: w.meaningViStatus,
  hanVietStatus: w.hanVietStatus ?? 'derived',
})

/**
 * Ngăn kéo sửa nghĩa một từ (R-CA9): nghĩa Việt (danh sách dòng thêm/xoá), Hán Việt, trạng thái nghĩa/Hán Việt (chọn
 * tường minh), nghĩa tiếng Anh chỉ đọc để đối chiếu; "Lưu" giữ trạng thái đã chọn, "Lưu & đánh dấu đã duyệt" đặt cả
 * hai `reviewed`. 409 ⇒ Alert + nút tải lại (bỏ thay đổi). `AppDrawer` không đóng khi bấm ra ngoài (có thao tác ghi).
 */
export function WordEditDrawer({ wordId, initial, onClose }: WordEditDrawerProps) {
  const toast = useToast()
  const detail = useAdminWord(wordId, initial)
  const word = detail.data ?? initial ?? null
  const update = useUpdateWord()
  const [form, setForm] = useState<FormState | null>(null)
  const [touched, setTouched] = useState(false)
  const [problems, setProblems] = useState<WordEditProblems>({})
  const [serverError, setServerError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)

  // Đổi từ ⇒ reset; bản mới nhất về mà chưa đụng vào ⇒ đồng bộ.
  useEffect(() => {
    setTouched(false)
    setProblems({})
    setServerError(null)
    setConflict(false)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [wordId])
  useEffect(() => {
    if (!word) return
    if (!touched) setForm(formFromWord(word))
  }, [word, touched])

  const edit = (patch: Partial<FormState>) => {
    setTouched(true)
    setServerError(null)
    setForm((prev) => (prev ? { ...prev, ...patch } : prev))
  }

  const save = async (markReviewed: boolean) => {
    if (!word || !form) return
    const meaningsVi = normalizeMeanings(form.meanings)
    const hanViet = normalizeHanViet(form.hanViet)
    const local = validateWordEdit(meaningsVi, hanViet)
    setProblems(local)
    setServerError(null)
    if (local.meaningsVi || local.hanViet) return
    const body = {
      version: form.baseVersion,
      meaningsVi,
      meaningViStatus: markReviewed ? ('reviewed' as const) : form.meaningViStatus,
      hanViet,
      hanVietStatus: markReviewed ? ('reviewed' as const) : form.hanVietStatus,
    }
    try {
      await update.mutateAsync({ id: word.id, body })
      toast.success(markReviewed ? `Đã lưu và duyệt ${word.simplified}` : `Đã lưu ${word.simplified}`)
      setTouched(false)
      onClose()
    } catch (err) {
      const p = parseApiError(err)
      if (p.status === 409) setConflict(true)
      else if (p.status === 404) setServerError('Từ này không còn tồn tại.')
      else if (p.status === 400) {
        const fields = flattenValidationDetails(p.details)
        setProblems({ meaningsVi: fields.meaningsVi?.[0], hanViet: fields.hanViet?.[0] })
        if (!fields.meaningsVi && !fields.hanViet) setServerError(p.message)
      } else setServerError(p.message)
    }
  }

  const reload = async () => {
    setTouched(false)
    setConflict(false)
    const r = await detail.refetch()
    if (r.data) setForm(formFromWord(r.data))
  }

  const detailError = detail.error ? parseApiError(detail.error) : null
  const changed = !!word && !!form && (meaningsChanged(form.meanings, word.meaningsVi) || (normalizeHanViet(form.hanViet) ?? '') !== (word.hanViet ?? '') || form.meaningViStatus !== word.meaningViStatus || form.hanVietStatus !== (word.hanVietStatus ?? 'derived'))
  const sourceLabel = meaningViSourceLabel(word?.meaningViSource)

  return (
    <AppDrawer
      open={wordId !== null}
      onClose={onClose}
      title="Sửa nghĩa"
      width={480}
      actions={
        <>
          <Button color="inherit" onClick={onClose} disabled={update.isPending}>
            Đóng
          </Button>
          <Button variant="outlined" onClick={() => void save(false)} loading={update.isPending} disabled={!form || !word || conflict || !changed}>
            Lưu
          </Button>
          <Button variant="contained" color="success" onClick={() => void save(true)} loading={update.isPending} disabled={!form || !word || conflict}>
            Lưu &amp; đã duyệt
          </Button>
        </>
      }
    >
      {!word || !form ? (
        detailError ? (
          <Alert severity="error">{detailError.status === 404 ? 'Từ này không còn tồn tại.' : detailError.message}</Alert>
        ) : (
          <Stack spacing={1.5}>
            <Skeleton variant="rounded" height={72} />
            <Skeleton variant="rounded" height={120} />
          </Stack>
        )
      ) : (
        <Stack spacing={2}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
            <Hanzi size="xl" sx={{ fontSize: 44 }}>
              {word.simplified}
            </Hanzi>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="h6" component="p" sx={{ fontWeight: 500 }} title={word.pinyin}>
                {numberedToMarked(word.pinyin)}
              </Typography>
              <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                {word.hsk3Level != null && <Chip size="small" variant="outlined" label={`HSK ${word.hsk3Level}`} />}
                {word.pathOrder != null && <Chip size="small" variant="outlined" label={`Lộ trình #${word.pathOrder}`} />}
                {sourceLabel && <Chip size="small" variant="outlined" label={sourceLabel} />}
              </Box>
            </Box>
            <SpeakButton text={word.simplified} size="medium" ariaLabel="Nghe" />
          </Box>

          {detailError && detailError.status !== 404 && <Alert severity="warning">Không tải được bản mới nhất ({detailError.message}) — đang dùng dữ liệu từ danh sách.</Alert>}
          {conflict && (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={() => void reload()}>
                  Tải lại
                </Button>
              }
            >
              Từ này đã bị sửa ở nơi khác — tải lại để lấy bản mới (thay đổi của bạn sẽ bỏ).
            </Alert>
          )}
          {serverError && <Alert severity="error">{serverError}</Alert>}

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              Nghĩa tiếng Anh (đối chiếu)
            </Typography>
            {(word.meaningsEn ?? []).length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                Không có.
              </Typography>
            ) : (
              <Box component="ol" lang="en" sx={{ pl: 3, my: 0, '& li': { fontSize: 14 } }}>
                {(word.meaningsEn ?? []).map((m, i) => (
                  <li key={i}>{m}</li>
                ))}
              </Box>
            )}
          </Box>

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              Nghĩa tiếng Việt
            </Typography>
            {problems.meaningsVi && (
              <Typography variant="caption" color="error" sx={{ display: 'block', mb: 0.5 }}>
                {problems.meaningsVi}
              </Typography>
            )}
            <Stack spacing={1}>
              {form.meanings.map((m, i) => (
                <Box key={i} sx={{ display: 'flex', gap: 0.5, alignItems: 'flex-start' }}>
                  <TextField
                    value={m}
                    onChange={(e) => edit({ meanings: replaceAt(form.meanings, i, e.target.value) })}
                    size="small"
                    fullWidth
                    label={`Nghĩa ${i + 1}`}
                    disabled={conflict || update.isPending}
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                  />
                  <Tooltip title="Xoá nghĩa">
                    <span>
                      <IconButton size="small" color="error" aria-label={`Xoá nghĩa ${i + 1}`} disabled={form.meanings.length <= 1 || conflict} onClick={() => edit({ meanings: removeAt(form.meanings, i) })} sx={{ mt: 0.5 }}>
                        <DeleteOutlineIcon fontSize="inherit" />
                      </IconButton>
                    </span>
                  </Tooltip>
                </Box>
              ))}
            </Stack>
            <Button size="small" startIcon={<AddIcon />} disabled={form.meanings.length >= MEANINGS_MAX || conflict} onClick={() => edit({ meanings: [...form.meanings, ''] })} sx={{ mt: 0.5 }}>
              Thêm nghĩa
            </Button>
          </Box>

          <TextField
            label="Hán Việt"
            value={form.hanViet}
            onChange={(e) => edit({ hanViet: e.target.value })}
            size="small"
            fullWidth
            disabled={conflict || update.isPending}
            error={!!problems.hanViet}
            helperText={problems.hanViet ?? 'Chữ thường có dấu, cách nhau một dấu cách (vd "nhĩ hảo"). Để trống nếu không có.'}
            slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none' } }}
          />

          <Box sx={{ display: 'grid', gap: 1, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
            <Box>
              <Typography variant="subtitle2">Trạng thái nghĩa</Typography>
              <RadioGroup value={form.meaningViStatus} onChange={(_e, v) => edit({ meaningViStatus: v as MeaningViStatus })}>
                <FormControlLabel value="machine" control={<Radio size="small" />} label="Chưa duyệt (dịch máy)" disabled={conflict} />
                <FormControlLabel value="reviewed" control={<Radio size="small" />} label="Đã duyệt" disabled={conflict} />
              </RadioGroup>
            </Box>
            <Box>
              <Typography variant="subtitle2">Trạng thái Hán Việt</Typography>
              <RadioGroup value={form.hanVietStatus} onChange={(_e, v) => edit({ hanVietStatus: v as HanVietStatus })}>
                <FormControlLabel value="derived" control={<Radio size="small" />} label="Suy ra từ từng chữ" disabled={conflict} />
                <FormControlLabel value="reviewed" control={<Radio size="small" />} label="Đã duyệt" disabled={conflict} />
              </RadioGroup>
            </Box>
          </Box>

          <Typography variant="caption" color="text.secondary">
            Đổi nội dung nghĩa ⇒ nguồn thành "sửa tay"; mọi lần lưu đều bảo vệ từ này khỏi bị lần nạp học liệu sau ghi đè.
            {word.editedAt ? ` Sửa gần nhất${word.editedByName ? ` bởi ${word.editedByName}` : ''}.` : ''}
          </Typography>
        </Stack>
      )}
    </AppDrawer>
  )
}
