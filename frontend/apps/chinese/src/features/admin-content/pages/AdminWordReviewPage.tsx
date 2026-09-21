import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Alert, Box, Button, InputAdornment, MenuItem, Pagination, Stack, TextField, Typography, useMediaQuery, useTheme } from '@mui/material'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import DoneAllIcon from '@mui/icons-material/DoneAll'
import { PageContainer, StickyActionBar, useConfirm, useTabParam, useToast } from '@af/ui'
import { parseApiError } from '@af/utils'
import { enumParam, patchSearchParams } from '@/lib/searchParams'
import { ChineseSpeechProvider } from '@/components/speech/ChineseSpeechProvider'
import type { HanVietStatus, MeaningViStatus } from '@af/chinese-kit'
import { ADMIN_WORDS_PAGE_SIZE, useAdminWords, useBulkReviewWords } from '../hooks'
import type { AdminWord } from '../types'
import { AdminContentTabs } from '../components/AdminContentTabs'
import { WordEditDrawer } from '../components/WordEditDrawer'
import { WordReviewList } from '../components/WordReviewList'

const STATUS_VALUES = ['machine', 'reviewed', 'tat-ca'] as const
type StatusParam = (typeof STATUS_VALUES)[number]
const HAN_VIET_VALUES = ['tat-ca', 'derived', 'reviewed'] as const
type HanVietParam = (typeof HAN_VIET_VALUES)[number]
// Backend: `hsk` bỏ trống ⇒ mặc định 1 (không có "mọi cấp") ⇒ chỉ cho chọn 1–7.
const HSK_VALUES = ['1', '2', '3', '4', '5', '6', '7'] as const
type HskParam = (typeof HSK_VALUES)[number]
const SEARCH_DEBOUNCE_MS = 300
const BULK_MAX = 100

/**
 * `/quan-tri/tu-vung?trang-thai=machine|reviewed|tat-ca&han-viet=&hsk=1..7&q=&page=&sua=<id>` (F10 §5.3.3, cần
 * `content.manage`): mặc định `trang-thai=machine`, `hsk=1`, sắp theo lộ trình (R-CA11 — từ sắp học duyệt trước).
 * Chọn nhiều ⇒ "Đánh dấu đã duyệt (N)" (`POST /api/admin/words/review`); có `conflicts` ⇒ toast cảnh báo + refetch.
 * Bấm dòng ⇒ `WordEditDrawer` (`?sua=`). Sau lưu, dòng không còn khớp bộ lọc vẫn ở lại tới lần refetch.
 */
function AdminWordReviewInner() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const toast = useToast()
  const confirm = useConfirm()
  // Chỉ ĐỌC qua `useTabParam` (giá trị lạ ⇒ mặc định); GHI gộp với `page` ở `setFilter` — hai lần `setSearchParams`
  // liên tiếp thì lần sau dựng từ URL cũ, đè mất bộ lọc vừa chọn (review F10).
  const [status] = useTabParam<StatusParam>(STATUS_VALUES, 'machine', 'trang-thai')
  const [hanViet] = useTabParam<HanVietParam>(HAN_VIET_VALUES, 'tat-ca', 'han-viet')
  const [hsk] = useTabParam<HskParam>(HSK_VALUES, '1', 'hsk')
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const editingId = params.get('sua')
  const pageRaw = Number(params.get('page'))
  const page = Number.isInteger(pageRaw) && pageRaw >= 1 ? pageRaw : 1

  const [input, setInput] = useState(q)
  useEffect(() => {
    setInput((prev) => (prev.trim() === q ? prev : q))
  }, [q])
  useEffect(() => {
    if (input.trim() === q) return
    const t = setTimeout(() => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev)
          const trimmed = input.trim()
          if (trimmed) out.set('q', trimmed)
          else out.delete('q')
          out.delete('page')
          return out
        },
        { replace: true },
      )
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(t)
  }, [input, q, setParams])

  const patchParams = (patch: Record<string, string | null>) => setParams((prev) => patchSearchParams(prev, patch), { replace: true })
  const setPage = (next: number) => patchParams({ page: next <= 1 ? null : String(next) })
  /** Đổi một bộ lọc + về trang 1 trong MỘT lần ghi URL; giá trị mặc định ⇒ xoá khoá. */
  const setFilter = (patch: { status?: StatusParam; hanViet?: HanVietParam; hsk?: HskParam }) =>
    patchParams({
      ...(patch.status !== undefined ? { 'trang-thai': enumParam(patch.status, 'machine') } : {}),
      ...(patch.hanViet !== undefined ? { 'han-viet': enumParam(patch.hanViet, 'tat-ca') } : {}),
      ...(patch.hsk !== undefined ? { hsk: enumParam(patch.hsk, '1') } : {}),
      page: null,
    })
  const openWord = (w: AdminWord) => patchParams({ sua: w.id })
  const closeWord = () => patchParams({ sua: null })

  const query = useMemo(
    () => ({
      meaningViStatus: status === 'tat-ca' ? undefined : (status as MeaningViStatus),
      hanVietStatus: hanViet === 'tat-ca' ? undefined : (hanViet as HanVietStatus),
      hsk: Number(hsk),
      q,
      page,
    }),
    [status, hanViet, hsk, q, page],
  )
  const words = useAdminWords(query)
  const bulk = useBulkReviewWords()
  const items = words.data?.items ?? []
  const total = words.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / (words.data?.pageSize || ADMIN_WORDS_PAGE_SIZE)))
  const err = words.error ? parseApiError(words.error) : null

  // Lựa chọn duyệt hàng loạt — đổi bộ lọc/trang ⇒ bỏ chọn (version của từ ở trang khác không còn trong tay).
  const [selected, setSelected] = useState<Set<string>>(new Set())
  useEffect(() => {
    setSelected(new Set())
  }, [status, hanViet, hsk, q, page])

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  const toggleAll = (ids: string[], checked: boolean) =>
    setSelected((prev) => {
      const next = new Set(prev)
      ids.forEach((id) => (checked ? next.add(id) : next.delete(id)))
      return next
    })

  const bulkReview = async () => {
    const chosen = items.filter((w) => selected.has(w.id)).slice(0, BULK_MAX)
    if (chosen.length === 0) return
    const ok = await confirm({
      title: `Đánh dấu đã duyệt ${chosen.length} từ?`,
      message: 'Chỉ đổi trạng thái nghĩa sang "Đã duyệt" — nội dung nghĩa giữ nguyên. Hãy chắc bạn đã đọc qua các nghĩa này.',
      confirmText: 'Đánh dấu đã duyệt',
    })
    if (!ok) return
    try {
      const r = await bulk.mutateAsync({ items: chosen.map((w) => ({ id: w.id, version: w.version })) })
      if (r.updated > 0) toast.success(`Đã duyệt ${r.updated} từ`)
      if (r.conflicts.length > 0) toast.warning(`${r.conflicts.length} từ đã bị sửa ở nơi khác — danh sách đã tải lại, hãy chọn lại.`)
      if (r.notFound.length > 0) toast.warning(`${r.notFound.length} từ không còn tồn tại.`)
      setSelected(new Set())
    } catch (e) {
      toast.error(parseApiError(e).message)
    }
  }

  const editingInitial = editingId ? items.find((w) => w.id === editingId) : undefined

  return (
    <PageContainer title="Quản trị nội dung">
      <Stack spacing={2}>
        <AdminContentTabs />
        <Typography variant="body2" color="text.secondary">
          Duyệt nghĩa tiếng Việt và Hán Việt. Mặc định hiện từ HSK 1 chưa duyệt theo thứ tự lộ trình — từ sắp học được duyệt trước.
        </Typography>

        <Box sx={{ display: 'grid', gap: 1, gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(3, minmax(0, 1fr))' } }}>
          <TextField select size="small" label="Nghĩa" value={status} onChange={(e) => setFilter({ status: e.target.value as StatusParam })}>
            <MenuItem value="machine">Chưa duyệt</MenuItem>
            <MenuItem value="reviewed">Đã duyệt</MenuItem>
            <MenuItem value="tat-ca">Tất cả</MenuItem>
          </TextField>
          <TextField select size="small" label="Hán Việt" value={hanViet} onChange={(e) => setFilter({ hanViet: e.target.value as HanVietParam })}>
            <MenuItem value="tat-ca">Tất cả</MenuItem>
            <MenuItem value="derived">Suy ra</MenuItem>
            <MenuItem value="reviewed">Đã duyệt</MenuItem>
          </TextField>
          <TextField select size="small" label="HSK" value={hsk} onChange={(e) => setFilter({ hsk: e.target.value as HskParam })} sx={{ gridColumn: { xs: '1 / -1', sm: 'auto' } }}>
            {HSK_VALUES.map((v) => (
              <MenuItem key={v} value={v}>
                Cấp {v}
              </MenuItem>
            ))}
          </TextField>
        </Box>

        <TextField
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Tìm chữ Hán, pinyin hoặc nghĩa"
          fullWidth
          size="small"
          autoComplete="off"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchOutlinedIcon fontSize="small" />
                </InputAdornment>
              ),
            },
            htmlInput: { 'aria-label': 'Tìm từ', maxLength: 64, autoCapitalize: 'none', spellCheck: false },
          }}
        />

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void words.refetch()}>
                Thử lại
              </Button>
            }
          >
            Không tải được danh sách: {err.message}
          </Alert>
        )}

        <WordReviewList
          words={items}
          selected={selected}
          onToggle={toggle}
          onToggleAll={toggleAll}
          onOpen={openWord}
          loading={words.isPending}
          stale={words.isPlaceholderData}
        />

        {total > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.secondary">
              {total} từ
            </Typography>
            {pageCount > 1 && (
              <Pagination count={pageCount} page={Math.min(page, pageCount)} onChange={(_e, p) => setPage(p)} size={isXs ? 'small' : 'medium'} siblingCount={isXs ? 0 : 1} />
            )}
          </Box>
        )}

        {selected.size > 0 && (
          <StickyActionBar>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              <Typography variant="body2" sx={{ flex: 1 }}>
                Đã chọn {selected.size} từ
              </Typography>
              <Button color="inherit" size="small" onClick={() => setSelected(new Set())} disabled={bulk.isPending}>
                Bỏ chọn
              </Button>
              <Button variant="contained" color="success" startIcon={<DoneAllIcon />} onClick={() => void bulkReview()} loading={bulk.isPending}>
                Đánh dấu đã duyệt ({Math.min(selected.size, BULK_MAX)})
              </Button>
            </Box>
          </StickyActionBar>
        )}
      </Stack>

      <WordEditDrawer wordId={editingId} initial={editingInitial} onClose={closeWord} />
    </PageContainer>
  )
}

export function AdminWordReviewPage() {
  return (
    <ChineseSpeechProvider>
      <AdminWordReviewInner />
    </ChineseSpeechProvider>
  )
}
