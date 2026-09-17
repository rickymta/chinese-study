import { useState } from 'react'
import { Alert, Button, Skeleton, Stack, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { PageContainer, useConfirm, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { useDeleteLanguage, useLanguages, useReorderLanguages } from '../hooks'
import type { LanguageDto } from '../types'
import { LanguageCard } from '../components/LanguageCard'
import { LanguageDrawer } from '../components/LanguageDrawer'

/**
 * `/website/ngon-ngu` (hợp đồng W3a §5.3.3a, cần `cms:site.manage`): danh sách thẻ theo `sortOrder`, lên/xuống gọi
 * `PUT order` với TOÀN BỘ id (422 `ORDER_MISMATCH` ⇒ báo + tải lại), thêm/sửa qua `LanguageDrawer`, xoá qua
 * `useConfirm`. Không có nút ẩn theo quyền trong trang — quyền kiểm ở route + mục menu.
 */
export function LanguagesPage() {
  const toast = useToast()
  const confirm = useConfirm()
  const languages = useLanguages()
  const reorder = useReorderLanguages()
  const remove = useDeleteLanguage()
  const [drawer, setDrawer] = useState<LanguageDto | 'new' | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const items = languages.data ?? []
  const busy = reorder.isPending || remove.isPending

  const move = async (from: number, to: number) => {
    if (to < 0 || to >= items.length) return
    const next = [...items]
    const [moved] = next.splice(from, 1)
    next.splice(to, 0, moved!)
    setActionError(null)
    try {
      await reorder.mutateAsync(next)
    } catch (err) {
      const parsed = parseApiError(err)
      setActionError(
        parsed.code === 'ORDER_MISMATCH'
          ? 'Danh sách ngôn ngữ vừa thay đổi (có người thêm/xoá). Đã tải lại — vui lòng sắp xếp lại.'
          : `Không sắp xếp được: ${parsed.message}`,
      )
    }
  }

  const onDelete = async (language: LanguageDto) => {
    const ok = await confirm({
      title: `Xoá ngôn ngữ "${language.name}"?`,
      message: 'Website sẽ không còn hiện ngôn ngữ này. Thao tác không hoàn tác được.',
      confirmText: 'Xoá',
      tone: 'danger',
    })
    if (!ok) return
    setActionError(null)
    try {
      await remove.mutateAsync(language.id)
      toast.success(`Đã xoá ngôn ngữ ${language.name}`)
    } catch (err) {
      const parsed = parseApiError(err)
      if (parsed.status === 404) {
        toast.info('Ngôn ngữ này đã bị xoá trước đó.')
        void languages.refetch()
      } else setActionError(`Không xoá được: ${parsed.message}`)
    }
  }

  const err = languages.error ? parseApiError(languages.error) : null

  return (
    <PageContainer
      title="Ngôn ngữ"
      maxWidth={800}
      actions={
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDrawer('new')} disabled={languages.isPending}>
          Thêm ngôn ngữ
        </Button>
      }
    >
      <Stack spacing={2}>
        <Typography variant="body2" color="text.secondary">
          Thứ tự ở đây là thứ tự hiển thị trên website. Ngôn ngữ "Ẩn" không xuất hiện trên website; "Sắp ra mắt" hiện
          nhưng không có nút vào học.
        </Typography>

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void languages.refetch()}>
                Thử lại
              </Button>
            }
          >
            Không tải được danh sách: {err.message}
          </Alert>
        )}
        {actionError && (
          <Alert severity="error" onClose={() => setActionError(null)}>
            {actionError}
          </Alert>
        )}

        {languages.isPending ? (
          <Stack spacing={1.5}>
            <Skeleton variant="rounded" height={110} />
            <Skeleton variant="rounded" height={110} />
            <Skeleton variant="rounded" height={110} />
          </Stack>
        ) : items.length === 0 ? (
          <Alert severity="info">Chưa có ngôn ngữ nào. Bấm "Thêm ngôn ngữ" để tạo.</Alert>
        ) : (
          <Stack spacing={1.5}>
            {items.map((l, i) => (
              <LanguageCard key={l.id} language={l} index={i} count={items.length} disabled={busy} onMove={(f, t) => void move(f, t)} onEdit={setDrawer} onDelete={(x) => void onDelete(x)} />
            ))}
          </Stack>
        )}
      </Stack>

      <LanguageDrawer target={drawer} onClose={() => setDrawer(null)} />
    </PageContainer>
  )
}
