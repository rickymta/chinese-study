import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  InputAdornment,
  Pagination,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { PageContainer, useTabParam } from '@af/ui'
import { formatRelativeTime, parseApiError } from '@af/utils'
import { enumParam, patchSearchParams } from '@/lib/searchParams'
import { ADMIN_LESSONS_PAGE_SIZE, useAdminLessons } from '../hooks'
import type { AdminLessonListItem, LessonStatus } from '../types'
import { AdminContentTabs } from '../components/AdminContentTabs'
import { CreateLessonDialog } from '../components/CreateLessonDialog'
import { LESSON_STATUS_COLORS, LESSON_STATUS_LABELS } from '../components/LessonStatusBar'

const STATUS_TABS = ['tat-ca', 'nhap', 'da-xuat-ban', 'luu-tru'] as const
type StatusTab = (typeof STATUS_TABS)[number]
const STATUS_OF_TAB: Record<StatusTab, LessonStatus | undefined> = { 'tat-ca': undefined, nhap: 'draft', 'da-xuat-ban': 'published', 'luu-tru': 'archived' }
const STATUS_TAB_LABELS: Record<StatusTab, string> = { 'tat-ca': 'Tất cả', nhap: 'Nháp', 'da-xuat-ban': 'Đã xuất bản', 'luu-tru': 'Lưu trữ' }
const SEARCH_DEBOUNCE_MS = 300

function ReviewChip({ item }: { item: AdminLessonListItem }) {
  return item.reviewStatus === 'machine' ? (
    <Chip size="small" variant="outlined" color="warning" label="Chưa duyệt" />
  ) : (
    <Chip size="small" variant="outlined" color="success" label="Đã duyệt" />
  )
}

/**
 * `/quan-tri/bai-hoc?trang-thai=&q=&page=` (F10 §5.3.3, cần `content.manage`): lọc trạng thái (mặc định mọi trạng
 * thái trừ lưu trữ), tìm tiêu đề/slug (debounce, `replace` lên URL), phân trang; bấm dòng ⇒ trang soạn; "Tạo bài".
 * Desktop: bảng; điện thoại: thẻ.
 */
export function AdminLessonListPage() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const navigate = useNavigate()
  // Chỉ ĐỌC qua `useTabParam` (giá trị lạ ⇒ mặc định); GHI gộp với `page` ở `setStatusFilter`.
  const [statusTab] = useTabParam<StatusTab>(STATUS_TABS, 'tat-ca', 'trang-thai')
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const pageRaw = Number(params.get('page'))
  const page = Number.isInteger(pageRaw) && pageRaw >= 1 ? pageRaw : 1
  const [creating, setCreating] = useState(false)

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

  const setPage = (next: number) => setParams((prev) => patchSearchParams(prev, { page: next <= 1 ? null : next }), { replace: true })
  // Đổi bộ lọc trạng thái + về trang 1 trong MỘT lần ghi URL (hai lần liên tiếp ⇒ lần sau đè mất lần trước).
  const setStatusFilter = (next: StatusTab) =>
    setParams((prev) => patchSearchParams(prev, { 'trang-thai': enumParam(next, 'tat-ca'), page: null }), { replace: true })

  const lessons = useAdminLessons({ status: STATUS_OF_TAB[statusTab], q, page })
  const items = lessons.data?.items ?? []
  const total = lessons.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / (lessons.data?.pageSize || ADMIN_LESSONS_PAGE_SIZE)))
  const err = lessons.error ? parseApiError(lessons.error) : null
  const loading = lessons.isPending
  const open = (item: AdminLessonListItem) => navigate(`/quan-tri/bai-hoc/${item.id}`)

  return (
    <PageContainer title="Quản trị nội dung">
      <Stack spacing={2}>
        <AdminContentTabs />

        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
          <ToggleButtonGroup
            exclusive
            size="small"
            value={statusTab}
            onChange={(_e, v: StatusTab | null) => {
              if (v) setStatusFilter(v)
            }}
            aria-label="Lọc trạng thái"
            sx={{ flexWrap: 'wrap' }}
          >
            {STATUS_TABS.map((t) => (
              <ToggleButton key={t} value={t}>
                {STATUS_TAB_LABELS[t]}
              </ToggleButton>
            ))}
          </ToggleButtonGroup>
          <Box sx={{ flex: 1 }} />
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)} sx={{ minHeight: 40 }}>
            Tạo bài
          </Button>
        </Box>

        <TextField
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Tìm theo tiêu đề hoặc slug"
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
            htmlInput: { 'aria-label': 'Tìm bài học', maxLength: 100 },
          }}
        />

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void lessons.refetch()}>
                Thử lại
              </Button>
            }
          >
            Không tải được danh sách: {err.message}
          </Alert>
        )}

        {loading ? (
          <Stack spacing={1}>
            {[0, 1, 2].map((i) => (
              <Skeleton key={i} variant="rounded" height={isXs ? 96 : 52} />
            ))}
          </Stack>
        ) : items.length === 0 ? (
          <Typography color="text.secondary">
            {q ? 'Không có bài nào khớp.' : statusTab === 'luu-tru' ? 'Không có bài lưu trữ.' : 'Chưa có bài học nào — bấm "Tạo bài" để soạn bài đầu tiên.'}
          </Typography>
        ) : isXs ? (
          <Stack spacing={1} sx={{ opacity: lessons.isPlaceholderData ? 0.6 : 1 }}>
            {items.map((item) => (
              <Card key={item.id} variant="outlined">
                <CardActionArea onClick={() => open(item)}>
                  <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 0.75 }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
                      <Typography component="span" sx={{ fontWeight: 700, color: 'primary.main', fontVariantNumeric: 'tabular-nums', flexShrink: 0 }}>
                        {String(item.orderIndex).padStart(2, '0')}
                      </Typography>
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography sx={{ fontWeight: 600, lineHeight: 1.3 }}>{item.title}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          /bai-hoc/{item.slug}
                        </Typography>
                      </Box>
                    </Box>
                    <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap' }}>
                      <Chip size="small" color={LESSON_STATUS_COLORS[item.status]} label={LESSON_STATUS_LABELS[item.status]} />
                      <ReviewChip item={item} />
                      {item.source === 'seed' && <Chip size="small" variant="outlined" label="Có sẵn" />}
                    </Box>
                    <Typography variant="caption" color="text.secondary">
                      {item.wordCount} từ · {item.questionCount} câu{item.editedAt ? ` · sửa ${formatRelativeTime(item.editedAt)}` : ''}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            ))}
          </Stack>
        ) : (
          <Box sx={{ overflowX: 'auto', border: 1, borderColor: 'divider', borderRadius: 1, opacity: lessons.isPlaceholderData ? 0.6 : 1 }}>
            <Table size="small" aria-label="Danh sách bài học">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: 56 }}>#</TableCell>
                  <TableCell>Bài</TableCell>
                  <TableCell>Trạng thái</TableCell>
                  <TableCell>Duyệt</TableCell>
                  <TableCell>Nguồn</TableCell>
                  <TableCell align="right">Từ</TableCell>
                  <TableCell align="right">Câu</TableCell>
                  <TableCell>Sửa lúc</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((item) => (
                  <TableRow key={item.id} hover onClick={() => open(item)} sx={{ cursor: 'pointer' }}>
                    <TableCell sx={{ fontVariantNumeric: 'tabular-nums' }}>{item.orderIndex}</TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {item.title}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {item.slug}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip size="small" color={LESSON_STATUS_COLORS[item.status]} label={LESSON_STATUS_LABELS[item.status]} />
                    </TableCell>
                    <TableCell>
                      <ReviewChip item={item} />
                    </TableCell>
                    <TableCell>{item.source === 'seed' ? 'Có sẵn' : 'Tự soạn'}</TableCell>
                    <TableCell align="right">{item.wordCount}</TableCell>
                    <TableCell align="right">{item.questionCount}</TableCell>
                    <TableCell>{item.editedAt ? formatRelativeTime(item.editedAt) : '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}

        {total > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.secondary">
              {total} bài
            </Typography>
            {pageCount > 1 && (
              <Pagination count={pageCount} page={Math.min(page, pageCount)} onChange={(_e, p) => setPage(p)} size={isXs ? 'small' : 'medium'} siblingCount={isXs ? 0 : 1} />
            )}
          </Box>
        )}
      </Stack>

      <CreateLessonDialog open={creating} onClose={() => setCreating(false)} />
    </PageContainer>
  )
}
