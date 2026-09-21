import { useEffect, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  MenuItem,
  Pagination,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useSearchParams } from 'react-router-dom'
import { PageContainer } from '@af/ui'
import { parseApiError } from '@af/utils'
import { AUDIT_LOGS_PAGE_SIZE, useAuditLogs } from '../hooks'
import { AUDIT_TARGET_TYPES, type AuditLogDto } from '../types'

const DEBOUNCE_MS = 300

/** `dd/MM/yyyy HH:mm` theo giờ trình duyệt; chuỗi lỗi ⇒ hiện nguyên. */
function formatAt(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const p = (n: number) => String(n).padStart(2, '0')
  return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()} ${p(d.getHours())}:${p(d.getMinutes())}`
}

const targetLabel = (t: string | null) => AUDIT_TARGET_TYPES.find((x) => x.value === t)?.label ?? t ?? '—'

const ResultChip = ({ ok }: { ok: boolean }) => <Chip size="small" label={ok ? 'Thành công' : 'Thất bại'} color={ok ? 'success' : 'error'} variant={ok ? 'outlined' : 'filled'} />

/**
 * `/he-thong/nhat-ky?targetType=&targetId=&page=` (hợp đồng W3b §5.3.3a, cần `cms:users.manage`): bộ lọc + trang
 * lên URL (`replace`); ≥ md bảng, < md danh sách thẻ. Chỉ đọc — không có nút ẩn theo quyền.
 */
export function AuditLogsPage() {
  const theme = useTheme()
  const isMdUp = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  const [params, setParams] = useSearchParams()
  const targetType = params.get('targetType') ?? ''
  const targetId = params.get('targetId') ?? ''
  const pageRaw = Number(params.get('page'))
  const page = Number.isInteger(pageRaw) && pageRaw >= 1 ? pageRaw : 1

  const patch = (changes: Record<string, string | null>) =>
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        for (const [k, v] of Object.entries(changes)) {
          if (v) out.set(k, v)
          else out.delete(k)
        }
        return out
      },
      { replace: true },
    )

  // Ô mã đối tượng gõ tay ⇒ debounce rồi đẩy lên URL.
  const [idInput, setIdInput] = useState(targetId)
  useEffect(() => {
    setIdInput((prev) => (prev.trim() === targetId ? prev : targetId))
  }, [targetId])
  useEffect(() => {
    if (idInput.trim() === targetId) return
    const t = setTimeout(() => patch({ targetId: idInput.trim(), page: null }), DEBOUNCE_MS)
    return () => clearTimeout(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [idInput, targetId])

  const logs = useAuditLogs({ targetType: targetType || undefined, targetId: targetId || undefined, page })
  const total = logs.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(total / (logs.data?.pageSize || AUDIT_LOGS_PAGE_SIZE)))
  const err = logs.error ? parseApiError(logs.error) : null
  const items = logs.data?.items ?? []
  const loading = logs.isPending || logs.isPlaceholderData

  return (
    <PageContainer title="Nhật ký thao tác">
      <Stack spacing={2}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <TextField
            select
            size="small"
            label="Đối tượng"
            value={targetType}
            onChange={(e) => patch({ targetType: e.target.value, page: null })}
            sx={{ minWidth: 200 }}
          >
            {AUDIT_TARGET_TYPES.map((t) => (
              <MenuItem key={t.value} value={t.value}>
                {t.label}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label="Mã đối tượng"
            value={idInput}
            onChange={(e) => setIdInput(e.target.value)}
            placeholder="Dán id để xem lịch sử một bản ghi"
            fullWidth
            slotProps={{ htmlInput: { maxLength: 64, autoCapitalize: 'none', spellCheck: false } }}
          />
        </Stack>

        {err && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={() => void logs.refetch()}>
                Thử lại
              </Button>
            }
          >
            Không tải được nhật ký: {err.message}
          </Alert>
        )}

        {logs.isPending ? (
          <Stack spacing={1}>
            <Skeleton variant="rounded" height={48} />
            <Skeleton variant="rounded" height={48} />
            <Skeleton variant="rounded" height={48} />
          </Stack>
        ) : items.length === 0 ? (
          <Alert severity="info">Không có bản ghi nào khớp bộ lọc.</Alert>
        ) : isMdUp ? (
          <TableContainer component={Card} variant="outlined" sx={{ opacity: loading ? 0.6 : 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Thời điểm</TableCell>
                  <TableCell>Người làm</TableCell>
                  <TableCell>Hành động</TableCell>
                  <TableCell>Đối tượng</TableCell>
                  <TableCell>Tóm tắt</TableCell>
                  <TableCell>Kết quả</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((l) => (
                  <TableRow key={l.id} hover>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatAt(l.at)}</TableCell>
                    <TableCell sx={{ overflowWrap: 'anywhere' }}>{l.actorEmail ?? l.actorId ?? '—'}</TableCell>
                    <TableCell>
                      <code>{l.action}</code>
                    </TableCell>
                    <TableCell>
                      {targetLabel(l.targetType)}
                      {l.targetId && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontFamily: 'monospace', overflowWrap: 'anywhere' }}>
                          {l.targetId}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell sx={{ overflowWrap: 'anywhere' }}>{l.summary}</TableCell>
                    <TableCell>
                      <ResultChip ok={l.success} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Stack spacing={1} sx={{ opacity: loading ? 0.6 : 1 }}>
            {items.map((l: AuditLogDto) => (
              <Card key={l.id} variant="outlined">
                <CardContent sx={{ '&:last-child': { pb: 1.5 } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1, mb: 0.5 }}>
                    <Typography variant="caption" color="text.secondary">
                      {formatAt(l.at)}
                    </Typography>
                    <ResultChip ok={l.success} />
                  </Box>
                  <Typography variant="body2" sx={{ fontWeight: 500, overflowWrap: 'anywhere' }}>
                    {l.summary}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', overflowWrap: 'anywhere' }}>
                    <code>{l.action}</code> · {targetLabel(l.targetType)} · {l.actorEmail ?? l.actorId ?? '—'}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Stack>
        )}

        {total > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.secondary">
              {total} bản ghi
            </Typography>
            {pageCount > 1 && (
              <Pagination count={pageCount} page={Math.min(page, pageCount)} onChange={(_e, p) => patch({ page: p <= 1 ? null : String(p) })} size={isXs ? 'small' : 'medium'} siblingCount={isXs ? 0 : 1} />
            )}
          </Box>
        )}
      </Stack>
    </PageContainer>
  )
}
