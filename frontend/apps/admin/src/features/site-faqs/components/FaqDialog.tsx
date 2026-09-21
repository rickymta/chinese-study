import { useEffect, useState } from 'react'
import { Alert, Autocomplete, Button, FormControlLabel, Stack, Switch, TextField } from '@mui/material'
import { Controller, useForm, type Path } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { AppDialog, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { MarkdownEditor } from '@/components/MarkdownEditor'
import { useCreateFaq, useFaqs, useUpdateFaq } from '../hooks'
import { DEFAULT_GROUP_KEY, type FaqDto } from '../types'

const GROUP_KEY = /^[a-z0-9-]{1,32}$/

/** Phản chiếu luật BE §5.2.3 W3b: question 1–300, answerMarkdown 1–10000, groupKey `^[a-z0-9-]{1,32}$`. */
const schema = z.object({
  question: z.string().trim().min(1, 'Bắt buộc').max(300, 'Tối đa 300 ký tự'),
  groupKey: z.string().trim().regex(GROUP_KEY, 'Chữ thường a-z, số, gạch nối; 1–32 ký tự'),
  answerMarkdown: z.string().trim().min(1, 'Bắt buộc').max(10000, 'Tối đa 10000 ký tự'),
  isPublished: z.boolean(),
})

type FormValues = z.infer<typeof schema>

const EMPTY: FormValues = { question: '', groupKey: DEFAULT_GROUP_KEY, answerMarkdown: '', isPublished: false }

const toForm = (f: FaqDto): FormValues => ({ question: f.question, groupKey: f.groupKey, answerMarkdown: f.answerMarkdown, isPublished: f.isPublished })

export interface FaqDialogProps {
  /** `null` ⇒ đóng; `'new'` ⇒ tạo; đối tượng ⇒ sửa. */
  target: FaqDto | 'new' | null
  /** Nhóm mặc định khi tạo (bấm "Thêm" ngay trong một nhóm). */
  defaultGroupKey?: string
  /** Gợi ý nhóm đã có cho Autocomplete `freeSolo`. */
  groupKeys: string[]
  onClose: () => void
}

/**
 * Hộp thoại tạo/sửa FAQ (hợp đồng W3b §5.3.3a): `AppDialog maxWidth="md"`, toàn màn dưới `sm`. `groupKey` là
 * Autocomplete `freeSolo` gợi ý nhóm đã có (trải `params.slotProps` trước khi ghi đè — CLAUDE.md). Câu trả lời dùng
 * `MarkdownEditor`. 409 `CONCURRENCY_CONFLICT` ⇒ Alert + "Tải lại bản mới" (không đóng, giữ giá trị đang gõ).
 */
export function FaqDialog({ target, defaultGroupKey, groupKeys, onClose }: FaqDialogProps) {
  const toast = useToast()
  const create = useCreateFaq()
  const update = useUpdateFaq()
  const list = useFaqs()
  const isNew = target === 'new'
  const editing = target && target !== 'new' ? target : null

  const [version, setVersion] = useState('')
  const [conflict, setConflict] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: EMPTY, mode: 'onBlur' })
  const { control, handleSubmit, reset, setError, formState } = form

  useEffect(() => {
    if (!target) return
    reset(editing ? toForm(editing) : { ...EMPTY, groupKey: defaultGroupKey || DEFAULT_GROUP_KEY })
    setVersion(editing?.version ?? '')
    setConflict(false)
    setServerError(null)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [target])

  const pending = create.isPending || update.isPending || list.isFetching

  const reloadLatest = async () => {
    const res = await list.refetch()
    const latest = res.data?.find((f) => f.id === editing?.id)
    if (!latest) {
      setServerError('Câu hỏi này đã bị xoá bởi người khác.')
      return
    }
    reset(toForm(latest))
    setVersion(latest.version)
    setConflict(false)
    setServerError(null)
  }

  const onSubmit = async (values: FormValues) => {
    setServerError(null)
    setConflict(false)
    const body = { question: values.question, answerMarkdown: values.answerMarkdown, groupKey: values.groupKey, isPublished: values.isPublished }
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: { ...body, version } })
        toast.success('Đã cập nhật câu hỏi')
      } else {
        await create.mutateAsync(body)
        toast.success('Đã thêm câu hỏi')
      }
      onClose()
    } catch (err) {
      const parsed = parseApiError(err)
      if (parsed.code === 'CONCURRENCY_CONFLICT') {
        setConflict(true)
        return
      }
      if (parsed.status === 404) {
        setServerError('Câu hỏi này không còn tồn tại (đã bị xoá).')
        return
      }
      let mapped = false
      if (parsed.fieldErrors) {
        for (const [key, messages] of Object.entries(parsed.fieldErrors)) {
          if (key in EMPTY) {
            setError(key as Path<FormValues>, { type: 'server', message: messages.join(' ') })
            mapped = true
          }
        }
      }
      if (!mapped) setServerError(parsed.message)
    }
  }

  return (
    <AppDialog
      open={target !== null}
      onClose={onClose}
      title={isNew ? 'Thêm câu hỏi' : 'Sửa câu hỏi'}
      maxWidth="md"
      fullWidth
      fullScreenBelow="sm"
      actions={
        <>
          <Button color="inherit" onClick={onClose} disabled={pending}>
            Huỷ
          </Button>
          <Button variant="contained" onClick={() => void handleSubmit(onSubmit)()} loading={create.isPending || update.isPending} disabled={pending || (!isNew && !formState.isDirty)}>
            {isNew ? 'Thêm' : 'Lưu'}
          </Button>
        </>
      }
    >
      <Stack component="form" spacing={2} noValidate onSubmit={(e) => void handleSubmit(onSubmit)(e)} sx={{ pt: 0.5 }}>
        {conflict && (
          <Alert
            severity="warning"
            action={
              <Button color="inherit" size="small" onClick={() => void reloadLatest()} disabled={list.isFetching}>
                Tải lại bản mới
              </Button>
            }
          >
            Có người vừa sửa câu hỏi này. Tải lại bản mới rồi nhập lại thay đổi của bạn.
          </Alert>
        )}
        {serverError && <Alert severity="error">{serverError}</Alert>}

        <Controller
          name="question"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Câu hỏi"
              fullWidth
              required
              autoFocus={isNew}
              disabled={pending}
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? `${field.value.length}/300 ký tự`}
              slotProps={{ htmlInput: { maxLength: 300 } }}
            />
          )}
        />
        <Controller
          name="groupKey"
          control={control}
          render={({ field, fieldState }) => (
            <Autocomplete
              freeSolo
              options={groupKeys}
              value={field.value}
              inputValue={field.value}
              onInputChange={(_e, v) => field.onChange(v.toLowerCase())}
              onChange={(_e, v) => field.onChange((v ?? '').toLowerCase())}
              onBlur={field.onBlur}
              disabled={pending}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Nhóm"
                  required
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message ?? 'Mã nhóm (vd general, hoc-phi, tai-khoan) — website gom câu hỏi theo nhóm'}
                  // Trải `params.slotProps` TRƯỚC rồi mới ghi đè slot con — đè nguyên `slotProps` làm mất ref (CLAUDE.md).
                  slotProps={{
                    ...params.slotProps,
                    htmlInput: { ...params.slotProps.htmlInput, maxLength: 32, autoCapitalize: 'none', spellCheck: false },
                  }}
                />
              )}
            />
          )}
        />
        <Controller
          name="answerMarkdown"
          control={control}
          render={({ field, fieldState }) => (
            <MarkdownEditor
              name={field.name}
              value={field.value}
              onChange={field.onChange}
              onBlur={field.onBlur}
              label="Câu trả lời (Markdown) *"
              minRows={10}
              maxLength={10000}
              disabled={pending}
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? `${field.value.length}/10000 ký tự · hỗ trợ Markdown, không HTML`}
            />
          )}
        />
        <Controller
          name="isPublished"
          control={control}
          render={({ field }) => (
            <FormControlLabel
              control={<Switch checked={field.value} onChange={(e) => field.onChange(e.target.checked)} disabled={pending} />}
              label={field.value ? 'Đã xuất bản — hiện trên website' : 'Nháp — chưa hiện trên website'}
            />
          )}
        />
      </Stack>
    </AppDialog>
  )
}
