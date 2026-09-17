import { useEffect, useState } from 'react'
import { Alert, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { Controller, useForm, type Path } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { AppDrawer, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useCreateLanguage, useLanguages, useUpdateLanguage } from '../hooks'
import type { LanguageDto } from '../types'
import { LANGUAGE_STATUS_OPTIONS } from './LanguageCard'

const CODE = /^[a-z][a-z0-9-]{1,31}$/
const APP_URL = /^(https:\/\/\S+|http:\/\/(localhost|127\.0\.0\.1)(:\d+)?(\/\S*)?)$/
const ACCENT = /^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$/

/** Phản chiếu luật BE §5.2.3 (ngôn ngữ). `appUrl` bắt buộc khi `open` kiểm ở client trước để đỡ một vòng 422. */
const schema = z
  .object({
    code: z.string().trim().regex(CODE, 'Chữ thường a-z, số, gạch nối; 2–32 ký tự; bắt đầu bằng chữ'),
    name: z.string().trim().min(1, 'Bắt buộc').max(60, 'Tối đa 60 ký tự'),
    nativeName: z.string().trim().min(1, 'Bắt buộc').max(60, 'Tối đa 60 ký tự'),
    tagline: z.string().trim().max(160, 'Tối đa 160 ký tự'),
    descriptionMarkdown: z.string().max(4000, 'Tối đa 4000 ký tự'),
    status: z.enum(['open', 'coming_soon', 'hidden']),
    appUrl: z
      .string()
      .trim()
      .max(300, 'Tối đa 300 ký tự')
      .refine((v) => v === '' || APP_URL.test(v), 'Phải là https://… (hoặc http://localhost… khi dev)'),
    accentColor: z
      .string()
      .trim()
      .refine((v) => v === '' || ACCENT.test(v), 'Dạng #RRGGBB hoặc #RRGGBBAA'),
  })
  .refine((v) => v.status !== 'open' || v.appUrl !== '', { path: ['appUrl'], message: 'Ngôn ngữ đang mở phải có đường dẫn ứng dụng' })

type FormValues = z.infer<typeof schema>

const EMPTY: FormValues = { code: '', name: '', nativeName: '', tagline: '', descriptionMarkdown: '', status: 'coming_soon', appUrl: '', accentColor: '' }

const toForm = (l: LanguageDto): FormValues => ({
  code: l.code,
  name: l.name,
  nativeName: l.nativeName,
  tagline: l.tagline,
  descriptionMarkdown: l.descriptionMarkdown,
  status: l.status,
  appUrl: l.appUrl ?? '',
  accentColor: l.accentColor ?? '',
})

export interface LanguageDrawerProps {
  /** `null` ⇒ đóng; `'new'` ⇒ tạo; đối tượng ⇒ sửa. */
  target: LanguageDto | 'new' | null
  onClose: () => void
}

/**
 * Ngăn kéo tạo/sửa ngôn ngữ (hợp đồng W3a §5.3.3a): `AppDrawer` không đóng khi bấm ra ngoài. `code` chỉ nhập khi
 * tạo (sửa ⇒ chỉ đọc, "Mã không đổi được sau khi tạo"). `descriptionMarkdown` là TextField nhiều dòng ở W3a (W3b
 * thay `MarkdownEditor`). Lỗi: 409 `CODE_TAKEN` ⇒ ô mã; 422 `APP_URL_REQUIRED` ⇒ ô appUrl; 400 VALIDATION ⇒ map
 * `details`; 409 `CONCURRENCY_CONFLICT` ⇒ Alert + nút "Tải lại bản mới" (không đóng drawer, giữ giá trị đang gõ).
 */
export function LanguageDrawer({ target, onClose }: LanguageDrawerProps) {
  const toast = useToast()
  const create = useCreateLanguage()
  const update = useUpdateLanguage()
  const list = useLanguages()
  const isNew = target === 'new'
  const editing = target && target !== 'new' ? target : null

  // `version` gửi kèm PUT — tách khỏi form để "Tải lại bản mới" chỉ đổi version + giá trị gốc.
  const [version, setVersion] = useState('')
  const [conflict, setConflict] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: EMPTY, mode: 'onBlur' })
  const { control, handleSubmit, reset, setError, formState, watch } = form

  // Mở drawer / đổi đối tượng ⇒ nạp lại form.
  useEffect(() => {
    if (!target) return
    reset(editing ? toForm(editing) : EMPTY)
    setVersion(editing?.version ?? '')
    setConflict(false)
    setServerError(null)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [target])

  const pending = create.isPending || update.isPending || list.isFetching

  /** Sau 409: lấy bản mới nhất từ danh sách (refetch), cập nhật version + giá trị gốc; giữ drawer mở. */
  const reloadLatest = async () => {
    const res = await list.refetch()
    const latest = res.data?.find((l) => l.id === editing?.id)
    if (!latest) {
      setServerError('Ngôn ngữ này đã bị xoá bởi người khác.')
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
    const body = {
      name: values.name,
      nativeName: values.nativeName,
      tagline: values.tagline,
      descriptionMarkdown: values.descriptionMarkdown,
      status: values.status,
      appUrl: values.appUrl || null,
      accentColor: values.accentColor || null,
    }
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: { ...body, version } })
        toast.success(`Đã cập nhật ngôn ngữ ${values.name}`)
      } else {
        await create.mutateAsync({ ...body, code: values.code })
        toast.success(`Đã thêm ngôn ngữ ${values.name}`)
      }
      onClose()
    } catch (err) {
      const parsed = parseApiError(err)
      switch (parsed.code) {
        case 'CODE_TAKEN':
          setError('code', { type: 'server', message: 'Mã này đã được dùng cho ngôn ngữ khác.' })
          return
        case 'APP_URL_REQUIRED':
          setError('appUrl', { type: 'server', message: 'Ngôn ngữ đang mở phải có đường dẫn ứng dụng.' })
          return
        case 'CONCURRENCY_CONFLICT':
          setConflict(true)
          return
        default:
          break
      }
      if (parsed.status === 404) {
        setServerError('Ngôn ngữ này không còn tồn tại (đã bị xoá).')
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

  const status = watch('status')

  return (
    <AppDrawer
      open={target !== null}
      onClose={onClose}
      title={isNew ? 'Thêm ngôn ngữ' : `Sửa ngôn ngữ${editing ? ` · ${editing.name}` : ''}`}
      width={480}
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
      <Stack component="form" spacing={2} noValidate onSubmit={(e) => void handleSubmit(onSubmit)(e)}>
        {conflict && (
          <Alert
            severity="warning"
            action={
              <Button color="inherit" size="small" onClick={() => void reloadLatest()} disabled={list.isFetching}>
                Tải lại bản mới
              </Button>
            }
          >
            Có người vừa sửa ngôn ngữ này. Tải lại bản mới rồi nhập lại thay đổi của bạn.
          </Alert>
        )}
        {serverError && <Alert severity="error">{serverError}</Alert>}

        <Controller
          name="code"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Mã ngôn ngữ"
              fullWidth
              required={isNew}
              disabled={pending || !isNew}
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? (isNew ? 'Vd chinese, english — dùng cho đường dẫn website và khoá nhận tin' : 'Mã không đổi được sau khi tạo')}
              slotProps={{ input: { readOnly: !isNew }, htmlInput: { autoCapitalize: 'none', spellCheck: false, maxLength: 32 } }}
            />
          )}
        />
        <Controller
          name="name"
          control={control}
          render={({ field, fieldState }) => (
            <TextField {...field} label="Tên (tiếng Việt)" fullWidth required disabled={pending} error={!!fieldState.error} helperText={fieldState.error?.message} slotProps={{ htmlInput: { maxLength: 60 } }} />
          )}
        />
        <Controller
          name="nativeName"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Tên bản địa"
              fullWidth
              required
              disabled={pending}
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? 'Vd 中文, English, 日本語'}
              slotProps={{ htmlInput: { maxLength: 60, style: { fontFamily: 'var(--af-font-cjk)' } } }}
            />
          )}
        />
        <Controller
          name="tagline"
          control={control}
          render={({ field, fieldState }) => (
            <TextField {...field} label="Khẩu hiệu ngắn" fullWidth disabled={pending} error={!!fieldState.error} helperText={fieldState.error?.message ?? `${field.value.length}/160 ký tự`} slotProps={{ htmlInput: { maxLength: 160 } }} />
          )}
        />
        <Controller
          name="status"
          control={control}
          render={({ field, fieldState }) => (
            <TextField {...field} select label="Trạng thái" fullWidth disabled={pending} error={!!fieldState.error} helperText={fieldState.error?.message}>
              {Object.entries(LANGUAGE_STATUS_OPTIONS).map(([value, opt]) => (
                <MenuItem key={value} value={value}>
                  {opt.label}
                </MenuItem>
              ))}
            </TextField>
          )}
        />
        <Controller
          name="appUrl"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Đường dẫn ứng dụng"
              type="url"
              fullWidth
              required={status === 'open'}
              disabled={pending}
              placeholder="https://chinese.antfarms.xyz"
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? (status === 'open' ? 'Bắt buộc khi trạng thái "Đang mở"' : 'Tuỳ chọn')}
              slotProps={{ htmlInput: { autoCapitalize: 'none', spellCheck: false, maxLength: 300 } }}
            />
          )}
        />
        <Controller
          name="accentColor"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Màu nhấn"
              fullWidth
              disabled={pending}
              placeholder="#D32F2F"
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? 'Tuỳ chọn, dạng #RRGGBB'}
              slotProps={{
                htmlInput: { autoCapitalize: 'none', spellCheck: false, maxLength: 9 },
                input: {
                  startAdornment: field.value && ACCENT.test(field.value) ? (
                    <Typography component="span" aria-hidden sx={{ width: 16, height: 16, borderRadius: '50%', bgcolor: field.value, mr: 1, border: 1, borderColor: 'divider' }} />
                  ) : undefined,
                },
              }}
            />
          )}
        />
        <Controller
          name="descriptionMarkdown"
          control={control}
          render={({ field, fieldState }) => (
            // W3a: ô Markdown thuần; W3b thay bằng `MarkdownEditor` (có xem trước).
            <TextField
              {...field}
              label="Mô tả (Markdown)"
              fullWidth
              multiline
              minRows={6}
              disabled={pending}
              error={!!fieldState.error}
              helperText={fieldState.error?.message ?? `${field.value.length}/4000 ký tự · hỗ trợ Markdown, không HTML`}
              slotProps={{ htmlInput: { maxLength: 4000 } }}
            />
          )}
        />
        {/* Ảnh bìa (`coverMediaId`) ẩn tới W4 — thư viện ảnh chưa có. */}
      </Stack>
    </AppDrawer>
  )
}
