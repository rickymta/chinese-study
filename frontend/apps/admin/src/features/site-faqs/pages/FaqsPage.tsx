import { useMemo, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, Divider, IconButton, Skeleton, Stack, Tooltip, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import { PageContainer, useConfirm, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { ReorderButtons } from '@/components/ReorderButtons'
import { StatusChip, type StatusChipOption } from '@/components/StatusChip'
import { useDeleteFaq, useFaqs, useReorderFaqs } from '../hooks'
import type { FaqDto } from '../types'
import { FaqDialog } from '../components/FaqDialog'

const PUBLISH_OPTIONS: Record<string, StatusChipOption> = {
  published: { label: 'Đã xuất bản', color: 'success' },
  draft: { label: 'Nháp', color: 'default' },
}

/**
 * `/website/faq` (hợp đồng W3b §5.3.3a, cần `cms:site.manage`): FAQ gom theo `groupKey`, mỗi dòng: câu hỏi, chip
 * xuất bản/nháp, lên/xuống TRONG nhóm (`PUT order { groupKey, ids }` đủ id nhóm — 422 `ORDER_MISMATCH` ⇒ báo + tải
 * lại), sửa/xoá. Thêm/sửa qua `FaqDialog`. Không có nút ẩn theo quyền trong trang — quyền kiểm ở route + menu.
 */
export function FaqsPage() {
  const toast = useToast()
  const confirm = useConfirm()
  const faqs = useFaqs()
  const reorder = useReorderFaqs()
  const remove = useDeleteFaq()
  const [dialog, setDialog] = useState<FaqDto | 'new' | null>(null)
  const [newGroup, setNewGroup] = useState<string | undefined>(undefined)
  const [actionError, setActionError] = useState<string | null>(null)

  const items = faqs.data ?? []
  const busy = reorder.isPending || remove.isPending

  // Gom theo nhóm, giữ thứ tự nhóm theo tên + sortOrder trong nhóm (api đã sắp).
  const groups = useMemo(() => {
    const map = new Map<string, FaqDto[]>()
    for (const f of items) {
      const arr = map.get(f.groupKey) ?? []
      arr.push(f)
      map.set(f.groupKey, arr)
    }
    return [...map.entries()]
  }, [items])
  const groupKeys = useMemo(() => groups.map(([k]) => k), [groups])

  const move = async (groupKey: string, list: FaqDto[], from: number, to: number) => {
    if (to < 0 || to >= list.length) return
    const next = [...list]
    const [moved] = next.splice(from, 1)
    next.splice(to, 0, moved!)
    setActionError(null)
    try {
      await reorder.mutateAsync({ groupKey, ordered: next })
    } catch (err) {
      const parsed = parseApiError(err)
      setActionError(
        parsed.code === 'ORDER_MISMATCH'
          ? 'Nhóm câu hỏi vừa thay đổi (có người thêm/xoá). Đã tải lại — vui lòng sắp xếp lại.'
          : `Không sắp xếp được: ${parsed.message}`,
      )
    }
  }

  const onDelete = async (faq: FaqDto) => {
    const ok = await confirm({
      title: 'Xoá câu hỏi này?',
      message: `"${faq.question}" sẽ không còn hiện trên website. Thao tác không hoàn tác được.`,
      confirmText: 'Xoá',
      tone: 'danger',
    })
    if (!ok) return
    setActionError(null)
    try {
      await remove.mutateAsync(faq.id)
      toast.success('Đã xoá câu hỏi')
    } catch (err) {
      const parsed = parseApiError(err)
      if (parsed.status === 404) {
        toast.info('Câu hỏi này đã bị xoá trước đó.')
        void faqs.refetch()
      } else setActionError(`Không xoá được: ${parsed.message}`)
    }
  }

  const openNew = (groupKey?: string) => {
    setNewGroup(groupKey)
    setDialog('new')
  }

  const err = faqs.error ? parseApiError(faqs.error) : null

  return (
    <PageContainer
      title="Câu hỏi thường gặp"
      maxWidth={900}
      actions={
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => openNew()} disabled={faqs.isPending}>
          Thêm câu hỏi
        </Button>
      }
    >
      <Stack spacing={2}>
        <Typography variant="body2" color="text.secondary">
          Website gom câu hỏi theo nhóm và hiện theo thứ tự ở đây. Chỉ câu hỏi "Đã xuất bản" mới hiện trên website.
        </Typography>

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void faqs.refetch()}>
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

        {faqs.isPending ? (
          <Stack spacing={1.5}>
            <Skeleton variant="rounded" height={80} />
            <Skeleton variant="rounded" height={80} />
          </Stack>
        ) : items.length === 0 ? (
          <Alert severity="info">Chưa có câu hỏi nào. Bấm "Thêm câu hỏi" để tạo.</Alert>
        ) : (
          groups.map(([groupKey, list]) => (
            <Card key={groupKey} variant="outlined">
              <CardContent sx={{ '&:last-child': { pb: 1 } }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 1 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                    Nhóm <code>{groupKey}</code>{' '}
                    <Typography component="span" variant="caption" color="text.secondary">
                      ({list.length})
                    </Typography>
                  </Typography>
                  <Button size="small" startIcon={<AddIcon />} onClick={() => openNew(groupKey)}>
                    Thêm vào nhóm
                  </Button>
                </Box>
                <Stack divider={<Divider />} spacing={0}>
                  {list.map((f, i) => (
                    <Box key={f.id} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, py: 1 }}>
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography variant="body1" sx={{ fontWeight: 500, overflowWrap: 'anywhere' }}>
                          {f.question}
                        </Typography>
                        <Box sx={{ mt: 0.5 }}>
                          <StatusChip value={f.isPublished ? 'published' : 'draft'} options={PUBLISH_OPTIONS} />
                        </Box>
                      </Box>
                      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, alignItems: 'center', flexShrink: 0 }}>
                        <ReorderButtons index={i} count={list.length} onMove={(from, to) => void move(groupKey, list, from, to)} disabled={busy} itemLabel="câu hỏi" />
                        <Tooltip title="Sửa">
                          <span>
                            <IconButton size="small" aria-label="Sửa câu hỏi" disabled={busy} onClick={() => setDialog(f)}>
                              <EditOutlinedIcon fontSize="inherit" />
                            </IconButton>
                          </span>
                        </Tooltip>
                        <Tooltip title="Xoá">
                          <span>
                            <IconButton size="small" color="error" aria-label="Xoá câu hỏi" disabled={busy} onClick={() => void onDelete(f)}>
                              <DeleteOutlineIcon fontSize="inherit" />
                            </IconButton>
                          </span>
                        </Tooltip>
                      </Box>
                    </Box>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          ))
        )}
      </Stack>

      <FaqDialog target={dialog} defaultGroupKey={newGroup} groupKeys={groupKeys} onClose={() => setDialog(null)} />
    </PageContainer>
  )
}
