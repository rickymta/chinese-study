import { useEffect, useState } from 'react'
import { Alert, Button, Stack, TextField } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { AppDialog, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useCreateLesson } from '../hooks'
import { TOPIC_RE } from '../lib/metaValidation'
import { slugify, slugProblem } from '../lib/slug'
import { flattenValidationDetails, firstError, type PathErrors } from '../lib/validationErrors'

export interface CreateLessonDialogProps {
  open: boolean
  onClose: () => void
}

/**
 * Tạo bài mới (`POST /api/admin/lessons` ⇒ `draft`, `source=admin`): tiêu đề, slug (tự sinh từ tiêu đề tới khi người
 * dùng sửa tay), chủ đề, thứ tự (bỏ trống ⇒ cuối). Thành công ⇒ sang trang soạn. 409 `SLUG_TAKEN` ⇒ lỗi dưới ô slug.
 * `AppDialog` không đóng khi bấm ra ngoài (có thao tác ghi).
 */
export function CreateLessonDialog({ open, onClose }: CreateLessonDialogProps) {
  const navigate = useNavigate()
  const toast = useToast()
  const create = useCreateLesson()
  const [title, setTitle] = useState('')
  const [slug, setSlug] = useState('')
  const [slugTouched, setSlugTouched] = useState(false)
  const [topic, setTopic] = useState('giao-tiep')
  const [orderIndex, setOrderIndex] = useState('')
  const [errors, setErrors] = useState<PathErrors>({})
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setTitle('')
    setSlug('')
    setSlugTouched(false)
    setTopic('giao-tiep')
    setOrderIndex('')
    setErrors({})
    setFormError(null)
    create.reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const onTitle = (v: string) => {
    setTitle(v)
    if (!slugTouched) setSlug(slugify(v))
  }

  const submit = async () => {
    const local: PathErrors = {}
    if (!title.trim()) local.title = ['Tiêu đề không được để trống.']
    const s = slugProblem(slug.trim())
    if (s) local.slug = [s]
    if (!TOPIC_RE.test(topic.trim())) local.topic = ['Chủ đề: 2–64 ký tự, chữ thường a–z, số, gạch ngang.']
    const order = orderIndex.trim() === '' ? undefined : Number(orderIndex)
    if (order !== undefined && (!Number.isInteger(order) || order < 1 || order > 999)) local.orderIndex = ['Thứ tự là số nguyên 1–999.']
    setErrors(local)
    setFormError(null)
    if (Object.keys(local).length > 0) return
    try {
      const lesson = await create.mutateAsync({ title: title.trim(), slug: slug.trim(), topic: topic.trim(), ...(order !== undefined ? { orderIndex: order } : {}) })
      toast.success('Đã tạo bài nháp')
      onClose()
      navigate(`/quan-tri/bai-hoc/${lesson.id}`)
    } catch (err) {
      const parsed = parseApiError(err)
      if (parsed.code === 'SLUG_TAKEN') setErrors({ slug: ['Slug này đã có bài khác dùng — chọn slug khác.'] })
      else if (parsed.status === 400) {
        const details = flattenValidationDetails(parsed.details)
        setErrors(details)
        if (Object.keys(details).length === 0) setFormError(parsed.message)
      } else setFormError(parsed.message)
    }
  }

  return (
    <AppDialog
      open={open}
      onClose={onClose}
      title="Tạo bài học mới"
      actions={
        <>
          <Button color="inherit" onClick={onClose} disabled={create.isPending}>
            Huỷ
          </Button>
          <Button variant="contained" onClick={() => void submit()} loading={create.isPending}>
            Tạo bài nháp
          </Button>
        </>
      }
    >
      <Stack spacing={2} sx={{ pt: 0.5 }}>
        {formError && <Alert severity="error">{formError}</Alert>}
        <TextField
          label="Tiêu đề"
          value={title}
          onChange={(e) => onTitle(e.target.value)}
          required
          autoFocus
          fullWidth
          error={!!firstError(errors, 'title')}
          helperText={firstError(errors, 'title') ?? '1–200 ký tự'}
          slotProps={{ htmlInput: { maxLength: 200 } }}
        />
        <TextField
          label="Slug (đường dẫn)"
          value={slug}
          onChange={(e) => {
            setSlugTouched(true)
            setSlug(e.target.value)
          }}
          required
          fullWidth
          error={!!firstError(errors, 'slug')}
          helperText={firstError(errors, 'slug') ?? 'Tự sinh từ tiêu đề, sửa được. Khoá lại sau lần xuất bản đầu tiên.'}
          slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none', spellCheck: false } }}
        />
        <TextField
          label="Chủ đề"
          value={topic}
          onChange={(e) => setTopic(e.target.value)}
          required
          fullWidth
          error={!!firstError(errors, 'topic')}
          helperText={firstError(errors, 'topic') ?? 'Mã chủ đề, vd giao-tiep, gia-dinh, so-dem'}
          slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none', spellCheck: false } }}
        />
        <TextField
          label="Thứ tự"
          type="number"
          value={orderIndex}
          onChange={(e) => setOrderIndex(e.target.value)}
          fullWidth
          error={!!firstError(errors, 'orderIndex')}
          helperText={firstError(errors, 'orderIndex') ?? 'Bỏ trống ⇒ xếp cuối danh sách'}
          slotProps={{ htmlInput: { min: 1, max: 999, inputMode: 'numeric' } }}
        />
      </Stack>
    </AppDialog>
  )
}
